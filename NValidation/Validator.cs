using System.Linq.Expressions;
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

        // The rules as an array, taken on the first validation and never re-read: the loop walks an
        // array rather than a list enumerator, and a rule declared afterwards is refused.
        private IPropertyRule<T>[]? frozenRules;

        // What this validator declared for itself. A setting left null falls through to what the pass
        // inherits — the options of the call, or the composer's settings — then to what the registration
        // configured, then to NValidationOptions.Default.
        private NValidationOptions options = NValidationOptions.None;

        // What the registration configured, for a validator the container built; null for one it did not.
        private NValidationOptions? registeredOptions;

        private bool? isSynchronous;

        private IPropertyRule<T>[] Rules => this.frozenRules ??= this.FreezeRules();

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
        /// Whether every chain of this validator judges rather than awaits, so a validation can run
        /// without an async state machine anywhere in it. Settled by the rules on first use.
        /// </summary>
        private bool IsSynchronous
        {
            get
            {
                if (this.isSynchronous is { } settled)
                {
                    return settled;
                }

                var synchronous = true;

                foreach (var rule in this.Rules)
                {
                    if (!rule.IsSynchronous)
                    {
                        synchronous = false;
                        break;
                    }
                }

                this.isSynchronous = synchronous;

                return synchronous;
            }
        }

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

            this.rules.Add(rule);

            return new PropertyRuleBuilder<T, TProperty?>(rule);
        }

        /// <summary>
        /// The same, naming the property and reading it with a plain delegate, for a value that is not a
        /// member path: a computed value, a dictionary entry, an indexer.
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

            this.rules.Add(rule);

            return new PropertyRuleBuilder<T, TProperty>(rule);
        }

        /// <summary>
        /// The same, for a value reached through something the payload may have omitted:
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

            var builder = this.Property(propertyName, accessor);

            return builder.When(isReachable);
        }

        /// <summary>
        /// Starts a rule chain for the instance itself rather than for one of its properties. Used for
        /// the elements of a collection of scalars, which have no property to name.
        /// </summary>
        internal PropertyRuleBuilder<T, T?> RuleForSelf()
        {
            this.ThrowIfFrozen();

            var rule = new PropertyRule<T, T?>(PropertyPath.Self, instance => instance);

            this.rules.Add(rule);

            return new PropertyRuleBuilder<T, T?>(rule);
        }

        /// <inheritdoc/>
        public ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
        {
            return this.ValidateAsync(instance, new ValidationRun(default, Scope: null, cancellationToken));
        }

        /// <inheritdoc/>
        public ValueTask<ValidationResult> ValidateAsync(
            T instance, NValidationOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);

            return this.ValidateAsync(instance, new ValidationRun(InheritedSettings.From(options), Scope: null, cancellationToken));
        }

        /// <summary>
        /// Validates as part of <paramref name="run"/>, so a validator composed into another inherits its
        /// composer's settings and knows the collection entry it is inside.
        /// </summary>
        internal ValueTask<ValidationResult> ValidateAsync(T instance, ValidationRun run)
        {
            var frame = this.CreateFrame(instance, errors: null, run, out var settings);

            if (this.IsSynchronous)
            {
                this.ValidateFrame(frame, settings);

                return new ValueTask<ValidationResult>(Result(frame));
            }

            return this.ValidateAwaitingAsync(frame, settings);
        }

        /// <inheritdoc cref="ValidateAsync(T, ValidationRun)"/>
        private async ValueTask<ValidationResult> ValidateAwaitingAsync(ValidationFrame<T> frame, PassSettings settings)
        {
            await this.ValidateFrameAsync(frame, settings);

            return Result(frame);
        }

        /// <summary>
        /// The synchronous form of <see cref="ValidateAsync(T, ValidationRun)"/>, for a composer which
        /// knows that every rule here judges rather than awaits.
        /// </summary>
        internal ValidationResult Validate(T instance, ValidationRun run)
        {
            this.RequireSynchronous();

            var frame = this.CreateFrame(instance, errors: null, run, out var settings);

            this.ValidateFrame(frame, settings);

            return Result(frame);
        }

        /// <summary>
        /// Reports into a list the caller owns, for a caller running this validator once per entry of a
        /// collection, where a list and a result per entry would be the bulk of what the entry costs.
        /// </summary>
        internal ValueTask ValidateIntoAsync(T instance, List<ValidationError> errors, ValidationRun run)
        {
            var frame = this.CreateFrame(instance, errors, run, out var settings);

            if (this.IsSynchronous)
            {
                this.ValidateFrame(frame, settings);

                return default;
            }

            return this.ValidateFrameAsync(frame, settings);
        }

        /// <inheritdoc cref="ValidateIntoAsync"/>
        internal void ValidateInto(T instance, List<ValidationError> errors, ValidationRun run)
        {
            this.RequireSynchronous();

            var frame = this.CreateFrame(instance, errors, run, out var settings);

            this.ValidateFrame(frame, settings);
        }

        /// <summary>
        /// What a finished pass amounts to: the failures it collected, or the shared successful result
        /// where it collected none.
        /// </summary>
        private static ValidationResult Result(ValidationFrame<T> frame)
        {
            return frame.TryTakeErrors(out var errors)
                ? ValidationResult.FromValidationErrorsInternal(errors)
                : ValidationResult.Success;
        }

        /// <summary>
        /// The state this validator's pass over one object works against, with the settings the pass
        /// resolved substituted into its run — so what this validator composes inherits them.
        /// </summary>
        private ValidationFrame<T> CreateFrame(T instance, List<ValidationError>? errors, ValidationRun run, out PassSettings settings)
        {
            ArgumentNullException.ThrowIfNull(instance);

            settings = this.Resolve(run.Inherited);

            var resolvedRun = run with
            {
                Inherited = new InheritedSettings(settings.MessageProvider, settings.ClassBehavior, settings.PropertyBehavior),
            };

            return new ValidationFrame<T>(instance, errors, resolvedRun);
        }

        /// <summary>
        /// The provider and the two behavior axes this pass runs with, resolved once from the most
        /// specific level that names each: this validator, then what the pass inherits — the options of
        /// the call, or the composer's settings — then what the registration configured, then
        /// <see cref="NValidationOptions.Default"/>, then the built-in values. <c>Default</c> is read
        /// once, so the three settings come from the same options even if it is reassigned meanwhile.
        /// </summary>
        private PassSettings Resolve(InheritedSettings inherited)
        {
            var defaults = NValidationOptions.Default;
            var declared = this.options;
            var registered = this.registeredOptions;

            var messageProvider = declared.MessageProvider
                ?? inherited.MessageProvider
                ?? registered?.MessageProvider
                ?? defaults.MessageProvider
                ?? DefaultValidationMessageProvider.Instance;

            var classBehavior = declared.ValidationBehaviors.Class
                ?? inherited.Class
                ?? registered?.ValidationBehaviors.Class
                ?? defaults.ValidationBehaviors.Class
                ?? ValidationBehavior.All;

            // A run that stops at the first error stops inside a chain too, or the setting would not do
            // what its name says. A chain which declared something of its own still gets it.
            var propertyBehavior = classBehavior == ValidationBehavior.StopAtFirstError
                ? ValidationBehavior.StopAtFirstError
                : declared.ValidationBehaviors.Property
                    ?? inherited.Property
                    ?? registered?.ValidationBehaviors.Property
                    ?? defaults.ValidationBehaviors.Property
                    ?? ValidationBehavior.StopAtFirstError;

            return new PassSettings(messageProvider, classBehavior, propertyBehavior);
        }

        /// <summary>
        /// Runs every chain against a pass which has already been set up, where no chain awaits.
        /// </summary>
        /// <remarks>
        /// The twin of <see cref="ValidateFrameAsync"/>, for the same reason
        /// <see cref="PropertyRule{T, TProperty}.Validate"/> is the twin of its awaiting form: a body
        /// shared between them would have to be an async method, which is the cost this avoids.
        /// </remarks>
        private void ValidateFrame(ValidationFrame<T> frame, PassSettings settings)
        {
            var cancellationToken = frame.Run.CancellationToken;

            // What this pass found, told apart from what the caller's list already held: an element's
            // inline rules have already reported into it by the time the element's own validator runs.
            var errorCountAtStart = frame.ErrorCount;

            var rules = this.Rules;

            for (var i = 0; i < rules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (settings.ClassBehavior == ValidationBehavior.StopAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                rules[i].Validate(frame, settings.MessageProvider, settings.PropertyBehavior);
            }
        }

        /// <inheritdoc cref="ValidateFrame"/>
        private async ValueTask ValidateFrameAsync(ValidationFrame<T> frame, PassSettings settings)
        {
            var cancellationToken = frame.Run.CancellationToken;

            var errorCountAtStart = frame.ErrorCount;

            var rules = this.Rules;

            for (var i = 0; i < rules.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (settings.ClassBehavior == ValidationBehavior.StopAtFirstError && frame.ErrorCount > errorCountAtStart)
                {
                    return;
                }

                await rules[i].ValidateAsync(frame, settings.MessageProvider, settings.PropertyBehavior);
            }
        }

        /// <summary>
        /// The rules as an array, and every chain told that it is now in use and which display names the
        /// validator's properties carry. Done once, on the first validation; the race between two first
        /// calls is benign, because both compute the same thing. From then on a rule declared late is
        /// refused rather than silently ignored.
        /// </summary>
        private IPropertyRule<T>[] FreezeRules()
        {
            var frozen = this.rules.ToArray();
            var displayNames = PropertyDisplayNames.For(frozen);

            foreach (var rule in frozen)
            {
                rule.Freeze(displayNames);
            }

            return frozen;
        }

        /// <exception cref="InvalidOperationException">This validator has already validated something.</exception>
        private void ThrowIfFrozen()
        {
            if (this.frozenRules != null)
            {
                throw new InvalidOperationException(
                    $"{this.GetType()} has already validated something, so its rules can no longer change. " +
                    "Declare every rule before the first validation, typically in the constructor.");
            }
        }

        private void RequireSynchronous()
        {
            if (!this.IsSynchronous)
            {
                throw new InvalidOperationException(
                    $"{this.GetType()} has a rule which awaits, so it cannot be run synchronously.");
            }
        }

        /// <inheritdoc/>
        bool IValidationRunAware<T>.IsSynchronous => this.IsSynchronous;

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
        /// What one pass runs with, resolved once rather than per chain.
        /// </summary>
        private readonly record struct PassSettings(
            IValidationMessageProvider MessageProvider,
            ValidationBehavior ClassBehavior,
            ValidationBehavior PropertyBehavior);
    }
}
