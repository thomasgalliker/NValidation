using System.Linq.Expressions;
using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// Declares the rules that every element of a collection has to satisfy. Obtained from
    /// <see cref="PropertyRuleBuilderExtensions.ForEach{TElement}(IPropertyRuleTarget{IEnumerable{TElement}}, Action{ElementRuleBuilder{TElement}})"/>.
    /// </summary>
    /// <remarks>
    /// A validator in its own right: the rules are declared with <see cref="Property{TProperty}"/> and
    /// extended by exactly the same rule methods as anywhere else, so nothing has to be written twice
    /// for elements.
    /// </remarks>
    public sealed class ElementRuleBuilder<TElement>
    {
        private readonly ElementRules rules = new();

        private Func<TElement, bool>? condition;

        private IValidator<TElement>? elementValidator;

        private Func<TElement, int, string>? indexer;

        internal ElementRuleBuilder()
        {
        }

        /// <summary>
        /// How much one entry reports: across its properties, and within one property's chain.
        /// </summary>
        /// <remarks>
        /// Built where it is declared rather than by the container, so — like a validator constructed
        /// with <c>new</c> — it takes the built-in defaults rather than what <c>AddNValidation</c>
        /// configured. Every entry is still walked whatever this says; it governs what one entry reports.
        /// </remarks>
        public ValidationBehaviors ValidationBehaviors => this.rules.ValidationBehaviors;

        /// <inheritdoc cref="Validator{T}.Property{TProperty}(System.Linq.Expressions.Expression{System.Func{T, TProperty}})"/>
        public PropertyRuleBuilder<TElement, TProperty?> Property<TProperty>(Expression<Func<TElement, TProperty>> expression)
        {
            return this.rules.Declare(expression);
        }

        /// <summary>
        /// Starts a rule chain for the element itself, for a collection of scalars which have no
        /// property to name: <c>this.Property(x => x.Mileages).ForEach(mileage => mileage.Element().GreaterThan(0));</c>
        /// A failure is reported under the element's position alone — <c>Mileages[1]</c>.
        /// </summary>
        public PropertyRuleBuilder<TElement, TElement?> Element()
        {
            return this.rules.DeclareSelf();
        }

        /// <summary>
        /// Applies these rules only to the elements <paramref name="condition"/> accepts. The elements
        /// it rejects keep their position, so the index a failure reports still points at the row the
        /// caller sent.
        /// </summary>
        public ElementRuleBuilder<TElement> Where(Func<TElement, bool> condition)
        {
            ArgumentNullException.ThrowIfNull(condition);

            var existingCondition = this.condition;

            this.condition = existingCondition == null
                ? condition
                : element => existingCondition(element) && condition(element);

            return this;
        }

        /// <summary>
        /// Validates each element with its own validator and merges the result — the form to reach for
        /// when the element already has one.
        /// </summary>
        public ElementRuleBuilder<TElement> SetValidator(IValidator<TElement> validator)
        {
            ArgumentNullException.ThrowIfNull(validator);

            this.elementValidator = validator;

            return this;
        }

        /// <summary>
        /// Identifies each element by something of its own instead of by its position, so a caller can
        /// match a failure to a row by key: <c>WithIndexer((record, _) => record.InvoiceNumber)</c>
        /// reports <c>ServiceHistory[INV-9912].Workshop</c>.
        /// </summary>
        /// <remarks>
        /// The position is still passed, for an identity which reads better one-based, or which falls
        /// back to it. Only the reported property name changes; <see cref="ValidationMessagePlaceholders.CollectionIndex"/>
        /// keeps reporting the position.
        /// <para>
        /// Whatever this returns becomes part of the property name, which a host renders straight into its
        /// response — as a JSON member name, for a problem details body. Identify an element by
        /// something short and of the application's own choosing; a value the caller sent is echoed back
        /// at whatever length the caller chose.
        /// </para>
        /// </remarks>
        public ElementRuleBuilder<TElement> WithIndexer(Func<TElement, int, string> indexer)
        {
            ArgumentNullException.ThrowIfNull(indexer);

            this.indexer = indexer;

            return this;
        }

        internal async ValueTask ValidateElementsAsync(
            IEnumerable<TElement> elements,
            string propertyName,
            Action<ValidationError> report,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken)
        {
            var index = 0;

            // One list for the whole collection, cleared per entry: what an entry reports is copied out
            // under the entry's own propertyName straight away, so nothing has to be kept between entries. A
            // list per entry was the bulk of what an entry cost.
            List<ValidationError>? elementErrors = null;

            foreach (var element in elements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var position = index++;

                // A null entry has no properties to judge. Requiring entries to be there at all is a
                // question about the collection, which its own rules answer. A skipped entry still
                // spends its position, so an index points at the row the caller sent.
                if (element is null || (this.condition != null && !this.condition(element)))
                {
                    continue;
                }

                var elementMessages = new IndexedMessageProvider<TElement>(messages, propertyName, element, position, this.indexer);

                elementErrors ??= [];
                elementErrors.Clear();

                await this.AddErrorsAsync(element, report, elementMessages, elementErrors, cancellationToken);
            }
        }

        private async ValueTask AddErrorsAsync(
            TElement element,
            Action<ValidationError> report,
            IndexedMessageProvider<TElement> messages,
            List<ValidationError> elementErrors,
            CancellationToken cancellationToken)
        {
            await this.rules.ValidateIntoAsync(element, elementErrors, messages, cancellationToken);

            if (this.elementValidator != null)
            {
                await NestedValidation.ValidateIntoAsync(
                    this.elementValidator, element, elementErrors, messages, cancellationToken);
            }

            if (elementErrors.Count == 0)
            {
                // The entry is only named on demand, and an entry with nothing to report never asks.
                return;
            }

            foreach (var error in elementErrors)
            {
                report(new ValidationError(
                    Compose(messages.ElementPropertyName, error.PropertyName),
                    error.Message,
                    error.ErrorCode,
                    error.Arguments));
            }
        }

        /// <summary>
        /// The property name a failure is reported under. A rule about the element itself carries no
        /// property, so the element's position is the whole property name.
        /// </summary>
        private static string Compose(string elementPropertyName, string propertyName)
        {
            return propertyName.Length == 0 ? elementPropertyName : $"{elementPropertyName}.{propertyName}";
        }

        /// <summary>
        /// The rules an entry has to satisfy, as an ordinary validator.
        /// </summary>
        /// <remarks>
        /// Held rather than inherited. An element builder that <em>was</em> a validator was publicly an
        /// <see cref="IValidator{T}"/> which ignored half of its own configuration: running it directly
        /// applied the property rules but not <see cref="Where"/>, <see cref="SetValidator"/> or
        /// <see cref="WithIndexer"/>, so it gave a different verdict than the <c>ForEach</c> it belongs
        /// to — and, satisfying <see cref="IValidator{T}"/>, it could even be passed to its own
        /// <see cref="SetValidator"/>.
        /// </remarks>
        private sealed class ElementRules : Validator<TElement>
        {
            public PropertyRuleBuilder<TElement, TProperty?> Declare<TProperty>(Expression<Func<TElement, TProperty>> expression)
            {
                return this.Property(expression);
            }

            public PropertyRuleBuilder<TElement, TElement?> DeclareSelf()
            {
                return this.RuleForSelf();
            }
        }
    }
}
