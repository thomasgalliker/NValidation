using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// What a rule sees while it runs: the value under test, the instance it came from (for rules which
    /// compare against another property), the property name to report under, and the message provider.
    /// Rules add their failures here instead of returning them, so a chain can report several.
    /// </summary>
    /// <remarks>
    /// A <c>readonly struct</c> of five references and a count, over the <see cref="ValidationFrame{T}"/>
    /// of the pass, so running a chain allocates nothing. A fresh one is built for each rule rather than
    /// one being mutated between them, which is what confines <c>WithMessage</c> and <c>WithErrorCode</c>
    /// to the rule they follow.
    /// </remarks>
    public readonly struct RuleContext<T, TProperty>
    {
        private readonly ValidationFrame<T> frame;
        private readonly IValidationMessageProvider messageProvider;
        private readonly PropertyRule<T, TProperty> rule;
        private readonly PropertyRule<T, TProperty>.RuleCheck check;
        private readonly int errorCountAtStart;

        internal RuleContext(
            ValidationFrame<T> frame,
            IValidationMessageProvider messageProvider,
            PropertyRule<T, TProperty> rule,
            PropertyRule<T, TProperty>.RuleCheck check,
            TProperty value,
            int errorCountAtStart)
        {
            this.frame = frame;
            this.messageProvider = messageProvider;
            this.rule = rule;
            this.check = check;
            this.Value = value;
            this.errorCountAtStart = errorCountAtStart;
        }

        internal ValidationRun Run => this.frame.Run;

        internal ValidationFrame Frame => this.frame;

        /// <summary>
        /// The object being validated. Rules which compare two properties read the other one from here.
        /// </summary>
        public T Instance => this.frame.Instance;

        /// <summary>
        /// The value of the property this chain was declared for.
        /// </summary>
        public TProperty Value { get; }

        /// <summary>
        /// What failures of this property are reported under: the property name it opted into with
        /// <c>WithPropertyName(...)</c>, or — the default — the member path of the expression it was
        /// declared with (<c>Name</c>, or <c>Address.Street</c> for a nested one).
        /// </summary>
        public string PropertyName => this.rule.PropertyNameOverride ?? this.rule.PropertyName;

        /// <summary>
        /// Where a rule takes its message texts from, resolved for this run. A rule which reports through
        /// <see cref="AddError(string, ReadOnlySpan{ValueTuple{string, object}})"/> never needs this; one
        /// which builds a <see cref="ValidationError"/> of its own resolves the wording here.
        /// </summary>
        public IValidationMessageProvider ValidationMessageProvider => this.messageProvider;

        /// <summary>
        /// The token the validation was started with, for a rule which does something cancellable.
        /// </summary>
        public CancellationToken CancellationToken => this.frame.Run.CancellationToken;

        /// <summary>
        /// What a message calls this property: the display name it opted into with
        /// <c>WithDisplayName(...)</c>, or its <see cref="PropertyName"/>. Only the message is affected;
        /// the error is always reported under the property name.
        /// </summary>
        public string DisplayName => this.rule.DisplayNames.Resolve(this.rule.PropertyName);

        /// <summary>
        /// <c>true</c> once a rule in this chain has failed. Used to stop the chain unless it opted out.
        /// </summary>
        public bool HasFailed => this.frame.ErrorCount > this.errorCountAtStart;

        /// <summary>
        /// Reports a failure of this property. The message is resolved from the property's
        /// <see cref="DisplayName"/>, passed as <see cref="ValidationMessagePlaceholders.PropertyName"/>,
        /// and the rule's own named <paramref name="arguments"/>, unless the rule was given a message of
        /// its own with <c>WithMessage</c>. Inside a <c>ForEach</c> the entry's position is added as
        /// <see cref="ValidationMessagePlaceholders.CollectionIndex"/>.
        /// </summary>
        public void AddError(string errorCode, params ReadOnlySpan<(string Name, object? Value)> arguments)
        {
            // A code of the chain's own replaces the rule's, and is also what the message is resolved
            // under: one token says which rule failed and which text says so.
            var reportedErrorCode = this.check.ErrorCodeOverride ?? errorCode;
            var messageArguments = ValidationMessageProviderExtensions.BuildArguments(this.DisplayName, arguments);

            this.frame.Run.Scope?.Enrich(messageArguments);

            // A message of the rule's own is a template like any other, substituted against exactly the
            // arguments the rule supplies.
            var message = this.check.Message is { } messageOverride
                ? ValidationMessageFormatter.Format(messageOverride(this.Instance, this.Value), messageArguments)
                : this.messageProvider.GetMessage(reportedErrorCode, messageArguments);

            this.frame.Errors.Add(new ValidationError(this.PropertyName, message, reportedErrorCode, messageArguments));
        }

        /// <summary>
        /// Reports a failure with a property name of the rule's choosing — used by rules which report per
        /// element, or which merge the errors of a nested validator. The property name is kept even when
        /// the rule was given a message of its own.
        /// </summary>
        public void AddError(ValidationError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            var messageOverride = this.check.Message;
            var errorCodeOverride = this.check.ErrorCodeOverride;

            if (messageOverride == null && errorCodeOverride == null)
            {
                this.frame.Errors.Add(error);
                return;
            }

            var message = error.Message;

            if (messageOverride != null)
            {
                var messageArguments = ValidationMessageProviderExtensions.BuildArguments(this.DisplayName, arguments: default);

                this.frame.Run.Scope?.Enrich(messageArguments);

                message = ValidationMessageFormatter.Format(messageOverride(this.Instance, this.Value), messageArguments);
            }

            this.frame.Errors.Add(new ValidationError(
                error.PropertyName, message, errorCodeOverride ?? error.ErrorCode, error.Arguments));
        }

        /// <summary>
        /// What a message calls another property of the same object — the one a value is compared
        /// against, typically. Falls back to <paramref name="propertyName"/> when that property declared
        /// no display name.
        /// </summary>
        public string GetDisplayName(string propertyName)
        {
            ArgumentNullException.ThrowIfNull(propertyName);

            return this.rule.DisplayNames.Resolve(propertyName);
        }

        internal void AddComposedError(ValidationError error)
        {
            this.frame.Errors.Add(error);
        }
    }
}
