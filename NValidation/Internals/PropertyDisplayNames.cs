namespace NValidation.Internals
{
    /// <summary>
    /// What a message calls each property: the name it opted into with <c>WithDisplayName</c>, or its
    /// member name. Resolved while the message is produced, so the request's culture applies.
    /// </summary>
    internal sealed class PropertyDisplayNames
    {
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
