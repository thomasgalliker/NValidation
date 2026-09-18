using System.Collections;

namespace NValidation.Internals
{
    /// <summary>
    /// Asks a collection its size, walking the sequence no further than the question needs. A chain of
    /// collection rules asks one question each, so it walks the sequence once per rule.
    /// </summary>
    internal static class CollectionCount
    {
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
        /// Whether the count is known without enumerating. <c>HashSet</c> and friends implement only the
        /// generic <c>ICollection</c>, whose <c>Count</c> needs the element type, so they are walked instead.
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
