using System.Linq.Expressions;
using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// Declares the rules that every element of a collection has to satisfy. Obtained from
    /// <see cref="PropertyRuleBuilderExtensions.ForEach{TElement}(IPropertyRuleTarget{IEnumerable{TElement}}, Action{ElementRuleBuilder{TElement}})"/>.
    /// The rules are declared with <see cref="Property{TProperty}"/>, grouped with
    /// <see cref="Group(string, Action)"/>, and extended by exactly the same rule methods as anywhere
    /// else, so nothing has to be written twice for elements.
    /// </summary>
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
        /// How much one entry reports: across its properties, and within one property's chain. An axis
        /// left <c>null</c> inherits from the validator the <c>ForEach</c> was declared in. Every entry is
        /// still walked whatever this says; it governs what one entry reports.
        /// </summary>
        public ValidationBehaviors ValidationBehaviors
        {
            get => this.rules.ValidationBehaviors;
            set => this.rules.ValidationBehaviors = value;
        }

        internal bool IsSynchronous =>
            ((IValidationRunAware<TElement>)this.rules).IsSynchronous &&
            (this.elementValidator is null || this.elementValidator is IValidationRunAware<TElement> { IsSynchronous: true });

        /// <inheritdoc cref="Validator{T}.Property{TProperty}(Expression{System.Func{T, TProperty}})"/>
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
        /// Puts every element chain declared inside <paramref name="declareRules"/> in
        /// <paramref name="group"/>, as <see cref="Validator{T}.Group(string, Action)"/> does for a
        /// validator's own chains.
        /// </summary>
        public void Group(string group, Action declareRules)
        {
            this.rules.DeclareGroup(group, declareRules);
        }

        /// <summary>
        /// Puts every element chain declared inside <paramref name="declareRules"/> in all of
        /// <paramref name="groups"/>, which is written as a collection expression.
        /// </summary>
        public void Group(ReadOnlySpan<string> groups, Action declareRules)
        {
            this.rules.DeclareGroup(groups, declareRules);
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
        /// Only the reported property name changes; <see cref="ValidationMessagePlaceholders.CollectionIndex"/>
        /// keeps reporting the position. Whatever this returns is rendered straight into the response as
        /// part of the property name, so identify an element by something short and of the
        /// application's own choosing.
        /// </remarks>
        public ElementRuleBuilder<TElement> WithIndexer(Func<TElement, int, string> indexer)
        {
            ArgumentNullException.ThrowIfNull(indexer);

            this.indexer = indexer;

            return this;
        }

        internal void ValidateElements(IEnumerable<TElement> elements, string propertyName, ValidationFrame frame)
        {
            var run = frame.Run;
            var index = 0;

            List<ValidationError>? elementErrors = null;
            ElementScope<TElement>? scope = null;

            foreach (var element in elements)
            {
                run.CancellationToken.ThrowIfCancellationRequested();

                var position = index++;

                if (this.IsSkipped(element))
                {
                    continue;
                }

                scope ??= new ElementScope<TElement>(propertyName, this.indexer);
                scope.MoveTo(element, position);

                elementErrors ??= [];
                elementErrors.Clear();

                var elementRun = run with { Scope = scope };

                this.rules.ValidateInto(element, elementErrors, elementRun);

                if (this.elementValidator is IValidationRunAware<TElement> aware)
                {
                    aware.ValidateInto(element, elementErrors, elementRun);
                }

                Report(frame, scope, elementErrors);
            }
        }

        internal async ValueTask ValidateElementsAsync(IEnumerable<TElement> elements, string propertyName, ValidationFrame frame)
        {
            var run = frame.Run;
            var index = 0;

            List<ValidationError>? elementErrors = null;
            ElementScope<TElement>? scope = null;

            foreach (var element in elements)
            {
                run.CancellationToken.ThrowIfCancellationRequested();

                var position = index++;

                if (this.IsSkipped(element))
                {
                    continue;
                }

                scope ??= new ElementScope<TElement>(propertyName, this.indexer);
                scope.MoveTo(element, position);

                elementErrors ??= [];
                elementErrors.Clear();

                var elementRun = run with { Scope = scope };

                await this.rules.ValidateIntoAsync(element, elementErrors, elementRun);

                if (this.elementValidator != null)
                {
                    await NestedValidation.ValidateIntoAsync(this.elementValidator, element, elementErrors, elementRun);
                }

                Report(frame, scope, elementErrors);
            }
        }

        private bool IsSkipped(TElement element)
        {
            return element is null || (this.condition != null && !this.condition(element));
        }

        private static void Report(ValidationFrame frame, ElementScope scope, List<ValidationError> elementErrors)
        {
            if (elementErrors.Count == 0)
            {
                return;
            }

            var elementPropertyName = scope.ElementPropertyName;

            foreach (var error in elementErrors)
            {
                frame.Errors.Add(new ValidationError(
                    Compose(elementPropertyName, error.PropertyName),
                    error.Message,
                    error.ErrorCode,
                    error.Arguments));
            }
        }

        private static string Compose(string elementPropertyName, string propertyName)
        {
            return propertyName.Length == 0 ? elementPropertyName : $"{elementPropertyName}.{propertyName}";
        }

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

            public void DeclareGroup(string group, Action declareRules)
            {
                this.Group(group, declareRules);
            }

            public void DeclareGroup(ReadOnlySpan<string> groups, Action declareRules)
            {
                this.Group(groups, declareRules);
            }
        }
    }
}
