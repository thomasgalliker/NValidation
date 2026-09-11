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
    public sealed class ElementRuleBuilder<TElement> : Validator<TElement>
    {
        private Func<TElement, bool>? condition;

        private IValidator<TElement>? elementValidator;

        private Func<TElement, int, string>? indexer;

        internal ElementRuleBuilder()
        {
        }

        /// <inheritdoc cref="Validator{T}.Property{TProperty}"/>
        public new PropertyRuleBuilder<TElement, TProperty?> Property<TProperty>(Expression<Func<TElement, TProperty>> expression)
        {
            return base.Property(expression);
        }

        /// <summary>
        /// Starts a rule chain for the element itself, for a collection of scalars which have no
        /// property to name: <c>this.Property(x => x.Mileages).ForEach(mileage => mileage.Element().GreaterThan(0));</c>
        /// A failure is reported under the element's position alone — <c>Mileages[1]</c>.
        /// </summary>
        public PropertyRuleBuilder<TElement, TElement?> Element()
        {
            return this.RuleForSelf();
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
        /// back to it. Only the code changes; <see cref="ValidationMessagePlaceholders.CollectionIndex"/>
        /// keeps reporting the position.
        /// <para>
        /// Whatever this returns becomes part of the error code, which a host renders straight into its
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
            string code,
            Action<ValidationError> report,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken)
        {
            var index = 0;

            // One list for the whole collection, cleared per entry: what an entry reports is copied out
            // under the entry's own code straight away, so nothing has to be kept between entries. A
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

                var elementMessages = new IndexedMessageProvider<TElement>(messages, code, element, position, this.indexer);

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
            await this.ValidateIntoAsync(element, elementErrors, messages, cancellationToken);

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
                report(new ValidationError(Compose(messages.ElementCode, error.Code), error.Message));
            }
        }

        /// <summary>
        /// The code a failure is reported under. A rule about the element itself carries no property, so
        /// the element's position is the whole code.
        /// </summary>
        private static string Compose(string elementCode, string propertyCode)
        {
            return propertyCode.Length == 0 ? elementCode : $"{elementCode}.{propertyCode}";
        }
    }
}
