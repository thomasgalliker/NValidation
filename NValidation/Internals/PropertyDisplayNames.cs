namespace NValidation.Internals
{
    /// <summary>
    /// The name a message shows for a property: the display name the property opted into with
    /// <c>WithDisplayName(...)</c>, or — the default — its code, i.e. its C# property name.
    /// </summary>
    /// <remarks>
    /// Built per validation rather than per validator, because a localized display name has to be
    /// resolved in the culture of the current request. Where the same property is declared more than
    /// once, the display name written last wins.
    /// </remarks>
    internal sealed class PropertyDisplayNames
    {
        private static readonly PropertyDisplayNames None = new(new Dictionary<string, Func<string>>(0, StringComparer.Ordinal));

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
                    displayNames[rule.Code] = rule.DisplayName;
                }
            }

            return displayNames == null ? None : new PropertyDisplayNames(displayNames);
        }

        public string Resolve(string code)
        {
            return this.displayNames.TryGetValue(code, out var displayName) ? displayName() : code;
        }
    }
}
