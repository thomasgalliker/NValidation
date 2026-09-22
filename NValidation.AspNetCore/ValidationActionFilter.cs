using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    /// <see cref="SkipNValidationAttribute"/> is skipped outright. A payload whose parameter, action or
    /// controller carries <see cref="ValidationGroupsAttribute"/> is validated with those rule groups
    /// selected.
    /// </summary>
    /// <remarks>
    /// Failures of every parameter are collected into one <see cref="ValidationResult"/> and thrown as a
    /// single <see cref="ValidationException"/>, so the response reports everything that is wrong with the
    /// request at once. Register <see cref="ValidationExceptionHandler"/>, or read
    /// <see cref="ValidationException.Errors"/> in the host's own exception handler, to turn that into a
    /// 400 problem details response.
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
        /// Held weakly, so an entry lives exactly as long as the action descriptor it describes.
        /// </summary>
        private static readonly ConditionalWeakTable<ActionDescriptor, ActionCache> Actions = new();

        private readonly ValidationFilterOptions options;
        private readonly IModelMetadataProvider modelMetadataProvider;
        private readonly ILogger<ValidationActionFilter> logger;

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

        private async Task<ValidationResult?> ValidateParameterAsync(ActionExecutingContext context, ParameterDescriptor parameter)
        {
            if (!this.IsRequestPayload(parameter) || IsSkipped(context, parameter))
            {
                return null;
            }

            var validatorServiceType = Actions.GetOrCreateValue(context.ActionDescriptor).ValidatorServiceTypes.GetOrAdd(
                parameter.Name,
                static (_, parameterType) => typeof(IValidator<>).MakeGenericType(parameterType),
                parameter.ParameterType);

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

            var cancellationToken = context.HttpContext.RequestAborted;

            return ResolveOptions(context, parameter) is { } options
                ? await validator.ValidateAsync(argument, options, cancellationToken)
                : await validator.ValidateAsync(argument, cancellationToken);
        }

        /// <summary>
        /// The options this payload is validated with, or null where nothing selected rule groups for it.
        /// Decided per action parameter rather than per request: which groups a payload is validated with
        /// is a property of the action.
        /// </summary>
        private static NValidationOptions? ResolveOptions(ActionExecutingContext context, ParameterDescriptor parameter)
        {
            return Actions.GetOrCreateValue(context.ActionDescriptor).GroupOptions.GetOrAdd(
                parameter.Name,
                static (_, state) => ResolveOptionsCore(state.Context, state.Parameter),
                (Context: context, Parameter: parameter));
        }

        private static NValidationOptions? ResolveOptionsCore(ActionExecutingContext context, ParameterDescriptor parameter)
        {
            // inherit is ignored for a ParameterInfo — the CLR does not walk base-method parameters —
            // so saying false is the honest form of what actually happens.
            var attribute = parameter is ControllerParameterDescriptor controllerParameter
                ? controllerParameter.ParameterInfo.GetCustomAttribute<ValidationGroupsAttribute>(inherit: false)
                : null;

            if (attribute is null)
            {
                // Carries the controller's attributes and the action's after them, so the last one found
                // is the most specific.
                foreach (var metadata in context.ActionDescriptor.EndpointMetadata)
                {
                    if (metadata is ValidationGroupsAttribute candidate)
                    {
                        attribute = candidate;
                    }
                }
            }

            return attribute is null ? null : new NValidationOptions { ValidationGroups = attribute.Groups };
        }

        /// <summary>
        /// Whether the parameter carries what the caller sent rather than where it was addressed. Outside
        /// <c>[ApiController]</c> the binding source is not filled in, so a complex type with none counts.
        /// </summary>
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
            return Actions.GetOrCreateValue(context.ActionDescriptor).SkipDecisions.GetOrAdd(
                parameter.Name,
                static (_, state) => IsSkippedCore(state.Context, state.Parameter),
                (Context: context, Parameter: parameter));
        }

        private static bool IsSkippedCore(ActionExecutingContext context, ParameterDescriptor parameter)
        {
            // inherit is ignored for a ParameterInfo — the CLR does not walk base-method parameters —
            // so saying false is the honest form of what actually happens.
            if (parameter is ControllerParameterDescriptor controllerParameter &&
                controllerParameter.ParameterInfo.IsDefined(typeof(SkipNValidationAttribute), inherit: false))
            {
                return true;
            }

            // Carries the action's and the controller's attributes both, so either level excludes.
            foreach (var metadata in context.ActionDescriptor.EndpointMetadata)
            {
                if (metadata is SkipNValidationAttribute)
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
                    if (Actions.GetOrCreateValue(context.ActionDescriptor).ReportedMissingValidators.TryAdd(parameter.Name, 0))
                    {
                        this.LogMissingValidator(
                            parameter.Name,
                            parameter.ParameterType.GetFormattedFullName(),
                            context.ActionDescriptor.DisplayName);
                    }

                    break;

                case MissingValidatorBehavior.Throw:
                    throw new InvalidOperationException(
                        $"No validator is registered for parameter '{parameter.Name}' of type " +
                        $"'{parameter.ParameterType.GetFormattedFullName()}' on action " +
                        $"'{context.ActionDescriptor.DisplayName}'. Register an " +
                        $"IValidator<{parameter.ParameterType.GetFormattedName()}>, or mark the parameter with " +
                        $"[SkipNValidation] to record that it is deliberately not validated.");

                case MissingValidatorBehavior.Ignore:
                default:
                    break;
            }
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "No validator is registered for parameter '{ParameterName}' of type '{ParameterType}' on action '{ActionDisplayName}'.")]
        private partial void LogMissingValidator(string parameterName, string parameterType, string? actionDisplayName);

        private sealed class ActionCache
        {
            public ConcurrentDictionary<string, bool> SkipDecisions { get; } = new(StringComparer.Ordinal);

            /// <summary>
            /// The parameters already reported as having no validator. Whether a payload has one is a
            /// property of the action, not of the request, so warning about it on every request would
            /// bury the warning in its own repetitions.
            /// </summary>
            public ConcurrentDictionary<string, byte> ReportedMissingValidators { get; } = new(StringComparer.Ordinal);

            public ConcurrentDictionary<string, Type> ValidatorServiceTypes { get; } = new(StringComparer.Ordinal);

            /// <summary>
            /// The options each payload is validated with, null where nothing selected rule groups for it.
            /// The null is cached too: a parameter carrying no attribute must not be looked at again.
            /// </summary>
            public ConcurrentDictionary<string, NValidationOptions?> GroupOptions { get; } = new(StringComparer.Ordinal);
        }
    }
}
