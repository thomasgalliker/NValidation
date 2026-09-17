using System.Runtime.CompilerServices;

namespace NValidation.Internals
{
    internal sealed class PropertyRule<T, TProperty> : IPropertyRule<T>
    {
        private readonly Func<T, TProperty> accessor;
        private readonly List<RuleCheck> checks = [];

        // The checks as an array, taken when the validator freezes and never re-read.
        private RuleCheck[]? frozenChecks;


        private Func<string>? displayName;

        private string? propertyNameOverride;

        private ValidationBehavior? validationBehaviorOverride;

        public PropertyRule(string propertyName, Func<T, TProperty> accessor)
        {
            this.PropertyName = propertyName;
            this.accessor = accessor;
        }

        public string PropertyName { get; }

        /// <summary>
        /// The display names of every property of the validator, handed over when it freezes.
        /// </summary>
        public PropertyDisplayNames DisplayNames { get; private set; } = PropertyDisplayNames.None;

        /// <summary>
        /// What messages call this property instead of its property name. <c>null</c> until the chain opts in.
        /// </summary>
        public Func<string>? DisplayName
        {
            get => this.displayName;

            set
            {
                this.ThrowIfFrozen();

                this.displayName = value;
            }
        }

        /// <summary>
        /// What failures of this property are reported under instead of its member path. <c>null</c>
        /// until the chain opts in.
        /// </summary>
        public string? PropertyNameOverride
        {
            get => this.propertyNameOverride;

            set
            {
                this.ThrowIfFrozen();

                this.propertyNameOverride = value;
            }
        }

        /// <summary>
        /// What this chain does once one of its rules has failed, where the chain declared it for
        /// itself. <c>null</c> — the default — means it takes whatever the validator resolved.
        /// </summary>
        public ValidationBehavior? ValidationBehaviorOverride
        {
            get => this.validationBehaviorOverride;

            set
            {
                this.ThrowIfFrozen();

                this.validationBehaviorOverride = value;
            }
        }

        /// <summary>
        /// Decides whether this property is validated at all. <c>null</c> means always.
        /// </summary>
        private Func<T, bool>? Condition { get; set; }

        /// <inheritdoc/>
        public bool IsSynchronous { get; private set; } = true;

        private RuleCheck[] Checks => this.frozenChecks ??= [.. this.checks];

        /// <summary>
        /// Appends a rule which has something to await.
        /// </summary>
        public void Add(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            this.ThrowIfFrozen();

            this.IsSynchronous = false;
            this.checks.Add(new RuleCheck(check, isComposed: false));
        }

        /// <summary>
        /// Appends a rule which judges the value without awaiting. Kept as written rather than wrapped
        /// in a lambda returning a completed <see cref="ValueTask"/>, because almost every rule is one of
        /// these and the wrapper would cost a delegate call and a <see cref="ValueTask"/> per rule per run.
        /// </summary>
        public void Add(Action<RuleContext<T, TProperty>> check)
        {
            this.ThrowIfFrozen();

            this.checks.Add(new RuleCheck(check, isComposed: false));
        }

        /// <summary>
        /// Appends a check which runs a validator this chain composed rather than judging the value
        /// itself; the composed validator awaits something.
        /// </summary>
        public void AddComposed(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            this.ThrowIfFrozen();

            this.IsSynchronous = false;
            this.checks.Add(new RuleCheck(check, isComposed: true));
        }

        /// <summary>
        /// The same, for a composed validator whose every rule judges rather than awaits, so this chain
        /// stays synchronous.
        /// </summary>
        public void AddComposed(Action<RuleContext<T, TProperty>> check)
        {
            this.ThrowIfFrozen();

            this.checks.Add(new RuleCheck(check, isComposed: true));
        }

        /// <summary>
        /// Gives the rule which was added last a message of its own, replacing the one its error code
        /// would have produced.
        /// </summary>
        public void SetMessageOfLastCheck(Func<T, TProperty, string> message)
        {
            this.ThrowIfFrozen();

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
            this.ThrowIfFrozen();

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
            this.ThrowIfFrozen();

            var existingCondition = this.Condition;

            this.Condition = existingCondition == null
                ? condition
                : instance => existingCondition(instance) && condition(instance);
        }

        public void Freeze(PropertyDisplayNames displayNames)
        {
            this.DisplayNames = displayNames;
            this.frozenChecks ??= [.. this.checks];
        }

        public ValueTask ValidateAsync(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior)
        {
            if (this.IsSynchronous)
            {
                this.Validate(frame, messageProvider, propertyBehavior);

                return default;
            }

            return this.ValidateAwaitingAsync(frame, messageProvider, propertyBehavior);
        }

        /// <summary>
        /// Runs a chain whose every rule judges rather than awaits.
        /// </summary>
        /// <remarks>
        /// The twin of <see cref="ValidateAwaitingAsync"/>: the same loop with the one branch that can
        /// suspend. Two loops because sharing the body would make this an async method, which is the
        /// state machine it exists to avoid. A change to the cascade rule is made to both.
        /// </remarks>
        public void Validate(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior)
        {
            if (!this.TryReadValue(frame, out var value))
            {
                return;
            }

            var cancellationToken = frame.Run.CancellationToken;
            var errorCountAtStart = frame.ErrorCount;
            var behavior = this.validationBehaviorOverride ?? propertyBehavior;

            var checks = this.Checks;

            for (var i = 0; i < checks.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (behavior == ValidationBehavior.StopAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                var ruleCheck = checks[i];

                ruleCheck.SyncCheck!(this.CreateContext(frame, messageProvider, value, errorCountAtStart, ruleCheck));
            }
        }

        /// <inheritdoc cref="Validate"/>
        private async ValueTask ValidateAwaitingAsync(ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior)
        {
            if (!this.TryReadValue(frame, out var value))
            {
                return;
            }

            var cancellationToken = frame.Run.CancellationToken;
            var errorCountAtStart = frame.ErrorCount;
            var behavior = this.validationBehaviorOverride ?? propertyBehavior;

            var checks = this.Checks;

            for (var i = 0; i < checks.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (behavior == ValidationBehavior.StopAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                var ruleCheck = checks[i];
                var context = this.CreateContext(frame, messageProvider, value, errorCountAtStart, ruleCheck);

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
        /// The property's value, or <c>false</c> where a condition says the chain does not apply. The
        /// condition is asked before the property is read: it is what guards a chain whose path is only
        /// reachable when the condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadValue(ValidationFrame<T> frame, out TProperty value)
        {
            if (this.Condition != null && !this.Condition(frame.Instance))
            {
                value = default!;

                return false;
            }

            value = this.accessor(frame.Instance);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private RuleContext<T, TProperty> CreateContext(
            ValidationFrame<T> frame, IValidationMessageProvider messageProvider, TProperty value, int errorCountAtStart, RuleCheck ruleCheck)
        {
            return new RuleContext<T, TProperty>(frame, messageProvider, this, ruleCheck, value, errorCountAtStart);
        }

        /// <exception cref="InvalidOperationException">The chain has already been used to validate.</exception>
        private void ThrowIfFrozen()
        {
            if (this.frozenChecks != null)
            {
                throw new InvalidOperationException(
                    $"The rules for '{this.PropertyName}' have already been used to validate, so they can no " +
                    "longer change. Declare every rule before the first validation, typically in the constructor.");
            }
        }

        /// <summary>
        /// One rule of the chain: what it runs, and the message and code the chain gave it.
        /// </summary>
        internal sealed class RuleCheck
        {
            public RuleCheck(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check, bool isComposed)
            {
                this.Check = check;
                this.IsComposed = isComposed;
            }

            public RuleCheck(Action<RuleContext<T, TProperty>> syncCheck, bool isComposed)
            {
                this.SyncCheck = syncCheck;
                this.IsComposed = isComposed;
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
            /// Whether this check runs a validator the chain composed, whose failures are its own and
            /// which <c>WithMessage</c> and <c>WithErrorCode</c> therefore cannot follow.
            /// </summary>
            public bool IsComposed { get; }
        }
    }
}
