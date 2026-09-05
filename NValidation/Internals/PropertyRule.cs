namespace NValidation.Internals
{
    internal sealed class PropertyRule<T, TProperty> : IPropertyRule<T>
    {
        private readonly Func<T, TProperty> accessor;
        private readonly List<RuleCheck> checks = [];

        public PropertyRule(string code, Func<T, TProperty> accessor)
        {
            this.Code = code;
            this.accessor = accessor;
        }

        public string Code { get; }

        /// <summary>
        /// What messages call this property instead of its code. <c>null</c> until the chain opts in.
        /// </summary>
        public Func<string>? DisplayName { get; set; }

        /// <summary>
        /// What failures of this property are reported under instead of its member path. <c>null</c>
        /// until the chain opts in.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// When <c>false</c> (the default) the chain stops at its first failing rule, so a property
        /// reports at most one message.
        /// </summary>
        public bool ContinueOnFailure { get; set; }

        /// <summary>
        /// Decides whether this property is validated at all. <c>null</c> means always.
        /// </summary>
        private Func<T, bool>? Condition { get; set; }

        public void Add(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            this.checks.Add(new RuleCheck(check));
        }

        /// <summary>
        /// Gives the rule which was added last a message of its own, replacing the one its message key
        /// would have produced.
        /// </summary>
        public void SetMessageOfLastCheck(Func<string> message)
        {
            if (this.checks.Count == 0)
            {
                throw new InvalidOperationException(
                    "WithMessage must follow a rule, e.g. this.Property(x => x.Name).NotEmpty().WithMessage(\"...\").");
            }

            this.checks[^1].Message = message;
        }

        /// <summary>
        /// Narrows when the chain applies. Several conditions combine, so each one can only make the
        /// chain apply less often.
        /// </summary>
        public void AddCondition(Func<T, bool> condition)
        {
            var existingCondition = this.Condition;

            this.Condition = existingCondition == null
                ? condition
                : instance => existingCondition(instance) && condition(instance);
        }

        public async ValueTask ValidateAsync(
            T instance,
            List<ValidationError> errors,
            IValidationMessageProvider messages,
            PropertyDisplayNames displayNames,
            CancellationToken cancellationToken)
        {
            var context = this.CreateContext(instance, errors, messages, displayNames);

            if (context == null)
            {
                return;
            }

            foreach (var ruleCheck in this.checks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!this.ContinueOnFailure && context.HasFailed)
                {
                    return;
                }

                context.UseMessageOverride(ruleCheck.Message);

                await ruleCheck.Check(context, cancellationToken);
            }
        }

        /// <summary>
        /// The state one run of this chain works against, or <c>null</c> where a condition says the
        /// chain does not apply.
        /// </summary>
        /// <remarks>
        /// The condition is asked before the property is read: it is what guards a chain whose path is
        /// only reachable in the first place when the condition holds (e.g. a nested property of an
        /// object which may be absent).
        /// </remarks>
        private RuleContext<T, TProperty>? CreateContext(
            T instance,
            List<ValidationError> errors,
            IValidationMessageProvider messages,
            PropertyDisplayNames displayNames)
        {
            if (this.Condition != null && !this.Condition(instance))
            {
                return null;
            }

            return new RuleContext<T, TProperty>(
                instance, this.accessor(instance), this.Code, this.ErrorCode, messages, displayNames, errors);
        }

        private sealed class RuleCheck
        {
            public RuleCheck(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
            {
                this.Check = check;
            }

            public Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> Check { get; }

            public Func<string>? Message { get; set; }
        }
    }
}
