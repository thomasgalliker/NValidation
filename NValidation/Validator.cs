using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// Base class for validators which declare their rules per property in the constructor:
    /// <code>this.Property(x => x.FirstName).NotEmpty().MaximumLength(200);</code>
    /// Implements <see cref="IValidator{T}"/>, so it is registered and called exactly like a validator
    /// written by hand — deriving from this class is a convenience, never a requirement.
    /// </summary>
    public abstract class Validator<T> : IValidator<T>, IValidationRegistrationTarget, IValidationRunAware<T>
    {
        private readonly List<IPropertyRule<T>> rules = [];

        /// <summary>
        /// Everything settled when the rules freeze on first use, published as one object: a pass reads it
        /// once, so it can never see the rules of one freeze beside the groups of another.
        /// </summary>
        private FrozenRules? frozen;

        private NValidationOptions options = NValidationOptions.None;

        private NValidationOptions? registeredOptions;

        /// <summary>
        /// The groups of the <see cref="Group(string, Action)"/> block being declared, which every chain
        /// started inside it joins. Declaration-time state: rules are declared once, before anything
        /// validates, so this is never touched while a validation runs.
        /// </summary>
        private string[]? declaringGroups;

        /// <summary>
        /// The condition of the <see cref="When(Func{T, bool}, Action)"/> blocks being declared, folded from
        /// the outermost in, while none of them reads the caller's data. Declaration-time state, like
        /// <see cref="declaringGroups"/>.
        /// </summary>
        private Func<T, bool>? declaringCondition;

        /// <summary>
        /// The same once one of the blocks reads the caller's data, the plain ones around it folded in.
        /// At most one of the two is set.
        /// </summary>
        private Func<T, ValidationData?, bool>? declaringDataCondition;

        private RootPass? plainPass;

        private RootPass? optionsPass;

        /// <summary>
        /// The options of the last call that passed any, so a second call with the same instance can keep
        /// what it resolves to. Holds on to one caller's options until the next call replaces them.
        /// </summary>
        private NValidationOptions? lastOptions;

        private NestedPass? nestedPass;

        /// <summary>
        /// The settings the last pass this validator was composed into brought without finding them kept, so
        /// the next pass bringing the same keeps them. Holds on to them until another pass replaces them.
        /// </summary>
        private InheritedSettings lastReceived;

        private FrozenRules Frozen => this.frozen ?? this.Freeze();

        /// <summary>
        /// Where this validator's rules take their message texts from, or <c>null</c> — the default — to
        /// inherit: from the options passed to the call or the validator this one is composed into, then
        /// what <c>AddNValidation</c> configured, then <see cref="NValidationOptions.Default"/>, and
        /// failing all of those the built-in English. What is resolved here is in turn inherited by the
        /// validators this one composes, unless they declared otherwise for themselves.
        /// </summary>
        /// <remarks>
        /// Read while validating rather than while the rules are declared, so it can be assigned after
        /// the constructor has run.
        /// </remarks>
        public IValidationMessageProvider? ValidationMessageProvider
        {
            get => this.options.MessageProvider;
            set => this.options = this.options with { MessageProvider = value };
        }

        /// <summary>
        /// How much this validator reports: across its properties, and within one property's chain. An
        /// axis left <c>null</c> inherits on the same ladder as <see cref="ValidationMessageProvider"/>:
        /// <code>this.ValidationBehaviors = new() { Property = ValidationBehavior.All };</code>
        /// </summary>
        /// <inheritdoc cref="ValidationMessageProvider" path="/remarks"/>
        public ValidationBehaviors ValidationBehaviors
        {
            get => this.options.ValidationBehaviors;
            set => this.options = this.options with { ValidationBehaviors = value };
        }

        /// <inheritdoc/>
        NValidationOptions IValidationRegistrationTarget.Options
        {
            set => this.registeredOptions = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Whether every chain of this validator judges rather than awaits, so a validation can run without
        /// an async state machine anywhere in it. Settled by the rules on first use.
        /// </summary>
        private bool IsSynchronous => this.Frozen.IsSynchronous;

        /// <summary>
        /// Starts a rule chain for the given property. The property name is taken from the expression's
        /// member path, so <c>x => x.Name</c> reports as <c>Name</c> and <c>x => x.Address.Street</c>
        /// as <c>Address.Street</c>.
        /// </summary>
        /// <remarks>
        /// The chain is always built for the nullable form of the property's type, so a rule is written
        /// once and has to face the fact that the value may be null. A chain declared through an object
        /// the payload may omit — <c>x => x.Address.Street</c> — is skipped rather than dereferenced when
        /// that object is absent; requiring it is a rule of its own.
        /// </remarks>
        /// <exception cref="ArgumentException">The expression does not reach a property through the lambda's own parameter.</exception>
        /// <exception cref="InvalidOperationException">This validator has already validated something.</exception>
        protected PropertyRuleBuilder<T, TProperty?> Property<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);
            this.ThrowIfFrozen();

            var propertyName = PropertyPath.From(expression);
            var rule = new PropertyRule<T, TProperty?>(propertyName, PropertyAccessor.For(propertyName, expression));

            // Added before any condition the chain declares, so it is the first thing asked and the
            // property is never read through something that is not there.
            var isReachable = ReachabilityGuard.For(propertyName, expression);

            if (isReachable != null)
            {
                rule.AddCondition(isReachable);
            }

            return this.Declare(rule);
        }

        /// <summary>
        /// Starts a rule chain for a value that is not a member path — a computed value, a dictionary entry,
        /// an indexer — naming the property and reading it with a plain delegate:
        /// <code>this.Property("Lines.Total", static o => o.Lines.Sum(l => l.Amount)).GreaterThan(0m);</code>
        /// </summary>
        /// <remarks>
        /// The name is written by hand, so renaming the property will not change what the failure is
        /// reported under, and nothing checks that the two agree. The accessor is called as written: a
        /// path through an object the payload may omit needs the overload taking a reachability
        /// predicate, or it dereferences the missing object.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">This validator has already validated something.</exception>
        protected PropertyRuleBuilder<T, TProperty> Property<TProperty>(string propertyName, Func<T, TProperty> accessor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            ArgumentNullException.ThrowIfNull(accessor);
            this.ThrowIfFrozen();

            var rule = new PropertyRule<T, TProperty>(propertyName, accessor);

            return this.Declare(rule);
        }

        /// <summary>
        /// Starts a rule chain for a value reached through something the payload may have omitted:
        /// <code>this.Property("Model.Name", static c => c.Model!.Name, static c => c.Model != null).NotEmpty();</code>
        /// A chain whose <paramref name="isReachable"/> says no is skipped, exactly as a chain declared
        /// through an absent object is.
        /// </summary>
        /// <inheritdoc cref="Property{TProperty}(string, Func{T, TProperty})" path="/exception"/>
        protected PropertyRuleBuilder<T, TProperty> Property<TProperty>(
            string propertyName,
            Func<T, TProperty> accessor,
            Func<T, bool> isReachable)
        {
            ArgumentNullException.ThrowIfNull(isReachable);
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            ArgumentNullException.ThrowIfNull(accessor);
            this.ThrowIfFrozen();

            var rule = new PropertyRule<T, TProperty>(propertyName, accessor);

            // Added before the chain is declared, so it is asked ahead of anything a block or the chain
            // adds, exactly as the guard of an expression is.
            rule.AddCondition(isReachable);

            return this.Declare(rule);
        }

        /// <summary>
        /// Puts every chain declared inside <paramref name="declareRules"/> in <paramref name="group"/>
        /// instead of the default group:
        /// <code>this.Group("Create", () => this.Property(c => c.TradeInValue).NotNull());</code>
        /// </summary>
        /// <remarks>
        /// The blocks nest, and a chain inside a nested one is in the groups of both; <c>WithGroup</c>
        /// written inside adds further groups to the one chain. A chain declared after the block is in
        /// none of its groups, even where the block left through an exception.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="declareRules"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="group"/> is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">This validator has already validated something.</exception>
        protected void Group(string group, Action declareRules)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(group);

            this.Group([group], declareRules);
        }

        /// <summary>
        /// Puts every chain declared inside <paramref name="declareRules"/> in all of
        /// <paramref name="groups"/>, which is written as a collection expression:
        /// <code>this.Group(["Create", "Update"], () => this.Property(c => c.Vin).NotEmpty());</code>
        /// </summary>
        /// <inheritdoc cref="Group(string, Action)" path="/remarks"/>
        /// <exception cref="ArgumentNullException"><paramref name="declareRules"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">No group is named, or a name is empty or whitespace.</exception>
        /// <inheritdoc cref="Group(string, Action)" path="/exception[@cref='T:System.InvalidOperationException']"/>
        protected void Group(ReadOnlySpan<string> groups, Action declareRules)
        {
            ArgumentNullException.ThrowIfNull(declareRules);
            GroupNames.ThrowIfEmpty(groups, nameof(groups));
            this.ThrowIfFrozen();

            var enclosingGroups = this.declaringGroups;

            this.declaringGroups = GroupNames.Union(enclosingGroups, groups, nameof(groups));

            try
            {
                declareRules();
            }
            finally
            {
                this.declaringGroups = enclosingGroups;
            }
        }

        /// <summary>
        /// Applies every chain declared inside <paramref name="declareRules"/> only when
        /// <paramref name="condition"/> holds:
        /// <code>this.When(c => c.Condition == CarCondition.Used, () => this.Property(c => c.IntakeCondition).NotNull());</code>
        /// </summary>
        /// <remarks>
        /// The condition joins each chain's own, ahead of anything the chain adds, and is asked for each of
        /// them, so keep it cheap. Blocks nest, combine with <see cref="Group(string, Action)"/>, and a chain
        /// declared after the block is not narrowed by it, even where the block left through an exception.
        /// The rules of the entries of a <c>ForEach</c> are declared on its own builder, and narrowed by
        /// that builder's <c>When</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="condition"/> or <paramref name="declareRules"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">This validator has already validated something.</exception>
        protected ConditionBlock<T> When(Func<T, bool> condition, Action declareRules)
        {
            ArgumentNullException.ThrowIfNull(condition);
            ArgumentNullException.ThrowIfNull(declareRules);

            this.DeclareWhen(condition, declareRules);

            return new ConditionBlock<T>(this, instance => !condition(instance));
        }

        /// <summary>
        /// Applies every chain declared inside <paramref name="declareRules"/> unless
        /// <paramref name="condition"/> holds.
        /// </summary>
        /// <inheritdoc cref="When(Func{T, bool}, Action)" path="/remarks"/>
        /// <inheritdoc cref="When(Func{T, bool}, Action)" path="/exception"/>
        protected ConditionBlock<T> Unless(Func<T, bool> condition, Action declareRules)
        {
            ArgumentNullException.ThrowIfNull(condition);
            ArgumentNullException.ThrowIfNull(declareRules);

            this.DeclareWhen(instance => !condition(instance), declareRules);

            return new ConditionBlock<T>(this, condition);
        }

        /// <summary>
        /// Applies every chain declared inside <paramref name="declareRules"/> only when the validation was
        /// handed a <typeparamref name="TData"/> and <paramref name="condition"/> holds for it.
        /// </summary>
        /// <remarks>
        /// A validation that hands over no <typeparamref name="TData"/> skips every chain of the block, so
        /// there is no <c>Otherwise</c>: without the data there is neither a yes nor a no.
        /// </remarks>
        /// <inheritdoc cref="When(Func{T, bool}, Action)" path="/exception"/>
        protected void When<TData>(Func<T, TData, bool> condition, Action declareRules)
        {
            ArgumentNullException.ThrowIfNull(condition);
            ArgumentNullException.ThrowIfNull(declareRules);

            this.DeclareWhen(
                (instance, data) => data is not null && data.TryGet<TData>(out var value) && condition(instance, value),
                declareRules);
        }

        internal void DeclareWhen(Func<T, bool> condition, Action declareRules)
        {
            ArgumentNullException.ThrowIfNull(declareRules);
            this.ThrowIfFrozen();

            var enclosingCondition = this.declaringCondition;
            var enclosingDataCondition = this.declaringDataCondition;

            if (enclosingDataCondition != null)
            {
                this.declaringDataCondition = (instance, data) => enclosingDataCondition(instance, data) && condition(instance);
            }
            else
            {
                this.declaringCondition = enclosingCondition == null
                    ? condition
                    : instance => enclosingCondition(instance) && condition(instance);
            }

            this.DeclareBlock(declareRules, enclosingCondition, enclosingDataCondition);
        }

        internal PropertyRuleBuilder<T, T?> RuleForSelf()
        {
            this.ThrowIfFrozen();

            var rule = new PropertyRule<T, T?>(PropertyPath.Self, instance => instance);

            return this.Declare(rule);
        }

        private void DeclareWhen(Func<T, ValidationData?, bool> condition, Action declareRules)
        {
            this.ThrowIfFrozen();

            var enclosingCondition = this.declaringCondition;
            var enclosingDataCondition = this.declaringDataCondition;

            if (enclosingDataCondition != null)
            {
                this.declaringDataCondition = (instance, data) => enclosingDataCondition(instance, data) && condition(instance, data);
            }
            else if (enclosingCondition != null)
            {
                this.declaringDataCondition = (instance, data) => enclosingCondition(instance) && condition(instance, data);
            }
            else
            {
                this.declaringDataCondition = condition;
            }

            this.declaringCondition = null;

            this.DeclareBlock(declareRules, enclosingCondition, enclosingDataCondition);
        }

        private void DeclareBlock(
            Action declareRules, Func<T, bool>? enclosingCondition, Func<T, ValidationData?, bool>? enclosingDataCondition)
        {
            try
            {
                declareRules();
            }
            finally
            {
                this.declaringCondition = enclosingCondition;
                this.declaringDataCondition = enclosingDataCondition;
            }
        }

        /// <summary>
        /// Registers every chain, so one place decides what a chain declared inside a Group or a When block
        /// joins. Its guard is already in place, so the block's condition is asked after it and ahead of
        /// anything the chain adds.
        /// </summary>
        private PropertyRuleBuilder<T, TProperty> Declare<TProperty>(PropertyRule<T, TProperty> rule)
        {
            if (this.declaringGroups is { } groups)
            {
                rule.JoinGroups(groups);
            }

            if (this.declaringDataCondition is { } dataCondition)
            {
                rule.AddCondition(dataCondition);
            }
            else if (this.declaringCondition is { } condition)
            {
                rule.AddCondition(condition);
            }

            this.rules.Add(rule);

            return new PropertyRuleBuilder<T, TProperty>(rule);
        }

        /// <inheritdoc/>
        public ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
        {
            return this.ValidateResolved(instance, new ValidationRun(default, Scope: null, cancellationToken), in this.PlainPass().Resolution);
        }

        /// <inheritdoc/>
        public ValueTask<ValidationResult> ValidateAsync(
            T instance, NValidationOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (this.KeptPassFor(options) is { } pass)
            {
                return this.ValidateResolved(instance, new ValidationRun(default, Scope: null, cancellationToken), in pass.Resolution);
            }

            return this.ValidateResolving(instance, this.RunFor(options, NValidationOptions.Default, cancellationToken), isRoot: true);
        }

        internal ValueTask<ValidationResult> ValidateAsync(T instance, ValidationRun run)
        {
            if (this.IsSynchronous)
            {
                return new ValueTask<ValidationResult>(Result(this.ValidateNested(instance, errors: null, run)));
            }

            return this.ValidateResolving(instance, run, isRoot: false);
        }

        /// <summary>
        /// Runs a pass whose resolution nothing kept: a call with options of its own, or an awaiting validator
        /// composed into another.
        /// </summary>
        private ValueTask<ValidationResult> ValidateResolving(T instance, ValidationRun run, bool isRoot)
        {
            var pass = this.Resolve(run.Inherited, isRoot);

            return this.ValidateResolved(instance, run, in pass);
        }

        private ValueTask<ValidationResult> ValidateResolved(T instance, ValidationRun run, in Resolution pass)
        {
            var frame = this.FrameFor(instance, errors: null, run, in pass);

            if (pass.Gates.Frozen.IsSynchronous)
            {
                this.ValidateFrame(frame, in pass);

                return new ValueTask<ValidationResult>(Result(frame));
            }

            return ValidateAwaitingAsync(frame, ValidateFrameAsync(frame, in pass));
        }

        /// <summary>
        /// Awaits a pass already started, so what waits across the await is the frame and the pass rather
        /// than everything that chose it. Out of line, so a synchronous pass does not carry its state machine.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask<ValidationResult> ValidateAwaitingAsync(ValidationFrame<T> frame, ValueTask pass)
        {
            await pass;

            return Result(frame);
        }

        internal ValidationResult Validate(T instance, ValidationRun run)
        {
            this.RequireSynchronous();

            return Result(this.ValidateNested(instance, errors: null, run));
        }

        internal ValueTask ValidateIntoAsync(T instance, List<ValidationError> errors, ValidationRun run)
        {
            if (this.IsSynchronous)
            {
                this.ValidateNested(instance, errors, run);

                return default;
            }

            var pass = this.Resolve(run.Inherited, isRoot: false);

            return ValidateFrameAsync(this.FrameFor(instance, errors, run, in pass), in pass);
        }

        internal void ValidateInto(T instance, List<ValidationError> errors, ValidationRun run)
        {
            this.RequireSynchronous();

            this.ValidateNested(instance, errors, run);
        }

        /// <summary>
        /// Runs a synchronous pass of a validation this validator is composed into, through what it resolved
        /// for the same settings before where it kept that.
        /// </summary>
        private ValidationFrame<T> ValidateNested(T instance, List<ValidationError>? errors, ValidationRun run)
        {
            if (this.NestedPassFor(run.Inherited) is not { } pass)
            {
                return this.ValidateNestedResolving(instance, errors, run);
            }

            var frame = this.FrameFor(instance, errors, run, in pass.Resolution);

            this.ValidateNestedFrame(frame, in pass.Resolution);

            return frame;
        }

        /// <summary>
        /// Runs a synchronous nested pass nothing kept a resolution for, out of line so the resolution it holds
        /// on its stack costs the kept path nothing.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private ValidationFrame<T> ValidateNestedResolving(T instance, List<ValidationError>? errors, ValidationRun run)
        {
            var pass = this.Resolve(run.Inherited, isRoot: false);
            var frame = this.FrameFor(instance, errors, run, in pass);

            this.ValidateNestedFrame(frame, in pass);

            return frame;
        }

        private ValidationFrame<T> FrameFor(T instance, List<ValidationError>? errors, ValidationRun run, in Resolution pass)
        {
            ArgumentNullException.ThrowIfNull(instance);

            return new ValidationFrame<T>(instance, errors, run with { Inherited = pass.Inherited });
        }

        private static ValidationResult Result(ValidationFrame<T> frame)
        {
            return frame.TryTakeErrors(out var errors)
                ? ValidationResult.FromValidationErrorsInternal(errors)
                : ValidationResult.Success;
        }

        /// <summary>
        /// The run a call starts, with the groups it selects, the data it hands over and the properties it is
        /// limited to resolved once for every pass of it: the options of the call, what the registration
        /// handed this validator, <see cref="NValidationOptions.Default"/>, and failing all of those the
        /// default group alone, no data, and every property.
        /// </summary>
        private ValidationRun RunFor(NValidationOptions? options, NValidationOptions defaults, CancellationToken cancellationToken)
        {
            var registered = this.registeredOptions;

            // Everything below the validator's own word is resolved here, once for the whole run, so no pass
            // walks the rest of the ladder again: a pass lays what its validator declared over this.
            var messageProvider = options?.MessageProvider
                ?? registered?.MessageProvider
                ?? defaults.MessageProvider
                ?? DefaultValidationMessageProvider.Instance;

            var classBehavior = options?.ValidationBehaviors.Class
                ?? registered?.ValidationBehaviors.Class
                ?? defaults.ValidationBehaviors.Class
                ?? ValidationBehavior.All;

            var propertyBehavior = options?.ValidationBehaviors.Property
                ?? registered?.ValidationBehaviors.Property
                ?? defaults.ValidationBehaviors.Property
                ?? ValidationBehavior.StopAtFirstError;

            var groups = options?.ValidationGroups
                ?? registered?.ValidationGroups
                ?? defaults.ValidationGroups
                ?? ValidationGroups.None;

            var data = options?.ValidationData
                ?? registered?.ValidationData
                ?? defaults.ValidationData;

            var properties = options?.ValidationProperties
                ?? registered?.ValidationProperties
                ?? defaults.ValidationProperties;

            var inherited = new InheritedSettings(
                messageProvider, classBehavior, propertyBehavior, RunInputs.Of(groups, data, properties));

            return new ValidationRun(inherited, Scope: null, cancellationToken);
        }

        /// <summary>
        /// What a call without options resolves to. It depends on nothing but
        /// <see cref="NValidationOptions.Default"/>, the registration and what this validator declared, which
        /// all but never change, so it is kept and handed to the next such call as long as all three are the
        /// ones it was resolved from.
        /// </summary>
        private RootPass PlainPass()
        {
            var defaults = NValidationOptions.Default;
            var pass = this.plainPass;

            if (pass != null && pass.IsCurrent(defaults, this.options, this.registeredOptions))
            {
                return pass;
            }

            pass = this.Resolved(options: null, defaults);
            this.plainPass = pass;

            return pass;
        }

        /// <summary>
        /// What options handed over before resolve to, where the same instance arrives again: a caller that
        /// keeps its options — in a static field, or an endpoint's filter caching them — resolves them once.
        /// Options built for one call are never kept, so they allocate nothing for it.
        /// </summary>
        private RootPass? KeptPassFor(NValidationOptions options)
        {
            var defaults = NValidationOptions.Default;
            var pass = this.optionsPass;

            if (pass != null && ReferenceEquals(pass.Options, options) && pass.IsCurrent(defaults, this.options, this.registeredOptions))
            {
                return pass;
            }

            if (!ReferenceEquals(this.lastOptions, options))
            {
                this.lastOptions = options;

                return null;
            }

            pass = this.Resolved(options, defaults);
            this.optionsPass = pass;

            return pass;
        }

        private RootPass Resolved(NValidationOptions? options, NValidationOptions defaults)
        {
            var run = this.RunFor(options, defaults, CancellationToken.None);

            return new RootPass(options, defaults, this.options, this.registeredOptions, this.Resolve(run.Inherited, isRoot: true));
        }

        /// <summary>
        /// The resolution kept for the settings a composer hands this validator, kept on the spot where they
        /// arrive a second time in a row; null where the pass is to resolve its own.
        /// </summary>
        private NestedPass? NestedPassFor(in InheritedSettings received)
        {
            var pass = this.nestedPass;

            return pass != null && pass.IsFor(received, this.options) ? pass : this.KeepNestedPass(received);
        }

        /// <summary>
        /// Keeps what a pass resolves to once the same settings arrive twice in a row, so a parent handing over
        /// something new each time — options built per call — never allocates for this validator.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private NestedPass? KeepNestedPass(in InheritedSettings received)
        {
            // Two passes racing here can read a mix of two settings. That decides only whether a resolution is
            // kept: what is kept is resolved from the pass's own settings, and is found only by those.
            if (!this.lastReceived.IsSameAs(received))
            {
                this.lastReceived = received;

                return null;
            }

            var pass = new NestedPass(received, this.options, this.Resolve(received, isRoot: false));

            this.nestedPass = pass;

            return pass;
        }

        private Resolution Resolve(in InheritedSettings inherited, bool isRoot)
        {
            var declared = this.options;

            // What the pass inherits is already resolved to the end of the ladder — by the call that started
            // the run, or by the validator composing this one — so only this validator's own word is asked.
            var messageProvider = declared.MessageProvider
                ?? inherited.MessageProvider
                ?? DefaultValidationMessageProvider.Instance;

            var classBehavior = declared.ValidationBehaviors.Class
                ?? inherited.Class
                ?? ValidationBehavior.All;

            // A run that stops at the first error stops inside a chain too, or the setting would not do
            // what its name says. A chain which declared something of its own still gets it.
            var propertyBehavior = classBehavior == ValidationBehavior.StopAtFirstError
                ? ValidationBehavior.StopAtFirstError
                : declared.ValidationBehaviors.Property
                    ?? inherited.Property
                    ?? ValidationBehavior.StopAtFirstError;

            // No rung for what the validator declared: the same validator serves every operation, so which
            // of its chains apply is a fact about the call, resolved once where the call started.
            var frozen = this.Frozen;
            var inputs = inherited.Inputs;
            var groups = inputs.Unpack(out var properties);

            if (!groups.IncludesDefault && !groups.Selects(frozen.DeclaredGroups))
            {
                this.ThrowIfTheCallsOwn(frozen, groups, isRoot);

                groups = groups.Additive;
                inputs = inputs.Additive;
            }

            return new Resolution(
                new InheritedSettings(messageProvider, classBehavior, propertyBehavior, inputs),
                new PassSettings(messageProvider, classBehavior, propertyBehavior, groups),
                ChooseLoop(frozen, groups, properties));
        }

        /// <summary>
        /// A validator that declares none of an exclusive selection's groups is validated with its additive
        /// form, so a composed validator which knows nothing of the selection is validated whole. The
        /// validator a call was made on has no such way out: a selection that runs nothing of it is a
        /// mistake, not a pass.
        /// </summary>
        private void ThrowIfTheCallsOwn(FrozenRules frozen, ValidationGroups groups, bool isRoot)
        {
            if (!isRoot)
            {
                return;
            }

            var declared = frozen.DeclaredGroups.Length == 0
                ? "It declares no group at all."
                : $"The groups it declares are: {string.Join(", ", frozen.DeclaredGroups)}.";

            throw new InvalidOperationException(
                $"{this.GetType().GetFormattedFullName()} has no chain in {groups}, so this validation " +
                $"would check nothing. {declared}");
        }

        /// <summary>
        /// Which loop a pass walks, and over which rules. Where every chain runs anyway, and where the
        /// selection is the default group alone — what a call without groups asks for — the loop without a
        /// gate does the same for less, the second over the chains in the default group alone.
        /// </summary>
        private static PassGates ChooseLoop(FrozenRules frozen, ValidationGroups groups, ValidationProperties? properties)
        {
            if (frozen.Chains is not { } chains || groups.RunsEveryChain(frozen.EveryChainInDefault))
            {
                return new PassGates(frozen, frozen.Rules, Chains: null, properties);
            }

            // A pass limited to properties matches each chain by its position, so it keeps every rule.
            if (properties is null && groups.SelectsTheDefaultGroupAlone)
            {
                return new PassGates(frozen, frozen.DefaultRules, Chains: null, properties);
            }

            return new PassGates(frozen, frozen.Rules, chains, properties);
        }

        /// <summary>
        /// The loop of a nested pass, out of line: inlined into a composed chain, it joins the chains that caller
        /// inlines, and the code the JIT builds then depends on which profile it saw first — a collection's
        /// entries run up to a third slower in the processes where it guesses wrong. The root pass inlines it.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ValidateNestedFrame(ValidationFrame<T> frame, in Resolution pass)
        {
            this.ValidateFrame(frame, in pass);
        }

        private void ValidateFrame(ValidationFrame<T> frame, in Resolution pass)
        {
            ref readonly var settings = ref pass.Settings;
            var rules = pass.Gates.Rules;

            if (pass.Gates.Properties is { } properties)
            {
                ValidateSelectedFrame(frame, in settings, rules, in pass.Gates, properties);

                return;
            }

            // A validator whose every chain runs in this pass is walked by the loop it was walked by before
            // groups existed: the gate is asked once here rather than once per chain.
            if (pass.Gates.Chains is { } chains)
            {
                ValidateGroupedFrame(frame, in settings, rules, chains);

                return;
            }

            var cancellationToken = frame.Run.CancellationToken;

            // What this pass found, told apart from what the caller's list already held: an element's
            // inline rules have already reported into it by the time the element's own validator runs.
            var errorCountAtStart = frame.ErrorCount;

            // Read once rather than through the settings per chain, so the loop keeps them in registers.
            var messageProvider = settings.MessageProvider;
            var propertyBehavior = settings.PropertyBehavior;
            var stopsAtFirstError = settings.ClassBehavior == ValidationBehavior.StopAtFirstError;

            for (var i = 0; i < rules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (stopsAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                rules[i].Validate(frame, messageProvider, propertyBehavior);
            }
        }

        /// <summary>
        /// The same loop for a validator whose chains this pass may skip, asking of each whether this run
        /// selected it. Kept apart rather than branched inside the loop, so a validator with no group is
        /// not asked once per chain about a feature it does not use. A change to the cascade rule is made
        /// to both, and to the awaiting twins below.
        /// </summary>
        private static void ValidateGroupedFrame(
            ValidationFrame<T> frame, in PassSettings settings, IPropertyRule<T>[] rules, ChainGates chains)
        {
            var cancellationToken = frame.Run.CancellationToken;
            var errorCountAtStart = frame.ErrorCount;
            var messageProvider = settings.MessageProvider;
            var propertyBehavior = settings.PropertyBehavior;
            var stopsAtFirstError = settings.ClassBehavior == ValidationBehavior.StopAtFirstError;
            var selection = settings.Groups;
            var includesDefault = selection.IncludesDefault;
            var named = chains.Named;

            for (var i = 0; i < rules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (stopsAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                // Asked before the chain's condition and before the property is read, so a chain the run did
                // not select reports nothing and has nothing for a stopping run to stop on. A chain its own
                // groups leave out may still run: in full where it is in the default group too, or only the
                // part that hands its value to a validator declaring one of the selected groups.
                var groups = named[i];
                var gate = (groups is null ? includesDefault : selection.Selects(groups))
                    ? ChainGate.Run
                    : chains.GateOfUnselected(i, selection);

                if (gate == ChainGate.Run)
                {
                    rules[i].Validate(frame, messageProvider, propertyBehavior);
                }
                else if (gate == ChainGate.Composed)
                {
                    rules[i].ValidateComposed(frame, messageProvider, propertyBehavior, selection);
                }
            }
        }

        /// <summary>
        /// Kept out of line: inlined, the state machines of the loops it starts land on the stack of every
        /// synchronous pass through its caller, which then zeroes them on entry whether it awaits or not.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValueTask ValidateFrameAsync(ValidationFrame<T> frame, in Resolution pass)
        {
            var rules = pass.Gates.Rules;

            if (pass.Gates.Properties is { } properties)
            {
                return ValidateSelectedFrameAsync(frame, pass.Settings, rules, pass.Gates, properties);
            }

            return pass.Gates.Chains is { } chains
                ? ValidateGroupedFrameAsync(frame, pass.Settings, rules, chains)
                : ValidateUngroupedFrameAsync(frame, pass.Settings, rules);
        }

        /// <summary>
        /// The loop for a pass limited to some properties, asking of each chain whether the groups and the
        /// properties both let it run, and whether in full or only the part that hands its value on. Kept
        /// apart, so neither other loop pays for a feature it is not asked for.
        /// </summary>
        private static void ValidateSelectedFrame(
            ValidationFrame<T> frame, in PassSettings settings, IPropertyRule<T>[] rules, in PassGates gates, ValidationProperties properties)
        {
            var cancellationToken = frame.Run.CancellationToken;
            var errorCountAtStart = frame.ErrorCount;
            var names = gates.Frozen.ReportedNames;
            var chains = gates.Chains;
            var stopsAtFirstError = settings.ClassBehavior == ValidationBehavior.StopAtFirstError;

            for (var i = 0; i < rules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (stopsAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                var groupGate = chains is null ? ChainGate.Run : chains.Gate(i, settings.Groups);

                if (groupGate == ChainGate.Skip)
                {
                    continue;
                }

                var propertyGate = properties.Match(names[i]);

                if (propertyGate == ChainGate.Skip)
                {
                    continue;
                }

                if (groupGate == ChainGate.Run && propertyGate == ChainGate.Run)
                {
                    rules[i].Validate(frame, settings.MessageProvider, settings.PropertyBehavior);
                }
                else
                {
                    var composedSelection = groupGate == ChainGate.Composed ? settings.Groups : null;

                    rules[i].ValidateComposed(frame, settings.MessageProvider, settings.PropertyBehavior, composedSelection);
                }
            }
        }

        private static async ValueTask ValidateSelectedFrameAsync(
            ValidationFrame<T> frame, PassSettings settings, IPropertyRule<T>[] rules, PassGates gates, ValidationProperties properties)
        {
            var cancellationToken = frame.Run.CancellationToken;
            var errorCountAtStart = frame.ErrorCount;
            var names = gates.Frozen.ReportedNames;
            var chains = gates.Chains;
            var stopsAtFirstError = settings.ClassBehavior == ValidationBehavior.StopAtFirstError;

            for (var i = 0; i < rules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (stopsAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                var groupGate = chains is null ? ChainGate.Run : chains.Gate(i, settings.Groups);

                if (groupGate == ChainGate.Skip)
                {
                    continue;
                }

                var propertyGate = properties.Match(names[i]);

                if (propertyGate == ChainGate.Skip)
                {
                    continue;
                }

                if (groupGate == ChainGate.Run && propertyGate == ChainGate.Run)
                {
                    await rules[i].ValidateAsync(frame, settings.MessageProvider, settings.PropertyBehavior);
                }
                else
                {
                    var composedSelection = groupGate == ChainGate.Composed ? settings.Groups : null;

                    await rules[i].ValidateComposedAsync(frame, settings.MessageProvider, settings.PropertyBehavior, composedSelection);
                }
            }
        }

        private static async ValueTask ValidateUngroupedFrameAsync(
            ValidationFrame<T> frame, PassSettings settings, IPropertyRule<T>[] rules)
        {
            var cancellationToken = frame.Run.CancellationToken;

            var errorCountAtStart = frame.ErrorCount;
            var messageProvider = settings.MessageProvider;
            var propertyBehavior = settings.PropertyBehavior;
            var stopsAtFirstError = settings.ClassBehavior == ValidationBehavior.StopAtFirstError;

            for (var i = 0; i < rules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (stopsAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                await rules[i].ValidateAsync(frame, messageProvider, propertyBehavior);
            }
        }

        private static async ValueTask ValidateGroupedFrameAsync(
            ValidationFrame<T> frame, PassSettings settings, IPropertyRule<T>[] rules, ChainGates chains)
        {
            var cancellationToken = frame.Run.CancellationToken;

            var errorCountAtStart = frame.ErrorCount;
            var messageProvider = settings.MessageProvider;
            var propertyBehavior = settings.PropertyBehavior;
            var stopsAtFirstError = settings.ClassBehavior == ValidationBehavior.StopAtFirstError;
            var selection = settings.Groups;
            var includesDefault = selection.IncludesDefault;
            var named = chains.Named;

            for (var i = 0; i < rules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (stopsAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                var groups = named[i];
                var gate = (groups is null ? includesDefault : selection.Selects(groups))
                    ? ChainGate.Run
                    : chains.GateOfUnselected(i, selection);

                if (gate == ChainGate.Run)
                {
                    await rules[i].ValidateAsync(frame, messageProvider, propertyBehavior);
                }
                else if (gate == ChainGate.Composed)
                {
                    await rules[i].ValidateComposedAsync(frame, messageProvider, propertyBehavior, selection);
                }
            }
        }

        private FrozenRules Freeze()
        {
            var rules = this.rules.ToArray();
            var displayNames = PropertyDisplayNames.For(rules);

            ChainGroups[]? chains = null;
            string[]? declaredGroups = null;
            var everyChainInDefault = true;
            var isSynchronous = true;

            for (var i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];

                rule.Freeze(displayNames);

                isSynchronous &= rule.IsSynchronous;

                var chain = ChainGroups.Of(rule.Groups, rule.GetComposedGroups());

                everyChainInDefault &= chain.InDefault;

                if (chain.IsPlain)
                {
                    continue;
                }

                // Every entry before this one is a plain chain, which is what a default entry describes.
                chains ??= new ChainGroups[rules.Length];
                chains[i] = chain;

                declaredGroups = GroupNames.Merge(GroupNames.Merge(declaredGroups, chain.Named), chain.Reach);
            }

            var defaultRules = everyChainInDefault ? rules : DefaultRulesOf(rules, chains!);

            var built = new FrozenRules(
                rules,
                defaultRules,
                chains is null ? null : ChainGates.From(chains),
                declaredGroups ?? GroupNames.Empty,
                everyChainInDefault,
                isSynchronous);

            // Published once, whoever gets there first: a pass that read another freeze's object would see
            // the same rules, but one object is what every later reader should share.
            return Interlocked.CompareExchange(ref this.frozen, built, null) ?? built;
        }

        private static IPropertyRule<T>[] DefaultRulesOf(IPropertyRule<T>[] rules, ChainGroups[] chains)
        {
            var defaultRules = new List<IPropertyRule<T>>(rules.Length);

            for (var i = 0; i < rules.Length; i++)
            {
                if (chains[i].InDefault)
                {
                    defaultRules.Add(rules[i]);
                }
            }

            return [.. defaultRules];
        }

        private void ThrowIfFrozen()
        {
            if (this.frozen != null)
            {
                throw new InvalidOperationException(
                    $"{this.GetType().GetFormattedFullName()} has already validated something, so its rules " +
                    "can no longer change. " +
                    "Declare every rule before the first validation, typically in the constructor.");
            }
        }

        private void RequireSynchronous()
        {
            if (!this.IsSynchronous)
            {
                throw new InvalidOperationException(
                    $"{this.GetType().GetFormattedFullName()} has a rule which awaits, so it cannot be run " +
                    "synchronously.");
            }
        }

        /// <inheritdoc/>
        bool IValidationRunAware<T>.IsSynchronous => this.IsSynchronous;

        /// <inheritdoc/>
        string[] IValidationRunAware<T>.DeclaredGroups => this.Frozen.DeclaredGroups;

        /// <inheritdoc/>
        ValueTask<ValidationResult> IValidationRunAware<T>.ValidateAsync(T instance, ValidationRun run)
        {
            return this.ValidateAsync(instance, run);
        }

        /// <inheritdoc/>
        ValueTask IValidationRunAware<T>.ValidateIntoAsync(T instance, List<ValidationError> errors, ValidationRun run)
        {
            return this.ValidateIntoAsync(instance, errors, run);
        }

        /// <inheritdoc/>
        ValidationResult IValidationRunAware<T>.Validate(T instance, ValidationRun run)
        {
            return this.Validate(instance, run);
        }

        /// <inheritdoc/>
        void IValidationRunAware<T>.ValidateInto(T instance, List<ValidationError> errors, ValidationRun run)
        {
            this.ValidateInto(instance, errors, run);
        }

        /// <summary>
        /// What one pass resolved and every loop reads per chain: four fields, which is what lets the loops
        /// keep them in registers rather than reading them back per chain.
        /// </summary>
        private readonly record struct PassSettings(
            IValidationMessageProvider MessageProvider,
            ValidationBehavior ClassBehavior,
            ValidationBehavior PropertyBehavior,
            ValidationGroups Groups);

        /// <summary>
        /// Which loop a pass walks, read once before it: the rules the validator froze, the ones this pass
        /// walks, the gate of each chain where the pass may skip one — null where every chain walked runs —
        /// and the properties it is limited to, null for every one.
        /// </summary>
        private readonly record struct PassGates(
            FrozenRules Frozen,
            IPropertyRule<T>[] Rules,
            ChainGates? Chains,
            ValidationProperties? Properties);

        /// <summary>
        /// What one pass resolved: what it hands the validators it composes, and what its loop reads. Fields,
        /// so a kept one is read where it lies rather than copied for every pass.
        /// </summary>
        private readonly struct Resolution
        {
            public readonly InheritedSettings Inherited;

            public readonly PassSettings Settings;

            public readonly PassGates Gates;

            public Resolution(InheritedSettings inherited, PassSettings settings, PassGates gates)
            {
                this.Inherited = inherited;
                this.Settings = settings;
                this.Gates = gates;
            }
        }

        /// <summary>
        /// The resolution of a call, kept for reuse, with what it was resolved from.
        /// </summary>
        private sealed class RootPass
        {
            public readonly Resolution Resolution;

            public RootPass(
                NValidationOptions? options,
                NValidationOptions defaults,
                NValidationOptions declared,
                NValidationOptions? registered,
                Resolution resolution)
            {
                this.Options = options;
                this.Defaults = defaults;
                this.Declared = declared;
                this.Registered = registered;
                this.Resolution = resolution;
            }

            /// <summary>
            /// The options of the call it was resolved for; null for a call without any.
            /// </summary>
            public NValidationOptions? Options { get; }

            public NValidationOptions Defaults { get; }

            public NValidationOptions Declared { get; }

            public NValidationOptions? Registered { get; }

            /// <summary>
            /// Whether the defaults, what the validator declared and what the registration handed it are
            /// still the ones this was resolved from.
            /// </summary>
            public bool IsCurrent(NValidationOptions defaults, NValidationOptions declared, NValidationOptions? registered)
            {
                return ReferenceEquals(this.Defaults, defaults)
                    && ReferenceEquals(this.Declared, declared)
                    && ReferenceEquals(this.Registered, registered);
            }
        }

        /// <summary>
        /// The resolution of a pass of a validation this validator is composed into, kept for reuse, with what
        /// it was resolved from.
        /// </summary>
        private sealed class NestedPass
        {
            public readonly Resolution Resolution;

            private readonly InheritedSettings received;

            private readonly NValidationOptions declared;

            public NestedPass(InheritedSettings received, NValidationOptions declared, Resolution resolution)
            {
                this.received = received;
                this.declared = declared;
                this.Resolution = resolution;
            }

            public bool IsFor(in InheritedSettings received, NValidationOptions declared)
            {
                return this.received.IsSameAs(received) && ReferenceEquals(this.declared, declared);
            }
        }

        private sealed class FrozenRules
        {
            private string[]? reportedNames;

            public FrozenRules(
                IPropertyRule<T>[] rules,
                IPropertyRule<T>[] defaultRules,
                ChainGates? chains,
                string[] declaredGroups,
                bool everyChainInDefault,
                bool isSynchronous)
            {
                this.Rules = rules;
                this.DefaultRules = defaultRules;
                this.Chains = chains;
                this.DeclaredGroups = declaredGroups;
                this.EveryChainInDefault = everyChainInDefault;
                this.IsSynchronous = isSynchronous;
            }

            /// <summary>
            /// The name each chain reports under, in the order the rules are walked: what a pass limited to
            /// some properties matches. Built on the first such pass, so a validator never limited to
            /// properties never pays for it.
            /// </summary>
            public string[] ReportedNames => this.reportedNames ??= this.CollectReportedNames();

            /// <summary>
            /// The rules as an array, walked by the loop rather than a list enumerator.
            /// </summary>
            public IPropertyRule<T>[] Rules { get; }

            /// <summary>
            /// The rules of the chains in the default group, in the same order: all a call without groups
            /// walks. The same array as <see cref="Rules"/> where every chain is in it.
            /// </summary>
            public IPropertyRule<T>[] DefaultRules { get; }

            /// <summary>
            /// Where each chain runs, in the order the rules are walked, or null where every chain is in the
            /// default group alone and reaches nothing — which is what lets a validator declaring no group
            /// pay nothing for the feature.
            /// </summary>
            public ChainGates? Chains { get; }

            /// <summary>
            /// Every group a chain is in or reaches, which is what an exclusive selection has to name one of.
            /// </summary>
            public string[] DeclaredGroups { get; }

            public bool EveryChainInDefault { get; }

            public bool IsSynchronous { get; }

            private string[] CollectReportedNames()
            {
                var names = new string[this.Rules.Length];

                for (var i = 0; i < names.Length; i++)
                {
                    names[i] = this.Rules[i].ReportedName;
                }

                return names;
            }
        }
    }
}
