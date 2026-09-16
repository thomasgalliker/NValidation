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
        public static string ValueErrorCode(EqualityKind kind)
        {
            return kind switch
            {
                EqualityKind.EqualTo => ValidationErrorCodes.EqualTo,
                EqualityKind.NotEqualTo => ValidationErrorCodes.NotEqualTo,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        /// <summary>
        /// The key for the form which compares against another property, whose message names that
        /// property instead of a value.
        /// </summary>
        public static string OtherPropertyErrorCode(EqualityKind kind)
        {
            return kind switch
            {
                EqualityKind.EqualTo => ValidationErrorCodes.EqualToOtherProperty,
                EqualityKind.NotEqualTo => ValidationErrorCodes.NotEqualToOtherProperty,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }
    }
}
