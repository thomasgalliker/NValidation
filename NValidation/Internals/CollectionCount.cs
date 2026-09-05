using System.Collections;

namespace NValidation.Internals
{
    /// <summary>
    /// Asks a collection about its size, so a collection rule works whatever the property is declared as.
    /// </summary>
    /// <remarks>
    /// A property may be declared as a bare <see cref="IEnumerable"/> backed by a query or a
    /// <c>yield return</c> iterator, which is enumerated afresh — or only once — every time it is read.
    /// So each method here walks the sequence no further than its own question needs, and a rule asks
    /// exactly one question.
    /// </remarks>
    internal static class CollectionCount
    {
        /// <summary>
        /// The number of entries. A collection that knows its own size is asked for it; anything else is
        /// enumerated once.
        /// </summary>
        public static int Of(IEnumerable value)
        {
            if (TryGetCount(value, out var knownCount))
            {
                return knownCount;
            }

            var count = 0;

            foreach (var _ in value)
            {
                count++;
            }

            return count;
        }

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
        /// Whether the count is known without enumerating. Arrays, <see cref="List{T}"/> and the other
        /// built-in collections implement the non-generic <see cref="ICollection"/> and report it; a
        /// sequence which does not is enumerated, which is safe because each caller enumerates it at
        /// most once.
        /// </summary>
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
