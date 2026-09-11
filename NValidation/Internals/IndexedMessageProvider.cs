using System.Globalization;

namespace NValidation.Internals
{
    /// <summary>
    /// Hands a rule the position of the element it is judging, so a message can name the row it is
    /// about, and names the element itself where a rule has no property to name.
    /// </summary>
    /// <remarks>
    /// Wraps the host's provider for the duration of one element rather than assigning it, because a
    /// validator is shared and may be running against more than one object at a time.
    /// </remarks>
    internal sealed class IndexedMessageProvider<TElement> : IValidationMessageProvider
    {
        private readonly IValidationMessageProvider inner;

        private readonly string collectionCode;

        private readonly TElement element;

        private readonly int index;

        private readonly Func<TElement, int, string>? indexer;

        private string? elementCode;

        public IndexedMessageProvider(
            IValidationMessageProvider inner,
            string collectionCode,
            TElement element,
            int index,
            Func<TElement, int, string>? indexer)
        {
            this.inner = inner;
            this.collectionCode = collectionCode;
            this.element = element;
            this.index = index;
            this.indexer = indexer;
        }

        /// <summary>
        /// What this element's failures are reported under — <c>ServiceHistory[2]</c>, or
        /// <c>ServiceHistory[INV-9912]</c> where the rules identified the elements themselves.
        /// </summary>
        /// <remarks>
        /// Built on first use: naming the entry costs two strings — the identity and the code around it
        /// — and an entry with nothing to say about it never needs naming.
        /// </remarks>
        public string ElementCode
        {
            get { return this.elementCode ??= $"{this.collectionCode}[{this.Identify()}]"; }
        }

        public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
        {
            var withIndex = new Dictionary<string, object?>(arguments.Count + 1, StringComparer.Ordinal);

            foreach (var argument in arguments)
            {
                withIndex[argument.Key] = argument.Value;
            }

            // Only where no inner element already answered: for a collection inside a collection the
            // message is about the innermost entry, not about the row that entry hangs off.
            if (!withIndex.ContainsKey(ValidationMessagePlaceholders.CollectionIndex))
            {
                withIndex[ValidationMessagePlaceholders.CollectionIndex] = this.index;
            }

            // A rule declared for the element itself names no property, so {PropertyName} would
            // substitute to nothing and the message would open with a space. The code the failure is
            // reported under is the subject it is missing.
            if (arguments.TryGetValue(ValidationMessagePlaceholders.PropertyName, out var propertyName) &&
                propertyName is string { Length: 0 })
            {
                withIndex[ValidationMessagePlaceholders.PropertyName] = this.ElementCode;
            }

            return this.inner.GetMessage(messageKey, withIndex);
        }

        private string Identify()
        {
            return this.indexer == null
                ? this.index.ToString(CultureInfo.InvariantCulture)
                : this.indexer(this.element, this.index);
        }
    }
}
