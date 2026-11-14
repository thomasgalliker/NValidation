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

        private string[]? groups;

        /// <summary>
        /// Every condition, folded in declaration order: a <c>Func&lt;T, bool&gt;</c> while none of them reads
        /// the caller's data, and a <c>Func&lt;T, ValidationData?, bool&gt;</c> with the plain ones folded in
        /// once one does, so the order they are asked in never changes. One field, so a chain without a
        /// condition pays one test.
        /// </summary>
        private Delegate? condition;

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

        /// <summary>
        /// The groups this chain was put in, as declared, or null where it was put in none and is therefore
        /// in the default group alone.
        /// </summary>
        public string[]? Groups => this.groups;

        public string ReportedName => this.propertyNameOverride ?? this.PropertyName;

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

        /// <summary>
        /// Appends a check which hands the value to another validator, and the groups that validator
        /// declares, through which a selection leaving the default group out still reaches this check.
        /// </summary>
        public void AddComposed(Func<RuleContext<T, TProperty>, CancellationToken, ValueTask> check, string[]? composedGroups)
        {
            this.ThrowIfFrozen();

            this.IsSynchronous = false;
            this.checks.Add(new RuleCheck(check, isComposed: true) { ComposedGroups = NullIfEmpty(composedGroups) });
        }

        public void AddComposed(Action<RuleContext<T, TProperty>> check, string[]? composedGroups)
        {
            this.ThrowIfFrozen();

            this.checks.Add(new RuleCheck(check, isComposed: true) { ComposedGroups = NullIfEmpty(composedGroups) });
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
        /// Puts the chain in the given groups, so it runs only when a validation selects one of them. A
        /// group it is already in is not added twice.
        /// </summary>
        public void AddGroups(ReadOnlySpan<string> groups, string paramName)
        {
            this.ThrowIfFrozen();

            this.groups = GroupNames.Union(this.groups, groups, paramName);
        }

        /// <summary>
        /// The same for the groups of the block the chain was declared inside, which are already checked
        /// and are shared with every other chain of that block rather than copied per chain. Nothing
        /// mutates an array of groups once it is set, which is what makes sharing it safe.
        /// </summary>
        public void JoinGroups(string[] groups)
        {
            this.ThrowIfFrozen();

            this.groups = this.groups is null ? groups : GroupNames.Union(this.groups, groups, nameof(groups));
        }

        /// <summary>
        /// Narrows when the chain applies. Several conditions combine, so each one can only make the chain
        /// apply less often.
        /// </summary>
        public void AddCondition(Func<T, bool> condition)
        {
            this.ThrowIfFrozen();

            this.condition = this.condition switch
            {
                null => condition,
                Func<T, bool> existing => (Func<T, bool>)(instance => existing(instance) && condition(instance)),
                var existing => (Func<T, ValidationData?, bool>)((instance, data) =>
                    ((Func<T, ValidationData?, bool>)existing)(instance, data) && condition(instance)),
            };
        }

        /// <summary>
        /// Narrows when the chain applies by something the caller handed over rather than something the
        /// object holds.
        /// </summary>
        public void AddCondition(Func<T, ValidationData?, bool> condition)
        {
            this.ThrowIfFrozen();

            this.condition = this.condition switch
            {
                null => condition,
                Func<T, bool> existing => (Func<T, ValidationData?, bool>)((instance, data) =>
                    existing(instance) && condition(instance, data)),
                var existing => (Func<T, ValidationData?, bool>)((instance, data) =>
                    ((Func<T, ValidationData?, bool>)existing)(instance, data) && condition(instance, data)),
            };
        }

        public void Freeze(PropertyDisplayNames displayNames)
        {
            this.DisplayNames = displayNames;

            this.frozenChecks ??= [.. this.checks];
        }

        public string[]? GetComposedGroups()
        {
            string[]? composedGroups = null;

            foreach (var check in this.Checks)
            {
                composedGroups = GroupNames.Merge(composedGroups, check.ComposedGroups);
            }

            return composedGroups;
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
        /// made to both, and to the ValidateComposed twins.
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

        public ValueTask ValidateComposedAsync(
            ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior, ValidationGroups? selection)
        {
            if (this.IsSynchronous)
            {
                this.ValidateComposed(frame, messageProvider, propertyBehavior, selection);

                return default;
            }

            return this.ValidateComposedAwaitingAsync(frame, messageProvider, propertyBehavior, selection);
        }

        /// <summary>
        /// The loop of <see cref="Validate"/>, running only the checks which hand the value to another
        /// validator — those declaring one of <paramref name="selection"/>'s groups, or every one where it is
        /// null. Kept apart rather than branched inside that loop, so a chain run in full pays nothing per
        /// check for it; a change to the cascade rule is made to all four loops.
        /// </summary>
        public void ValidateComposed(
            ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior, ValidationGroups? selection)
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

                if (!ruleCheck.IsComposed || (selection != null && !selection.Selects(ruleCheck.ComposedGroups)))
                {
                    continue;
                }

                ruleCheck.SyncCheck!(this.CreateContext(frame, messageProvider, value, errorCountAtStart, ruleCheck));
            }
        }

        private async ValueTask ValidateComposedAwaitingAsync(
            ValidationFrame<T> frame, IValidationMessageProvider messageProvider, ValidationBehavior propertyBehavior, ValidationGroups? selection)
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

                if (!ruleCheck.IsComposed || (selection != null && !selection.Selects(ruleCheck.ComposedGroups)))
                {
                    continue;
                }

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

        private static string[]? NullIfEmpty(string[]? groups)
        {
            return groups is { Length: > 0 } ? groups : null;
        }

        // The property's value, or false where a condition says the chain does not apply. The condition is
        // asked before the property is read: it is what guards a chain whose path is only reachable when
        // the condition holds.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadValue(ValidationFrame<T> frame, [MaybeNullWhen(false)] out TProperty value)
        {
            if (this.condition is { } condition && !ConditionHolds(condition, frame))
            {
                value = default;

                return false;
            }

            value = this.accessor(frame.Instance);

            return true;
        }

        /// <summary>
        /// Kept out of line, so a chain without a condition pays one test and <see cref="Validate"/> stays
        /// small enough for the loop calling it to inline it.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ConditionHolds(Delegate condition, ValidationFrame<T> frame)
        {
            return condition is Func<T, bool> plain
                ? plain(frame.Instance)
                : ((Func<T, ValidationData?, bool>)condition)(frame.Instance, frame.Run.Inherited.Inputs.Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private RuleContext<T, TProperty> CreateContext(
            ValidationFrame<T> frame, IValidationMessageProvider messageProvider, TProperty value, int errorCountAtStart, RuleCheck ruleCheck)
        {
            return new RuleContext<T, TProperty>(frame, messageProvider, this, ruleCheck, value, errorCountAtStart);
        }

        public void ThrowIfFrozen()
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

            /// <summary>
            /// The groups the validator this check runs declares; null for a check which runs none, or one
            /// which declares no group.
            /// </summary>
            public string[]? ComposedGroups { get; init; }
        }
    }
}
