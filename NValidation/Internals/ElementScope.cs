using System.Globalization;

namespace NValidation.Internals
{
    internal abstract class ElementScope
    {
        private readonly string collectionPropertyName;

        private string? elementPropertyName;

        protected ElementScope(string collectionPropertyName)
        {
            this.collectionPropertyName = collectionPropertyName;
        }

        public int Index { get; private set; }

        /// <summary>
        /// What this entry's failures are reported under — ServiceHistory[2], or ServiceHistory[INV-9912]
        /// where the rules identified the entries themselves. Built on first use, so an entry with nothing to
        /// report is never named.
        /// </summary>
        public string ElementPropertyName => this.elementPropertyName ??= $"{this.collectionPropertyName}[{this.Identify()}]";

        /// <summary>
        /// Adds to a rule's arguments what only the entry knows: its position, and — for a rule declared on
        /// the entry itself, which names no property — the property name the failure is reported under, so
        /// {PropertyName} does not render to nothing.
        /// </summary>
        public void Enrich(Dictionary<string, object?> arguments)
        {
            arguments.TryAdd(ValidationMessagePlaceholders.CollectionIndex, this.Index);

            if (arguments.TryGetValue(ValidationMessagePlaceholders.PropertyName, out var propertyName) &&
                propertyName is string { Length: 0 })
            {
                arguments[ValidationMessagePlaceholders.PropertyName] = this.ElementPropertyName;
            }
        }

        protected void MoveTo(int index)
        {
            this.Index = index;
            this.elementPropertyName = null;
        }

        protected abstract string Identify();
    }

    internal sealed class ElementScope<TElement> : ElementScope
    {
        private readonly Func<TElement, int, string>? indexer;

        private TElement element = default!;

        public ElementScope(string collectionPropertyName, Func<TElement, int, string>? indexer)
            : base(collectionPropertyName)
        {
            this.indexer = indexer;
        }

        public void MoveTo(TElement element, int index)
        {
            this.element = element;
            this.MoveTo(index);
        }

        protected override string Identify()
        {
            return this.indexer == null
                ? this.Index.ToString(CultureInfo.InvariantCulture)
                : this.indexer(this.element, this.Index);
        }
    }
}
