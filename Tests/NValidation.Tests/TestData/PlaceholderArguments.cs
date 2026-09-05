namespace NValidation.Tests.TestData
{
    /// <summary>
    /// Builds a message-argument bag holding every declared placeholder. A message may name any subset
    /// of them, so supplying all of them lets a test prove the opposite direction: that a template names
    /// nothing a rule does not actually supply.
    /// </summary>
    internal static class PlaceholderArguments
    {
        /// <summary>
        /// Stands in for the failing property. Distinctive enough that asserting its presence — or its
        /// absence — cannot collide with the wording of a message.
        /// </summary>
        public const string PropertyName = "TheFailingProperty";

        /// <summary>
        /// Matches a placeholder which survived formatting, i.e. one the arguments did not cover.
        /// </summary>
        public const string UnresolvedPlaceholderPattern = @"\{\w+(:[^}]+)?\}";

        public static IReadOnlyDictionary<string, object?> All(params string[] additionalPlaceholders)
        {
            var arguments = DeclaredNames(typeof(ValidationMessagePlaceholders))
                .Concat(additionalPlaceholders)
                .ToDictionary(name => name, object? (name) => name, StringComparer.Ordinal);

            arguments[ValidationMessagePlaceholders.PropertyName] = PropertyName;

            return arguments;
        }

        private static IEnumerable<string> DeclaredNames(Type placeholders)
        {
            return placeholders
                .GetFields()
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!);
        }
    }
}
