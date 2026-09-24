using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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

        private bool isFrozen;

        private string[]? rulesGroups;

        private string[]? validatorGroups;

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

        /// <summary>
        /// The groups the entries' own chains and their validator declare, through which a selection
        /// leaving the default group out reaches the entries; settled by <see cref="Freeze"/>.
        /// </summary>
        internal string[]? DeclaredGroups { get; private set; }

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
        /// Applies every element chain declared inside <paramref name="declareRules"/> only to the entries
        /// <paramref name="condition"/> holds for, as <see cref="Validator{T}.When(Func{T, bool}, Action)"/>
        /// does for a validator's own chains.
        /// </summary>
        /// <remarks>
        /// Narrows the chains inside the block alone; <see cref="Where"/> is what passes an entry by for
        /// every rule, its validator included.
        /// </remarks>
        public ConditionBlock<TElement> When(Func<TElement, bool> condition, Action declareRules)
        {
            return this.rules.DeclareWhenBlock(condition, declareRules);
        }

        /// <summary>
        /// Applies every element chain declared inside <paramref name="declareRules"/> to the entries
        /// <paramref name="condition"/> does not hold for.
        /// </summary>
        public ConditionBlock<TElement> Unless(Func<TElement, bool> condition, Action declareRules)
        {
            return this.rules.DeclareUnlessBlock(condition, declareRules);
        }

        /// <summary>
        /// Applies every element chain declared inside <paramref name="declareRules"/> only when the
        /// validation was handed a <typeparamref name="TData"/> and <paramref name="condition"/> holds for it
        /// and the entry.
        /// </summary>
        public void When<TData>(Func<TElement, TData, bool> condition, Action declareRules)
        {
            this.rules.DeclareDataBlock(condition, declareRules);
        }

        /// <summary>
        /// Applies these rules only to the elements <paramref name="condition"/> accepts. The elements
        /// it rejects keep their position, so the index a failure reports still points at the row the
        /// caller sent.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <c>ForEach</c> has already been declared.</exception>
        public ElementRuleBuilder<TElement> Where(Func<TElement, bool> condition)
        {
            ArgumentNullException.ThrowIfNull(condition);
            this.ThrowIfFrozen();

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
        /// <exception cref="InvalidOperationException">The <c>ForEach</c> has already been declared.</exception>
        public ElementRuleBuilder<TElement> SetValidator(IValidator<TElement> validator)
        {
            ArgumentNullException.ThrowIfNull(validator);
            this.ThrowIfFrozen();

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
        /// <exception cref="InvalidOperationException">The <c>ForEach</c> has already been declared.</exception>
        public ElementRuleBuilder<TElement> WithIndexer(Func<TElement, int, string> indexer)
        {
            ArgumentNullException.ThrowIfNull(indexer);
            this.ThrowIfFrozen();

            this.indexer = indexer;

            return this;
        }

        /// <summary>
        /// Refuses every later change: the chain declaring the <c>ForEach</c> has settled from here what the
        /// entries await and which groups they declare, and a change made afterwards would go unseen.
        /// </summary>
        internal void Freeze()
        {
            this.isFrozen = true;

            this.rulesGroups = NullIfEmpty(((IValidationRunAware<TElement>)this.rules).DeclaredGroups);
            this.validatorGroups = this.elementValidator is IValidationRunAware<TElement> aware
                ? NullIfEmpty(aware.DeclaredGroups)
                : null;

            this.DeclaredGroups = GroupNames.Merge(this.rulesGroups, this.validatorGroups);
        }

        /// <summary>
        /// Kept out of line, so the loop that inlines the chain handing its entries here does not take on this
        /// loop's frame, which it would zero on entry even for an empty collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void ValidateElements(IEnumerable<TElement> elements, string propertyName, ValidationFrame frame)
        {
            var run = frame.Run.Below(propertyName);
            var index = 0;

            // Where neither part declares a group, no selection can tell them apart.
            var (runRules, runValidator) = this.DeclaredGroups is null ? (true, true) : this.ChooseParts(run.Inherited.Groups);

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

                if (runRules)
                {
                    this.rules.ValidateInto(element, elementErrors, elementRun);
                }

                if (runValidator && this.elementValidator is IValidationRunAware<TElement> aware)
                {
                    aware.ValidateInto(element, elementErrors, elementRun);
                }

                Report(frame, scope, elementErrors);
            }
        }

        internal async ValueTask ValidateElementsAsync(IEnumerable<TElement> elements, string propertyName, ValidationFrame frame)
        {
            var run = frame.Run.Below(propertyName);
            var index = 0;

            var (runRules, runValidator) = this.DeclaredGroups is null ? (true, true) : this.ChooseParts(run.Inherited.Groups);

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

                if (runRules)
                {
                    await this.rules.ValidateIntoAsync(element, elementErrors, elementRun);
                }

                if (runValidator && this.elementValidator is { } validator)
                {
                    await NestedValidation.ValidateIntoAsync(validator, element, elementErrors, elementRun);
                }

                Report(frame, scope, elementErrors);
            }
        }

        private static string[]? NullIfEmpty(string[] groups)
        {
            return groups.Length == 0 ? null : groups;
        }

        /// <summary>
        /// The entries' own chains and their validator are one unit under a selection that leaves the default
        /// group out: where either declares a selected group, the part that declares none is passed over.
        /// Where neither does, the chain was selected by name, and both run as a plain call would run them.
        /// </summary>
        private (bool Rules, bool Validator) ChooseParts(ValidationGroups selection)
        {
            if (selection.IncludesDefault)
            {
                return (true, true);
            }

            var rulesTakePart = selection.Selects(this.rulesGroups);
            var validatorTakesPart = selection.Selects(this.validatorGroups);

            return rulesTakePart || validatorTakesPart ? (rulesTakePart, validatorTakesPart) : (true, true);
        }

        private void ThrowIfFrozen()
        {
            if (this.isFrozen)
            {
                throw new InvalidOperationException(
                    "The ForEach these rules belong to has already been declared, so they can no longer change. " +
                    "Declare them inside the ForEach.");
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

            public ConditionBlock<TElement> DeclareWhenBlock(Func<TElement, bool> condition, Action declareRules)
            {
                return this.When(condition, declareRules);
            }

            public ConditionBlock<TElement> DeclareUnlessBlock(Func<TElement, bool> condition, Action declareRules)
            {
                return this.Unless(condition, declareRules);
            }

            public void DeclareDataBlock<TData>(Func<TElement, TData, bool> condition, Action declareRules)
            {
                this.When(condition, declareRules);
            }
        }
    }
}
