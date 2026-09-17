using System.Globalization;

namespace NValidation.Internals
{
    /// <summary>
    /// The entry of a collection currently under judgement: what only the entry knows and a rule inside
    /// it needs — its position, and the property name its failures are reported under.
    /// </summary>
    /// <remarks>
    /// One instance serves a whole collection, pointed at each entry in turn with <c>MoveTo</c>; a scope
    /// per entry was an allocation for every row of every validation. Safe because entries are judged
    /// one after another, and everything an entry reported is copied out under its own name before the
    /// next entry is looked at.
    /// </remarks>
    internal abstract class ElementScope
    {
        private readonly string collectionPropertyName;

        private string? elementPropertyName;

        protected ElementScope(string collectionPropertyName)
        {
            this.collectionPropertyName = collectionPropertyName;
        }

        /// <summary>
        /// The entry's zero-based position.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// What this entry's failures are reported under — <c>ServiceHistory[2]</c>, or
        /// <c>ServiceHistory[INV-9912]</c> where the rules identified the entries themselves. Built on
        /// first use, so an entry with nothing to report is never named.
        /// </summary>
        public string ElementPropertyName => this.elementPropertyName ??= $"{this.collectionPropertyName}[{this.Identify()}]";

        /// <summary>
        /// Adds to a rule's arguments what only the entry knows: its position, and — for a rule declared
        /// on the entry itself, which names no property — the property name the failure is reported
        /// under, so <c>{PropertyName}</c> does not render to nothing.
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

    /// <inheritdoc cref="ElementScope"/>
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
