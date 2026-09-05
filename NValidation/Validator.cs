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
    public abstract class Validator<T> : IValidator<T>, IMessageProviderTarget, IMessageProviderAware<T>
    {
        private readonly List<IPropertyRule<T>> rules = [];

        private IValidationMessageProvider messages = DefaultValidationMessageProvider.Instance;

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
        /// Starts a rule chain for the given property. The error code is taken from the expression's
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

            var code = PropertyPath.From(expression);
            var rule = new PropertyRule<T, TProperty?>(code, PropertyAccessor.For(code, expression));

            // Added before any condition the chain declares, so it is the first thing asked and the
            // property is never read through something that is not there.
            var isReachable = ReachabilityGuard.For(code, expression);

            if (isReachable != null)
            {
                rule.AddCondition(isReachable);
            }

            this.rules.Add(rule);

            return new PropertyRuleBuilder<T, TProperty?>(rule);
        }

        /// <summary>
        /// Starts a rule chain for the instance itself rather than for one of its properties. Used for
        /// the elements of a collection of scalars, which have no property to name.
        /// </summary>
        internal PropertyRuleBuilder<T, T> RuleForSelf()
        {
            var rule = new PropertyRule<T, T>(string.Empty, instance => instance);

            this.rules.Add(rule);

            return new PropertyRuleBuilder<T, T>(rule);
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
        internal async ValueTask<ValidationResult> ValidateAsync(
            T instance,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken)
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
        internal async ValueTask ValidateIntoAsync(
            T instance,
            List<ValidationError> errors,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instance);

            // Built once and kept: a display name is stored as a Func<string> and resolved while the
            // message is produced, so the culture of the current run is already accounted for. The
            // race between two first calls is benign — both compute the same map.
            var displayNames = this.displayNames ??= PropertyDisplayNames.For(this.rules);

            foreach (var rule in this.rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await rule.ValidateAsync(instance, errors, messages, displayNames, cancellationToken);
            }
        }

        /// <inheritdoc/>
        ValueTask<ValidationResult> IMessageProviderAware<T>.ValidateAsync(
            T instance,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken)
        {
            return this.ValidateAsync(instance, messages, cancellationToken);
        }

        /// <inheritdoc/>
        ValueTask IMessageProviderAware<T>.ValidateIntoAsync(
            T instance,
            List<ValidationError> errors,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken)
        {
            return this.ValidateIntoAsync(instance, errors, messages, cancellationToken);
        }
    }
}
