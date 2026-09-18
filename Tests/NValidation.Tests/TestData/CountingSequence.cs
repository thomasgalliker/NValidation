namespace NValidation.Tests.TestData
{
    /// <summary>
    /// A sequence which counts how far it was walked, so a rule that enumerates more than its question
    /// needs is visible rather than merely slow.
    /// </summary>
    internal sealed class CountingSequence(IReadOnlyList<int> entries) : IEnumerable<int>
    {
        public int Enumerated { get; private set; }

        /// <summary>
        /// How many times the sequence was walked from the start, which is what a chain of collection
        /// rules costs a property backed by a query.
        /// </summary>
        public int Passes { get; private set; }

        public IEnumerator<int> GetEnumerator()
        {
            this.Passes++;

            foreach (var entry in entries)
            {
                this.Enumerated++;

                yield return entry;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
