using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// What a rule sees while it runs: the value under test, the instance it came from (for rules which
    /// compare against another property), the error code to report under, and the message provider.
    /// Rules add their failures here instead of returning them, so a chain can report several.
    /// </summary>
    public sealed class RuleContext<T, TProperty>
    {
        private readonly List<ValidationError> errors;
        private readonly int errorCountAtStart;
        private readonly PropertyDisplayNames displayNames;
        private Func<string>? messageOverride;

        private readonly string propertyPath;

        internal RuleContext(
            T instance,
            TProperty value,
            string propertyPath,
            string? errorCode,
            IValidationMessageProvider messages,
            PropertyDisplayNames displayNames,
            List<ValidationError> errors)
        {
            this.Instance = instance;
            this.Value = value;
            this.propertyPath = propertyPath;
            this.Code = errorCode ?? propertyPath;
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
        /// What failures of this property are reported under: the code it opted into with
        /// <c>WithErrorCode(...)</c>, or — the default — the member path of the expression it was declared
        /// with (<c>Name</c>, or <c>Address.Street</c> for a nested one).
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Where a rule takes its message texts from. A rule which reports through
        /// <see cref="AddError(string, ValueTuple{string, object}[])"/> never needs this; one which
        /// builds a <see cref="ValidationError"/> of its own resolves the wording here.
        /// </summary>
        public IValidationMessageProvider Messages { get; }

        /// <summary>
        /// What a message calls this property: the display name it opted into with <c>WithDisplayName(...)</c>,
        /// or its <see cref="Code"/>. Only the message is affected — the error is always reported under
        /// the code.
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
        public void AddError(string messageKey, params (string Name, object? Value)[] arguments)
        {
            if (this.messageOverride != null)
            {
                this.errors.Add(new ValidationError(this.Code, this.messageOverride()));
                return;
            }

            this.errors.Add(new ValidationError(this.Code, this.Messages.GetMessage(messageKey, this.DisplayName, arguments)));
        }

        /// <summary>
        /// Reports a failure with a code of the rule's choosing — used by rules which report per element
        /// (one error per item of a collection, say) or which merge the errors of a nested validator. The
        /// code is kept even when the rule was given a message of its own.
        /// </summary>
        public void AddError(ValidationError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            this.errors.Add(this.messageOverride == null
                ? error
                : new ValidationError(error.Code, this.messageOverride()));
        }

        /// <summary>
        /// What a message calls another property of the same object — the one a value is compared
        /// against, typically. Falls back to <paramref name="code"/> when that property declared no
        /// display name.
        /// </summary>
        public string GetDisplayName(string code)
        {
            ArgumentNullException.ThrowIfNull(code);

            return this.displayNames.Resolve(code);
        }

        /// <summary>
        /// Applied by the rule chain before each rule runs, so a message set with <c>WithMessage</c>
        /// only affects the rule it was written after.
        /// </summary>
        internal void UseMessageOverride(Func<string>? messageOverride)
        {
            this.messageOverride = messageOverride;
        }
    }
}
