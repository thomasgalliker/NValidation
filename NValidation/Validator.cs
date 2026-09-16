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
    public abstract class Validator<T> : IValidator<T>, IMessageProviderTarget, IValidationBehaviorTarget, IMessageProviderAware<T>
    {
        private readonly List<IPropertyRule<T>> rules = [];

        private IValidationMessageProvider messages = DefaultValidationMessageProvider.Instance;

        private readonly ValidationBehaviors validationBehaviors = new();

        /// <summary>
        /// What the registration configured, kept apart from what this validator declared for itself so
        /// the validator's word wins whichever was written first.
        /// </summary>
        private ValidationBehaviors? ambientValidationBehaviors;

        private PropertyDisplayNames? displayNames;

        /// <summary>
        /// Where the rules take their message texts from. Assigned by the DI registration
        /// (<c>AddValidator</c>) from the registered <see cref="IValidationMessageProvider"/>, so a
        /// concrete validator's constructor stays free of plumbing and only declares rules. Falls back
        /// to the built-in English messages when the validator is constructed directly.
        /// </summary>
        /// <remarks>
        /// Read while validating rather than while the rules are declared, so it can still be assigned
        /// after the constructor has run.
        /// </remarks>
        public IValidationMessageProvider Messages
        {
            get => this.messages;
            set => this.messages = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// How much this validator reports: across its properties, and within one property's chain.
        /// Mutated rather than assigned — <c>this.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;</c>
        /// — so naming one axis leaves the other inheriting.
        /// </summary>
        /// <remarks>
        /// An axis left unset takes what the DI registration configured through
        /// <c>NValidationBuilder.ValidationBehaviors</c>, and failing that the built-in defaults:
        /// every property, one message each. Like <see cref="Messages"/>, it is read while validating
        /// rather than while the rules are declared, so where in the constructor it is written makes no
        /// difference.
        /// </remarks>
        public ValidationBehaviors ValidationBehaviors => this.validationBehaviors;

        /// <inheritdoc/>
        ValidationBehaviors IValidationBehaviorTarget.AmbientValidationBehaviors
        {
            set => this.ambientValidationBehaviors = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Starts a rule chain for the given property. The property name is taken from the expression's
        /// member path, so <c>x => x.Name</c> reports as <c>Name</c> and <c>x => x.Address.Street</c>
        /// as <c>Address.Street</c>.
        /// </summary>
        /// <remarks>
        /// The chain is always built for the nullable form of the property's type. The builder cannot be
        /// variant (it is a struct, and the property type appears in input positions), so a rule declared
        /// for <c>string</c> would not accept a chain for a <c>string?</c> property and every optional
        /// property would warn at its call site. Normalizing here means a rule is written once and a rule
        /// body has to face the fact that the value may be null — which it may, since validation runs on
        /// whatever a caller supplied.
        /// </remarks>
        protected PropertyRuleBuilder<T, TProperty?> Property<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

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
        /// The same, naming the property and reading it directly instead of through an expression:
        /// <code>this.Property("Vin", static c => c.Vin).NotEmpty();</code>
        /// </summary>
        /// <remarks>
        /// An escape hatch for a validator on a hot path, not the spelling most code should use. The
        /// expression form is read once to produce three things — the property name, a compiled accessor
        /// and a guard against dereferencing something absent — and building it costs an expression tree
        /// per construction plus the reflection behind <c>Expression.Property</c>, which together are the
        /// larger part of what constructing a validator costs.
        /// <para>
        /// What it gives up: the name is written by hand, so renaming the property will not change what
        /// the failure is reported under, and nothing checks that the two agree. Use it for a property
        /// of the validated object itself; a path through another object needs the overload taking a
        /// reachability predicate, or the expression form, which works it out.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty or whitespace.</exception>
        protected PropertyRuleBuilder<T, TProperty> Property<TProperty>(string propertyName, Func<T, TProperty> accessor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            ArgumentNullException.ThrowIfNull(accessor);

            var rule = new PropertyRule<T, TProperty>(propertyName, accessor);

            this.rules.Add(rule);

            return new PropertyRuleBuilder<T, TProperty>(rule);
        }

        /// <summary>
        /// The same, for a property reached through something the payload may have omitted:
        /// <code>this.Property("Model.Name", static c => c.Model!.Name, static c => c.Model != null).NotEmpty();</code>
        /// </summary>
        /// <remarks>
        /// <paramref name="isReachable"/> is what the expression form works out for itself. Without it
        /// the accessor would dereference whatever is missing and turn a bad request into a server
        /// error, which is the one thing this library is careful never to do. A chain whose predicate
        /// says no is skipped, exactly as a chain declared through an absent object is.
        /// </remarks>
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
            var rule = new PropertyRule<T, T?>(PropertyPath.Self, instance => instance);

            this.rules.Add(rule);

            return new PropertyRuleBuilder<T, T?>(rule);
        }

        /// <inheritdoc/>
        public ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
        {
            return this.ValidateAsync(instance, this.Messages, cancellationToken);
        }

        /// <summary>
        /// Validates against a message provider supplied per call rather than the one this validator
        /// carries, so a caller which resolves messages differently — an element of a collection, whose
        /// messages know its index — does not have to mutate shared state to do it.
        /// </summary>
        internal async ValueTask<ValidationResult> ValidateAsync(T instance, IValidationMessageProvider messages, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            await this.ValidateIntoAsync(instance, errors, messages, cancellationToken);

            return ValidationResult.FromValidationErrors(errors);
        }

        /// <summary>
        /// Reports into a list the caller owns, instead of into one of this validator's own wrapped in a
        /// <see cref="ValidationResult"/>.
        /// </summary>
        /// <remarks>
        /// For a caller running this validator many times over — once per entry of a collection — where
        /// a list and a result per entry would be the bulk of what the entry costs.
        /// </remarks>
        internal async ValueTask ValidateIntoAsync(T instance, List<ValidationError> errors, IValidationMessageProvider messages, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instance);

            // What this run found, told apart from what the caller's list already held: the list is the
            // caller's, and an element's inline rules have already reported into it by the time the
            // element's own validator is handed the same list.
            var errorCountAtStart = errors.Count;

            var classBehavior = this.validationBehaviors.Class
                ?? this.ambientValidationBehaviors?.Class
                ?? ValidationBehavior.All;

            // A run that stops at the first error stops inside a chain too, or the setting would not do
            // what its name says. A chain which declared something of its own still gets it, because it
            // said so at the point it applies.
            var propertyBehavior = classBehavior == ValidationBehavior.StopAtFirstError
                ? ValidationBehavior.StopAtFirstError
                : this.validationBehaviors.Property
                    ?? this.ambientValidationBehaviors?.Property
                    ?? ValidationBehavior.StopAtFirstError;

            // Built once and kept: a display name is stored as a Func<string> and resolved while the
            // message is produced, so the culture of the current run is already accounted for. The
            // race between two first calls is benign — both compute the same map.
            var displayNames = this.displayNames ??= PropertyDisplayNames.For(this.rules);

            foreach (var rule in this.rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (classBehavior == ValidationBehavior.StopAtFirstError && errors.Count > errorCountAtStart)
                {
                    return;
                }

                await rule.ValidateAsync(instance, errors, messages, displayNames, propertyBehavior, cancellationToken);
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Implemented here rather than left to the default implementation on <see cref="IValidator{T}"/>.
        /// A validator which serves a second payload — by implementing <see cref="IValidator{T}"/> for it
        /// by hand, which <see cref="Internals.ValidatorTypeInfo"/> and the registration both support —
        /// would otherwise inherit two of those defaults, neither more specific than the other, and the
        /// type would not compile at all (CS8705). Declaring it on the base class settles the ambiguity
        /// for the common case.
        /// <para>
        /// Such a validator is the one that has to say what it means: it re-implements this member itself
        /// and dispatches on the instance's type, because a base class closed over one
        /// <typeparamref name="T"/> cannot know about the other.
        /// </para>
        /// </remarks>
        ValueTask<ValidationResult> IValidator.ValidateAsync(object instance, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (instance is not T typed)
            {
                throw new InvalidCastException(
                    $"{this.GetType()} validates {typeof(T)}, so it cannot validate an instance of " +
                    $"{instance.GetType()}. A validator which serves more than one payload implements " +
                    $"{nameof(IValidator)}.{nameof(IValidator.ValidateAsync)} itself and dispatches on the " +
                    "instance's type.");
            }

            return this.ValidateAsync(typed, cancellationToken);
        }

        /// <inheritdoc/>
        ValueTask<ValidationResult> IMessageProviderAware<T>.ValidateAsync(T instance, IValidationMessageProvider messages, CancellationToken cancellationToken)
        {
            return this.ValidateAsync(instance, messages, cancellationToken);
        }

        /// <inheritdoc/>
        ValueTask IMessageProviderAware<T>.ValidateIntoAsync(T instance, List<ValidationError> errors, IValidationMessageProvider messages, CancellationToken cancellationToken)
        {
            return this.ValidateIntoAsync(instance, errors, messages, cancellationToken);
        }
    }
}
