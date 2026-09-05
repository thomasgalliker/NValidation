namespace NValidation
{
    /// <summary>
    /// The call shape a rule uses: the failing property plus whatever arguments the rule itself has.
    /// </summary>
    public static class ValidationMessageProviderExtensions
    {
        /// <summary>
        /// Resolves a message for <paramref name="propertyName"/>, which is passed as
        /// <see cref="ValidationMessagePlaceholders.PropertyName"/> on top of the rule's own
        /// <paramref name="arguments"/>.
        /// </summary>
        public static string GetMessage(
            this IValidationMessageProvider provider,
            string messageKey,
            string propertyName,
            params (string Name, object? Value)[] arguments)
        {
            ArgumentNullException.ThrowIfNull(provider);

            return provider.GetMessage(messageKey, BuildArguments(propertyName, arguments));
        }

        internal static IReadOnlyDictionary<string, object?> BuildArguments(string propertyName, params (string Name, object? Value)[] arguments)
        {
            var result = new Dictionary<string, object?>(arguments.Length + 1, StringComparer.Ordinal)
            {
                [ValidationMessagePlaceholders.PropertyName] = propertyName,
            };

            foreach (var (name, value) in arguments)
            {
                result[name] = value;
            }

            return result;
        }
    }
}
