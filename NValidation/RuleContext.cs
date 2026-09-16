using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// What a rule sees while it runs: the value under test, the instance it came from (for rules which
    /// compare against another property), the property name to report under, and the message provider.
    /// Rules add their failures here instead of returning them, so a chain can report several.
    /// </summary>
    public sealed class RuleContext<T, TProperty>
    {
        private readonly List<ValidationError> errors;
        private readonly int errorCountAtStart;
        private readonly PropertyDisplayNames displayNames;
        private Func<T, TProperty, string>? messageOverride;

        private string? errorCodeOverride;

        private readonly string propertyPath;

        internal RuleContext(
            T instance,
            TProperty value,
            string propertyPath,
            string? propertyNameOverride,
            IValidationMessageProvider messages,
            PropertyDisplayNames displayNames,
            List<ValidationError> errors)
        {
            this.Instance = instance;
            this.Value = value;
            this.propertyPath = propertyPath;
            this.PropertyName = propertyNameOverride ?? propertyPath;
            this.Messages = messages;
            this.displayNames = displayNames;
            this.errors = errors;
            this.errorCountAtStart = errors.Count;
        }

        /// <summary>
        /// The object being validated. Rules which compare two properties read the other one from here.
        /// </summary>
        public T Instance { get; }

        /// <summary>
        /// The value of the property this chain was declared for.
        /// </summary>
        public TProperty Value { get; }

        /// <summary>
        /// What failures of this property are reported under: the property name it opted into with
        /// <c>WithPropertyName(...)</c>, or — the default — the member path of the expression it was declared
        /// with (<c>Name</c>, or <c>Address.Street</c> for a nested one).
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Where a rule takes its message texts from. A rule which reports through
        /// <see cref="AddError(string, ValueTuple{string, object}[])"/> never needs this; one which
        /// builds a <see cref="ValidationError"/> of its own resolves the wording here.
        /// </summary>
        public IValidationMessageProvider Messages { get; }

        /// <summary>
        /// What a message calls this property: the display name it opted into with <c>WithDisplayName(...)</c>,
        /// or its <see cref="PropertyName"/>. Only the message is affected — the error is always reported under
        /// the property name.
        /// </summary>
        public string DisplayName => this.displayNames.Resolve(this.propertyPath);

        /// <summary>
        /// <c>true</c> once a rule in this chain has failed. Used to stop the chain unless it opted out.
        /// </summary>
        public bool HasFailed => this.errors.Count > this.errorCountAtStart;

        /// <summary>
        /// Reports a failure of this property. The message is resolved from the property's
        /// <see cref="DisplayName"/> — passed as <see cref="ValidationMessagePlaceholders.PropertyName"/>
        /// — and the rule's own named
        /// <paramref name="arguments"/>, unless the rule was given a message of its own with
        /// <c>WithMessage</c>. The message uses whichever of them it names and ignores the rest.
        /// </summary>
        public void AddError(string errorCode, params (string Name, object? Value)[] arguments)
        {
            // A code of the chain's own replaces the rule's, and it is also what the message is resolved
            // under: one token says which rule failed and which text says so, and a rule of the caller's
            // own is localized by naming it rather than by carrying a literal.
            var reportedErrorCode = this.errorCodeOverride ?? errorCode;
            var messageArguments = ValidationMessageProviderExtensions.BuildArguments(this.DisplayName, arguments);

            // Completed before the message is produced rather than inside the provider, because a
            // message the chain wrote is formatted here and never reaches the provider at all.
            if (this.Messages is IMessageArgumentEnricher enricher)
            {
                messageArguments = enricher.Enrich(messageArguments);
            }

            // A message of the rule's own is a template like any other, substituted against exactly the
            // arguments the rule supplies. Handing it back unsubstituted would render its braces into
            // the response, and writing {PropertyName} is the first thing anyone tries.
            var message = this.messageOverride == null
                ? this.Messages.GetMessage(reportedErrorCode, messageArguments)
                : ValidationMessageFormatter.Format(this.messageOverride(this.Instance, this.Value), messageArguments);

            this.errors.Add(new ValidationError(this.PropertyName, message, reportedErrorCode, messageArguments));
        }

        /// <summary>
        /// Reports a failure with a property name of the rule's choosing — used by rules which report per
        /// element (one error per item of a collection, say) or which merge the errors of a nested
        /// validator. The property name is kept even when the rule was given a message of its own.
        /// </summary>
        public void AddError(ValidationError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            if (this.messageOverride == null && this.errorCodeOverride == null)
            {
                this.errors.Add(error);
                return;
            }

            var messageArguments = ValidationMessageProviderExtensions.BuildArguments(this.DisplayName);

            this.errors.Add(new ValidationError(
                error.PropertyName,
                this.messageOverride == null
                    ? error.Message
                    : ValidationMessageFormatter.Format(this.messageOverride(this.Instance, this.Value), messageArguments),
                this.errorCodeOverride ?? error.ErrorCode,
                error.Arguments));
        }

        /// <summary>
        /// What a message calls another property of the same object — the one a value is compared
        /// against, typically. Falls back to <paramref name="propertyName"/> when that property declared no
        /// display name.
        /// </summary>
        public string GetDisplayName(string propertyName)
        {
            ArgumentNullException.ThrowIfNull(propertyName);

            return this.displayNames.Resolve(propertyName);
        }

        /// <summary>
        /// Reports a failure a validator this chain composed produced, under the property name that
        /// validator chose.
        /// </summary>
        /// <remarks>
        /// Not subject to <c>WithMessage</c>: what a composed validator found is its own judgement, and
        /// one replacement wording copied across every failure it reported would say the same sentence
        /// under each of their property names. A chain which wants its own wording puts it on its own rules.
        /// </remarks>
        internal void AddComposedError(ValidationError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            this.errors.Add(error);
        }

        /// <summary>
        /// Applied by the rule chain before each rule runs, so a message or a code set with
        /// <c>WithMessage</c> or <c>WithErrorCode</c> only affects the rule it was written after.
        /// </summary>
        internal void UseOverrides(Func<T, TProperty, string>? messageOverride, string? errorCodeOverride)
        {
            this.messageOverride = messageOverride;
            this.errorCodeOverride = errorCodeOverride;
        }
    }
}
