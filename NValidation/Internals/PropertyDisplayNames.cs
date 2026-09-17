namespace NValidation.Internals
{
    /// <summary>
    /// The name a message shows for a property: the display name the property opted into with
    /// <c>WithDisplayName(...)</c>, or — the default — its property name, i.e. its C# member name.
    /// </summary>
    /// <remarks>
    /// Built once per validator, when its rules freeze; a display name is stored as a <see cref="Func{TResult}"/>
    /// and resolved while the message is produced, so the culture of the current request is accounted
    /// for. Where the same property is declared more than once, the display name written last wins.
    /// </remarks>
    internal sealed class PropertyDisplayNames
    {
        /// <summary>
        /// No property opted into a display name, so every message names its property by its name.
        /// </summary>
        public static PropertyDisplayNames None { get; } = new(new Dictionary<string, Func<string>>(0, StringComparer.Ordinal));

        private readonly IReadOnlyDictionary<string, Func<string>> displayNames;

        private PropertyDisplayNames(IReadOnlyDictionary<string, Func<string>> displayNames)
        {
            this.displayNames = displayNames;
        }

        public static PropertyDisplayNames For<T>(IReadOnlyList<IPropertyRule<T>> rules)
        {
            Dictionary<string, Func<string>>? displayNames = null;

            foreach (var rule in rules)
            {
                if (rule.DisplayName != null)
                {
                    displayNames ??= new Dictionary<string, Func<string>>(StringComparer.Ordinal);
                    displayNames[rule.PropertyName] = rule.DisplayName;
                }
            }

            return displayNames == null ? None : new PropertyDisplayNames(displayNames);
        }

        public string Resolve(string propertyName)
        {
            return this.displayNames.TryGetValue(propertyName, out var displayName) ? displayName() : propertyName;
        }
    }
}
