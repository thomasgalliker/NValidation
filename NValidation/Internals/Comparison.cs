namespace NValidation.Internals
{
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
