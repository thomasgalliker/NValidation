using System.Collections;
using NValidation.Internals;

namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires at least <paramref name="minimumCount"/> entries. A missing collection passes;
        /// use <c>NotEmpty()</c> or <c>NotNull()</c> to require one.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.MinimumCount"/> with
        /// <see cref="ValidationMessagePlaceholders.MinCount"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumCount"/> is negative.</exception>
        public static PropertyRuleBuilder<T, TCollection> MinimumCount<T, TCollection>(this PropertyRuleBuilder<T, TCollection> builder, int minimumCount)
            where TCollection : IEnumerable?
        {
            ArgumentOutOfRangeException.ThrowIfNegative(minimumCount);

            return builder.Add(context =>
            {
                if (context.Value != null && !CollectionCount.HasAtLeast(context.Value, minimumCount))
                {
                    context.AddError(ValidationErrorCodes.MinimumCount, (ValidationMessagePlaceholders.MinCount, minimumCount));
                }
            });
        }

        /// <summary>
        /// Caps the number of entries at <paramref name="maximumCount"/>. A missing collection passes.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.MaximumCount"/> with
        /// <see cref="ValidationMessagePlaceholders.MaxCount"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximumCount"/> is negative.</exception>
        public static PropertyRuleBuilder<T, TCollection> MaximumCount<T, TCollection>(this PropertyRuleBuilder<T, TCollection> builder, int maximumCount)
            where TCollection : IEnumerable?
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maximumCount);

            return builder.Add(context =>
            {
                if (context.Value != null && !CollectionCount.HasAtMost(context.Value, maximumCount))
                {
                    context.AddError(ValidationErrorCodes.MaximumCount, (ValidationMessagePlaceholders.MaxCount, maximumCount));
                }
            });
        }

        /// <summary>
        /// Requires every entry to be distinct — a list of ids a client assembled from a
        /// multi-select, typically.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.NoDuplicates"/>.
        /// Entries are compared by their own <see cref="object.Equals(object)"/>, and a collection of value
        /// types has each entry boxed on the way into the set. Where a collection needs a comparison of its
        /// own, or a hot path needs the entries at their declared type, write it as a <c>Must(...)</c> over
        /// a <see cref="HashSet{T}"/>.
        /// </remarks>
        public static PropertyRuleBuilder<T, TCollection> NoDuplicates<T, TCollection>(this PropertyRuleBuilder<T, TCollection> builder)
            where TCollection : IEnumerable?
        {
            return builder.Add(context =>
            {
                if (context.Value != null && HasDuplicates(context.Value))
                {
                    context.AddError(ValidationErrorCodes.NoDuplicates);
                }
            });
        }

        private static bool HasDuplicates(IEnumerable value)
        {
            // Sized up front where the collection knows its own size, so a large one does not pay for
            // the set growing underneath it.
            var seen = value is ICollection collection ? new HashSet<object?>(collection.Count) : [];

            foreach (var entry in value)
            {
                if (!seen.Add(entry))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Declares the rules every element of this collection has to satisfy:
        /// <code>this.Property(x => x.Cars).ForEach(car => car.Property(c => c.Name).NotEmpty());</code>
        /// Each failure is reported under the element's position — <c>Cars[2].Name</c> — so a caller can
        /// bind it to the row it came from.
        /// </summary>
        /// <remarks>
        /// Declare it last in the chain: it answers about the elements rather than about the collection, so
        /// it returns nothing to chain further rules onto. Rules about the collection itself — how many
        /// entries, whether they are distinct — go before it. A missing collection is skipped, as is a
        /// <c>null</c> element, and the collection is enumerated once.
        /// </remarks>
        public static void ForEach<TElement>(
            this IPropertyRuleTarget<IEnumerable<TElement>> target,
            Action<ElementRuleBuilder<TElement>> declareRules)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(declareRules);

            var elements = new ElementRuleBuilder<TElement>();
            declareRules(elements);

            target.AddElementRule(elements);
        }

        /// <summary>
        /// Validates every element with its own validator — the form to reach for when the element
        /// already has one:
        /// <code>this.Property(x => x.Cars).ForEach(new CarValidator());</code>
        /// </summary>
        /// <inheritdoc cref="ForEach{TElement}(IPropertyRuleTarget{IEnumerable{TElement}}, Action{ElementRuleBuilder{TElement}})" path="/remarks"/>
        public static void ForEach<TElement>(
            this IPropertyRuleTarget<IEnumerable<TElement>> target,
            IValidator<TElement> validator)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(validator);

            var elements = new ElementRuleBuilder<TElement>();
            elements.SetValidator(validator);

            target.AddElementRule(elements);
        }
    }
}
