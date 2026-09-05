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
        public static string ValueMessageKey(ComparisonKind kind)
        {
            return kind switch
            {
                ComparisonKind.GreaterThan => ValidationMessageKeys.GreaterThan,
                ComparisonKind.GreaterThanOrEqualTo => ValidationMessageKeys.GreaterThanOrEqualTo,
                ComparisonKind.LessThan => ValidationMessageKeys.LessThan,
                ComparisonKind.LessThanOrEqualTo => ValidationMessageKeys.LessThanOrEqualTo,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

        /// <summary>
        /// The key for the form which compares against another property, whose message names that
        /// property instead of a value.
        /// </summary>
        public static string OtherPropertyMessageKey(ComparisonKind kind)
        {
            return kind switch
            {
                ComparisonKind.GreaterThan => ValidationMessageKeys.GreaterThanOtherProperty,
                ComparisonKind.GreaterThanOrEqualTo => ValidationMessageKeys.GreaterThanOrEqualToOtherProperty,
                ComparisonKind.LessThan => ValidationMessageKeys.LessThanOtherProperty,
                ComparisonKind.LessThanOrEqualTo => ValidationMessageKeys.LessThanOrEqualToOtherProperty,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }
    }
}
