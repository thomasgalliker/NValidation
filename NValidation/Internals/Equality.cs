namespace NValidation.Internals
{
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

        public static string ValueErrorCode(EqualityKind kind)
        {
            return kind switch
            {
                EqualityKind.EqualTo => ValidationErrorCodes.EqualTo,
                EqualityKind.NotEqualTo => ValidationErrorCodes.NotEqualTo,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };
        }

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
