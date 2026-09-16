namespace NValidation.Internals
{
    /// <summary>
    /// Turns the result of <see cref="IComparable{T}.CompareTo"/> into a verdict, and names the message
    /// each verdict reports under.
    /// </summary>
    internal static class Comparison
    {
        public static bool IsSatisfied(int comparison, ComparisonKind kind)
        {
            return kind switch
            {
                ComparisonKind.GreaterThan => comparison > 0,
                ComparisonKind.GreaterThanOrEqualTo => comparison >= 0,
                ComparisonKind.LessThan => comparison < 0,
                ComparisonKind.LessThanOrEqualTo => comparison <= 0,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        /// <summary>
        /// The key for the form which compares against a fixed value.
        /// </summary>
        public static string ValueErrorCode(ComparisonKind kind)
        {
            return kind switch
            {
                ComparisonKind.GreaterThan => ValidationErrorCodes.GreaterThan,
                ComparisonKind.GreaterThanOrEqualTo => ValidationErrorCodes.GreaterThanOrEqualTo,
                ComparisonKind.LessThan => ValidationErrorCodes.LessThan,
                ComparisonKind.LessThanOrEqualTo => ValidationErrorCodes.LessThanOrEqualTo,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        /// <summary>
        /// The key for the form which compares against another property, whose message names that
        /// property instead of a value.
        /// </summary>
        public static string OtherPropertyErrorCode(ComparisonKind kind)
        {
            return kind switch
            {
                ComparisonKind.GreaterThan => ValidationErrorCodes.GreaterThanOtherProperty,
                ComparisonKind.GreaterThanOrEqualTo => ValidationErrorCodes.GreaterThanOrEqualToOtherProperty,
                ComparisonKind.LessThan => ValidationErrorCodes.LessThanOtherProperty,
                ComparisonKind.LessThanOrEqualTo => ValidationErrorCodes.LessThanOrEqualToOtherProperty,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }
    }
}
