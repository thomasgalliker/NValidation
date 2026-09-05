using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NValidation.AspNetCore
{
    /// <summary>
    /// Validates an action's payload before the action runs: for every parameter bound from the request
    /// body or form, the validator registered for that parameter's declared type is resolved and run. A
    /// parameter whose type has no registered validator is left alone — see
    /// <see cref="ValidationFilterOptions.MissingValidatorBehavior"/> — and one marked with
    /// <see cref="SkipValidationAttribute"/> is skipped outright.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Failures of every parameter are collected into one <see cref="ValidationResult"/> and thrown as a
    /// single <see cref="ValidationException"/>, so the response reports everything that is wrong with the
    /// request at once. Register <see cref="ValidationExceptionHandler"/>, or read
    /// <see cref="ValidationException.Errors"/> in the host's own exception handler, to turn that into a
    /// 400 problem details response.
    /// </para>
    /// <para>
    /// Register it as a global filter, which also settles the order: authorization filters run first, so an
    /// unauthorized request is still rejected before its payload is looked at.
    /// <code>
    /// services.AddControllers(o => o.Filters.Add&lt;ValidationActionFilter&gt;());
    /// </code>
    /// </para>
    /// <para>
    /// This is an MVC filter, so it covers controller actions only; a minimal API endpoint validates by
    /// calling its validator in the handler.
    /// </para>
    /// </remarks>
    public sealed partial class ValidationActionFilter : IAsyncActionFilter
    {
        /// <summary>
        /// Closed <see cref="IValidator{T}"/> types by the parameter type they validate. The set of action
        /// parameters is fixed once the application model is built, so this settles after warm-up.
        /// </summary>
        /// <remarks>
        /// <see cref="Type.MakeGenericType"/> is the one piece of reflection this filter cannot avoid: the
        /// service to resolve is only known from a parameter's runtime <see cref="Type"/>. An application
        /// published ahead-of-time therefore has to keep the closed validator types it registers, which it
        /// does anyway by registering them.
        /// </remarks>
        private static readonly ConcurrentDictionary<Type, Type> ValidatorServiceTypes = new();

        /// <summary>
        /// The action parameters already reported as having no validator. Whether a payload has one is a
        /// property of the action, not of the request, so warning about it on every request would bury
        /// the warning in its own repetitions.
        /// </summary>
        private static readonly ConcurrentDictionary<(string ActionId, string ParameterName), byte> ReportedMissingValidators = new();

        /// <summary>
        /// Whether a parameter is excluded from validation, by the action descriptor it belongs to.
        /// </summary>
        /// <remarks>
        /// The answer comes from attributes on the parameter, the action and the controller, none of
        /// which change once the application model is built — but finding it is reflection, and the
        /// endpoint metadata is walked once per parameter even though the answer does not depend on the
        /// parameter. Asked once per action instead of on every request.
        /// </remarks>
        private static readonly ConcurrentDictionary<(string ActionId, string ParameterName), bool> SkipDecisions = new();

        private readonly ValidationFilterOptions options;
        private readonly IModelMetadataProvider modelMetadataProvider;
        private readonly ILogger<ValidationActionFilter> logger;

        /// <summary>
        /// Creates the filter.
        /// </summary>
        public ValidationActionFilter(
            IOptions<ValidationFilterOptions> options,
            IModelMetadataProvider modelMetadataProvider,
            ILogger<ValidationActionFilter> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(modelMetadataProvider);
            ArgumentNullException.ThrowIfNull(logger);

            this.options = options.Value;
            this.modelMetadataProvider = modelMetadataProvider;
            this.logger = logger;
        }

        /// <inheritdoc />
        /// <exception cref="ValidationException">One or more of the action's payloads is invalid.</exception>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            List<ValidationError>? errors = null;

            foreach (var parameter in context.ActionDescriptor.Parameters)
            {
                var validationResult = await this.ValidateParameterAsync(context, parameter);
                if (validationResult is null || validationResult.Succeeded)
                {
                    continue;
                }

                errors ??= [];
                errors.AddRange(validationResult.Errors);
            }

            if (errors is not null)
            {
                ValidationResult.FromValidationErrors(errors).ThrowIfInvalid();
            }

            await next();
        }

        /// <summary>
        /// The result of validating one parameter, or <c>null</c> when there was nothing to validate.
        /// </summary>
        private async Task<ValidationResult?> ValidateParameterAsync(ActionExecutingContext context, ParameterDescriptor parameter)
        {
            if (!this.IsRequestPayload(parameter) || IsSkipped(context, parameter))
            {
                return null;
            }

            var validatorServiceType = ValidatorServiceTypes.GetOrAdd(
                parameter.ParameterType,
                static parameterType => typeof(IValidator<>).MakeGenericType(parameterType));

            if (context.HttpContext.RequestServices.GetService(validatorServiceType) is not IValidator validator)
            {
                // Reported per action rather than per request: whether a payload has a validator is a
                // property of the action, and does not depend on what a caller happened to send.
                this.HandleMissingValidator(context, parameter);
                return null;
            }

            // An absent body binds as null. Validating it would report a missing payload as a server
            // error, because a validator rejects a null instance rather than failing it.
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument) || argument is null)
            {
                return null;
            }

            return await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        }

        /// <summary>
        /// Whether the parameter carries what the caller sent, rather than where it was addressed. Route
        /// and query values are excluded: a complex type bound from them is the application's own
        /// plumbing — paging, filtering — and not a payload a client composed.
        /// </summary>
        /// <remarks>
        /// A parameter can also carry no binding source at all. Inside <c>[ApiController]</c> that does
        /// not happen — binding-source inference fills it in — but outside it a complex parameter with no
        /// <c>[FromBody]</c> is bound from whatever the caller sent, and treating that as "not a payload"
        /// would leave it silently unvalidated and out of reach of
        /// <see cref="ValidationFilterOptions.MissingValidatorBehavior"/>. MVC's own notion of a complex
        /// type is what separates such a payload from an addressing value like a route id.
        /// </remarks>
        private bool IsRequestPayload(ParameterDescriptor parameter)
        {
            var bindingSource = parameter.BindingInfo?.BindingSource;

            if (bindingSource != null)
            {
                return bindingSource == BindingSource.Body || bindingSource == BindingSource.Form;
            }

            return this.modelMetadataProvider.GetMetadataForType(parameter.ParameterType).IsComplexType;
        }

        private static bool IsSkipped(ActionExecutingContext context, ParameterDescriptor parameter)
        {
            return SkipDecisions.GetOrAdd(
                (context.ActionDescriptor.Id, parameter.Name),
                static (_, state) => IsSkippedCore(state.Context, state.Parameter),
                (Context: context, Parameter: parameter));
        }

        private static bool IsSkippedCore(ActionExecutingContext context, ParameterDescriptor parameter)
        {
            // inherit is ignored for a ParameterInfo — the CLR does not walk base-method parameters —
            // so saying false is the honest form of what actually happens.
            if (parameter is ControllerParameterDescriptor controllerParameter &&
                controllerParameter.ParameterInfo.IsDefined(typeof(SkipValidationAttribute), inherit: false))
            {
                return true;
            }

            // Carries the action's and the controller's attributes both, so either level excludes.
            foreach (var metadata in context.ActionDescriptor.EndpointMetadata)
            {
                if (metadata is SkipValidationAttribute)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleMissingValidator(ActionExecutingContext context, ParameterDescriptor parameter)
        {
            switch (this.options.MissingValidatorBehavior)
            {
                case MissingValidatorBehavior.Log:
                    if (ReportedMissingValidators.TryAdd((context.ActionDescriptor.Id, parameter.Name), 0))
                    {
                        this.LogMissingValidator(parameter.Name, parameter.ParameterType, context.ActionDescriptor.DisplayName);
                    }

                    break;

                case MissingValidatorBehavior.Throw:
                    throw new InvalidOperationException(
                        $"No validator is registered for parameter '{parameter.Name}' of type " +
                        $"'{parameter.ParameterType}' on action '{context.ActionDescriptor.DisplayName}'. " +
                        $"Register an IValidator<{parameter.ParameterType.Name}>, or mark the parameter with " +
                        $"[SkipValidation] to record that it is deliberately not validated.");

                case MissingValidatorBehavior.Ignore:
                default:
                    break;
            }
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "No validator is registered for parameter '{ParameterName}' of type '{ParameterType}' on action '{ActionDisplayName}'.")]
        private partial void LogMissingValidator(string parameterName, Type parameterType, string? actionDisplayName);
    }
}
