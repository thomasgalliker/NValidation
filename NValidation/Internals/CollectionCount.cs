using System.Collections;

namespace NValidation.Internals
{
    /// <summary>
    /// Asks a collection about its size, so a collection rule works whatever the property is declared as.
    /// </summary>
    /// <remarks>
    /// A property may be declared as a bare <see cref="IEnumerable"/> backed by a query or a
    /// <c>yield return</c> iterator, which is enumerated afresh — or only once — every time it is read.
    /// So each method here walks the sequence no further than its own question needs: a collection that
    /// knows its own size is asked for it, and one that does not is walked only until the answer is
    /// settled.
    /// <para>
    /// One rule therefore walks the sequence at most once, but a <em>chain</em> of collection rules asks
    /// one question each and so walks it once per rule. A property backed by a live query, or by a
    /// sequence that cannot be enumerated twice, has to be materialized by the caller — this library
    /// cannot do it without changing the value the caller's own rules see.
    /// </para>
    /// </remarks>
    internal static class CollectionCount
    {
        /// <summary>
        /// Whether there is at least one entry, without counting the rest.
        /// </summary>
        public static bool IsEmpty(IEnumerable value)
        {
            if (TryGetCount(value, out var knownCount))
            {
                return knownCount == 0;
            }

            var enumerator = value.GetEnumerator();

            try
            {
                return !enumerator.MoveNext();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// Whether there are at least <paramref name="count"/> entries, without walking past the one that
        /// settles it.
        /// </summary>
        public static bool HasAtLeast(IEnumerable value, int count)
        {
            if (count <= 0)
            {
                return true;
            }

            if (TryGetCount(value, out var knownCount))
            {
                return knownCount >= count;
            }

            var seen = 0;

            foreach (var _ in value)
            {
                if (++seen >= count)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether there are no more than <paramref name="count"/> entries. One entry past the cap is
        /// enough to answer, so a sequence which busts it is not walked to its end.
        /// </summary>
        public static bool HasAtMost(IEnumerable value, int count)
        {
            if (TryGetCount(value, out var knownCount))
            {
                return knownCount <= count;
            }

            var seen = 0;

            foreach (var _ in value)
            {
                if (++seen > count)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether the count is known without enumerating.
        /// </summary>
        /// <remarks>
        /// Arrays, <see cref="List{T}"/> and the other built-in collections implement the non-generic
        /// <see cref="ICollection"/> and report it. A few — <see cref="HashSet{T}"/> among them — only
        /// implement the generic one, whose <c>Count</c> cannot be reached without knowing the element
        /// type, so they are walked instead. That is bounded by the question being asked, and reaching
        /// the generic interface from here would take the reflection this library avoids.
        /// </remarks>
        private static bool TryGetCount(IEnumerable value, out int count)
        {
            if (value is ICollection collection)
            {
                count = collection.Count;
                return true;
            }

            count = 0;
            return false;
        }
    }
}
