using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// Values a caller hands the rules of one validation — facts about the request that the object itself
    /// cannot answer, such as the market a car is offered in — each asked for by its type.
    /// </summary>
    /// <remarks>
    /// Immutable, and written as a collection expression:
    /// <c>ValidationData = [new ListingPolicy(MaximumMileage: 200_000, RequiresServiceHistory: true)]</c>.
    /// A rule reads a value with <c>When&lt;TData&gt;</c>, <c>Must&lt;TData&gt;</c> or
    /// <see cref="RuleContext{T, TProperty}.TryGetData{TData}"/>; declaring a type of one's own for it keeps
    /// two unrelated values from answering the same question.
    /// </remarks>
    [CollectionBuilder(typeof(ValidationData), nameof(Create))]
    public sealed class ValidationData : IEnumerable<object>
    {
        private readonly object[] values;

        private CallInputs? inputs;

        private ValidationData(object[] values)
        {
            this.values = values;
        }

        /// <summary>
        /// Hands the rules <paramref name="values"/>, each found by its type.
        /// </summary>
        /// <exception cref="ArgumentException">A value is <c>null</c>, or two values are of the same type.</exception>
        public ValidationData(params ReadOnlySpan<object> values)
            : this(Copy(values))
        {
        }

        /// <summary>
        /// No value at all, so every rule asking for one passes it by.
        /// </summary>
        public static ValidationData Empty { get; } = new(Array.Empty<object>());

        /// <summary>
        /// How many values the rules are handed.
        /// </summary>
        public int Count => this.values.Length;

        /// <summary>
        /// Hands the rules <paramref name="values"/>, for a collection expression:
        /// <c>ValidationData data = [new ListingPolicy(200_000, true)];</c>
        /// </summary>
        /// <inheritdoc cref="ValidationData(ReadOnlySpan{object})" path="/exception"/>
        public static ValidationData Create(ReadOnlySpan<object> values)
        {
            return values.Length == 0 ? Empty : new ValidationData(values);
        }

        /// <summary>
        /// Finds the value that is a <typeparamref name="TData"/> — of that type, derived from it, or
        /// implementing it — and returns <c>false</c> where there is none.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Two of the values are a <typeparamref name="TData"/>, so which one is meant cannot be told.
        /// </exception>
        public bool TryGet<TData>([MaybeNullWhen(false)] out TData value)
        {
            var values = this.values;
            var found = -1;

            // Walked to the end rather than stopping at the first match: a second one would make the
            // answer depend on the order the caller happened to write, which is the one thing it must not.
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] is not TData)
                {
                    continue;
                }

                if (found >= 0)
                {
                    throw new InvalidOperationException(
                        $"Both a {values[found].GetType().GetFormattedFullName()} and a " +
                        $"{values[i].GetType().GetFormattedFullName()} were handed over as " +
                        $"{typeof(TData).GetFormattedFullName()}, so a rule asking for one cannot tell which is meant. " +
                        "Ask for a type only one of them is.");
                }

                found = i;
            }

            if (found < 0)
            {
                value = default;

                return false;
            }

            value = (TData)values[found];

            return true;
        }

        /// <inheritdoc/>
        public IEnumerator<object> GetEnumerator()
        {
            return ((IEnumerable<object>)this.values).GetEnumerator();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.values.Length == 0
                ? "None"
                : string.Join(", ", this.values.Select(value => value.GetType().Name));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// This data paired with the selection of a call, kept for the next call that pairs the two again:
        /// one entry, so options built once and reused allocate nothing per call.
        /// </summary>
        internal CallInputs InputsFor(ValidationGroups groups)
        {
            var cached = this.inputs;

            if (cached != null && ReferenceEquals(cached.Groups, groups))
            {
                return cached;
            }

            cached = new CallInputs(groups, this, properties: null);
            this.inputs = cached;

            return cached;
        }

        private static object[] Copy(ReadOnlySpan<object> values)
        {
            var copy = values.ToArray();

            for (var i = 0; i < copy.Length; i++)
            {
                if (copy[i] is null)
                {
                    throw new ArgumentException(
                        "A value cannot be null: a rule asks for data by its type, and null has none.", nameof(values));
                }

                for (var j = 0; j < i; j++)
                {
                    if (copy[j].GetType() == copy[i].GetType())
                    {
                        throw new ArgumentException(
                            $"Two values of type {copy[i].GetType().GetFormattedFullName()} were handed over, so a rule " +
                            "asking for that type could not tell them apart.",
                            nameof(values));
                    }
                }
            }

            return copy;
        }
    }
}
