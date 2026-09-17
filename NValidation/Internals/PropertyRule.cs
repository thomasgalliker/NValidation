using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NValidation.Internals
{
    internal sealed class PropertyRule<T, TProperty> : IPropertyRule<T>
    {
        private readonly Func<T, TProperty> accessor;
        private readonly List<RuleCheck> checks = [];

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

        public PropertyDisplayNames DisplayNames { get; private set; } = PropertyDisplayNames.None;

        public Func<string>? DisplayName
        {
            get => this.displayName;

            set
            {
                this.ThrowIfFrozen();

                this.displayName = value;
            }
        }

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
        /// What this chain does once one of its rules has failed, where the chain declared it for itself.
        /// null — the default — means it takes whatever the validator resolved.
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

        private Func<T, bool>? Condition { get; set; }

        public bool IsSynchronous { get; private set; } = true;

        private RuleCheck[] Checks => this.frozenChecks ??= [.. this.checks];

        public void Add(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            this.ThrowIfFrozen();

            this.IsSynchronous = false;
            this.checks.Add(new RuleCheck(check, isComposed: false));
        }

        /// <summary>
        /// Appends a rule which judges the value without awaiting. Kept as written rather than wrapped in a
        /// lambda returning a completed ValueTask, because almost every rule is one of these and the wrapper
        /// would cost a delegate call and a ValueTask per rule per run.
        /// </summary>
        public void Add(Action<RuleContext<T, TProperty>> check)
        {
            this.ThrowIfFrozen();

            this.checks.Add(new RuleCheck(check, isComposed: false));
        }

        public void AddComposed(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check)
        {
            this.ThrowIfFrozen();

            this.IsSynchronous = false;
            this.checks.Add(new RuleCheck(check, isComposed: true));
        }

        public void AddComposed(Action<RuleContext<T, TProperty>> check)
        {
            this.ThrowIfFrozen();

            this.checks.Add(new RuleCheck(check, isComposed: true));
        }

        /// <summary>
        /// Applies to the rule added last, not to the chain.
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
        /// Applies to the rule added last. The code is both what the failure reports and the key its message
        /// is resolved under.
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
        /// Narrows when the chain applies. Several conditions combine, so each one can only make the chain
        /// apply less often.
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
        /// Runs a chain whose every rule judges rather than awaits. The twin of ValidateAwaitingAsync: the
        /// same loop with the one branch that can suspend. Two loops because sharing the body would make this
        /// an async method, which is the state machine it exists to avoid. A change to the cascade rule is
        /// made to both.
        /// </summary>
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

        // The property's value, or false where a condition says the chain does not apply. The condition is
        // asked before the property is read: it is what guards a chain whose path is only reachable when
        // the condition holds.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadValue(ValidationFrame<T> frame, [MaybeNullWhen(false)] out TProperty value)
        {
            if (this.Condition != null && !this.Condition(frame.Instance))
            {
                value = default;

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

        private void ThrowIfFrozen()
        {
            if (this.frozenChecks != null)
            {
                throw new InvalidOperationException(
                    $"The rules for '{this.PropertyName}' have already been used to validate, so they can no " +
                    "longer change. Declare every rule before the first validation, typically in the constructor.");
            }
        }

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
            /// The body of a rule which awaits. Exactly one of this and SyncCheck is set.
            /// </summary>
            public Func<RuleContext<T, TProperty>, CancellationToken, ValueTask>? Check { get; }

            /// <summary>
            /// The body of a rule which judges rather than awaits. Exactly one of this and Check is set.
            /// </summary>
            public Action<RuleContext<T, TProperty>>? SyncCheck { get; }

            public Func<T, TProperty, string>? Message { get; set; }

            public string? ErrorCodeOverride { get; set; }

            /// <summary>
            /// Whether this check runs a validator the chain composed, whose failures are its own and which
            /// WithMessage and WithErrorCode therefore cannot follow.
            /// </summary>
            public bool IsComposed { get; }
        }
    }
}
