namespace NValidation.Internals
{
    /// <summary>
    /// Turns "these two are equal" into a verdict, and names the message each verdict reports under.
    /// </summary>
    internal static class Equality
    {
        public static bool IsSatisfied(bool areEqual, EqualityKind kind)
        {
            return kind switch
            {
                EqualityKind.EqualTo => areEqual,
                EqualityKind.NotEqualTo => !areEqual,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        /// <summary>
        /// The key for the form which compares against a fixed value.
        /// </summary>
        public static string ValueMessageKey(EqualityKind kind)
        {
            return kind switch
            {
                EqualityKind.EqualTo => ValidationMessageKeys.EqualTo,
                EqualityKind.NotEqualTo => ValidationMessageKeys.NotEqualTo,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        /// <summary>
        /// The key for the form which compares against another property, whose message names that
        /// property instead of a value.
        /// </summary>
        public static string OtherPropertyMessageKey(EqualityKind kind)
        {
            return kind switch
            {
                EqualityKind.EqualTo => ValidationMessageKeys.EqualToOtherProperty,
                EqualityKind.NotEqualTo => ValidationMessageKeys.NotEqualToOtherProperty,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }
    }
}
