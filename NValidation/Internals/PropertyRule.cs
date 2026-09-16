namespace NValidation.Internals
{
    internal sealed class PropertyRule<T, TProperty> : IPropertyRule<T>
    {
        private readonly Func<T, TProperty> accessor;
        private readonly List<RuleCheck> checks = [];

        public PropertyRule(string propertyName, Func<T, TProperty> accessor)
        {
            this.PropertyName = propertyName;
            this.accessor = accessor;
        }

        public string PropertyName { get; }

        /// <summary>
        /// What messages call this property instead of its property name. <c>null</c> until the chain opts in.
        /// </summary>
        public Func<string>? DisplayName { get; set; }

        /// <summary>
        /// What failures of this property are reported under instead of its member path. <c>null</c>
        /// until the chain opts in.
        /// </summary>
        public string? PropertyNameOverride { get; set; }

        /// <summary>
        /// What this chain does once one of its rules has failed, where the chain declared it for
        /// itself. <c>null</c> — the default — means it takes whatever the validator resolved.
        /// </summary>
        public ValidationBehavior? ValidationBehaviorOverride { get; set; }

        /// <summary>
        /// Decides whether this property is validated at all. <c>null</c> means always.
        /// </summary>
        private Func<T, bool>? Condition { get; set; }

        public void Add(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            this.checks.Add(new RuleCheck(check));
        }

        /// <summary>
        /// The same, for a rule which has nothing to await.
        /// </summary>
        /// <remarks>
        /// Kept as it was written rather than wrapped in a lambda returning a completed
        /// <see cref="ValueTask"/>. The wrapper cost a closure and a delegate for every rule of every
        /// validator built, and a delegate call plus a <see cref="ValueTask"/> round trip for every rule
        /// of every validation — and almost every rule this library ships is synchronous.
        /// </remarks>
        public void Add(Action<RuleContext<T, TProperty>> check)
        {
            this.checks.Add(new RuleCheck(check));
        }

        /// <summary>
        /// The same, for a check which runs a validator this chain composed rather than judging the
        /// value itself.
        /// </summary>
        public void AddComposed(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            this.checks.Add(new RuleCheck(check) { IsComposed = true });
        }

        /// <summary>
        /// Gives the rule which was added last a message of its own, replacing the one its error code
        /// would have produced.
        /// </summary>
        public void SetMessageOfLastCheck(Func<T, TProperty, string> message)
        {
            if (this.checks.Count == 0)
            {
                throw new InvalidOperationException(
                    "WithMessage must follow a rule, e.g. this.Property(x => x.Name).NotEmpty().WithMessage(\"...\").");
            }

            if (this.checks[^1].IsComposed)
            {
                throw new InvalidOperationException(
                    "WithMessage cannot replace the messages a composed validator reported: it found " +
                    "several things, each under its own name, and one wording cannot stand for all of " +
                    "them. Put the wording on the rules of that validator, or write a rule of your own.");
            }

            this.checks[^1].Message = message;
        }

        /// <summary>
        /// Gives the rule which was added last an error code of its own, which is both what the failure
        /// reports and the key its message is resolved under.
        /// </summary>
        public void SetErrorCodeOfLastCheck(string errorCode)
        {
            if (this.checks.Count == 0)
            {
                throw new InvalidOperationException(
                    "WithErrorCode must follow a rule, e.g. this.Property(x => x.Name).NotEmpty().WithErrorCode(\"...\").");
            }

            if (this.checks[^1].IsComposed)
            {
                throw new InvalidOperationException(
                    "WithErrorCode cannot replace the codes a composed validator reported: it found " +
                    "several things, each under its own code. Put the code on the rules of that " +
                    "validator, or write a rule of your own.");
            }

            this.checks[^1].ErrorCodeOverride = errorCode;
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
            ValidationBehavior propertyBehavior,
            RequestedBehaviors requested,
            CancellationToken cancellationToken)
        {
            var context = this.CreateContext(instance, errors, messages, displayNames, requested);

            if (context == null)
            {
                return;
            }

            var behavior = this.ValidationBehaviorOverride ?? propertyBehavior;

            foreach (var ruleCheck in this.checks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (behavior == ValidationBehavior.StopAtFirstError && context.HasFailed)
                {
                    return;
                }

                context.UseOverrides(ruleCheck.Message, ruleCheck.ErrorCodeOverride);

                if (ruleCheck.SyncCheck is { } syncCheck)
                {
                    syncCheck(context);
                }
                else
                {
                    await ruleCheck.Check!(context, cancellationToken);
                }
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
            PropertyDisplayNames displayNames,
            RequestedBehaviors requested)
        {
            if (this.Condition != null && !this.Condition(instance))
            {
                return null;
            }

            return new RuleContext<T, TProperty>(
                instance, this.accessor(instance), this.PropertyName, this.PropertyNameOverride, messages, displayNames, errors, requested);
        }

        private sealed class RuleCheck
        {
            public RuleCheck(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
            {
                this.Check = check;
            }

            public RuleCheck(Action<RuleContext<T, TProperty>> syncCheck)
            {
                this.SyncCheck = syncCheck;
            }

            /// <summary>
            /// Set for a rule which has something to await; <see cref="SyncCheck"/> is set instead for
            /// one which has not. Exactly one of the two is ever set.
            /// </summary>
            public Func<RuleContext<T, TProperty>, CancellationToken, ValueTask>? Check { get; }

            /// <inheritdoc cref="Check"/>
            public Action<RuleContext<T, TProperty>>? SyncCheck { get; }

            public Func<T, TProperty, string>? Message { get; set; }

            /// <summary>
            /// What this rule reports instead of its own code, and resolves its message under.
            /// </summary>
            public string? ErrorCodeOverride { get; set; }

            /// <summary>
            /// Whether this check runs a validator the chain composed, whose failures are its own.
            /// </summary>
            public bool IsComposed { get; init; }
        }
    }
}
