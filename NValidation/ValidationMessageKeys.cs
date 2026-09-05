namespace NValidation
{
    /// <summary>
    /// The message keys of the rules shipped with this validation core. A host resolves them through
    /// its own <see cref="IValidationMessageProvider"/>; rules never reference a resource directly, so
    /// the core carries no dependency on the application's resources.
    /// </summary>
    /// <remarks>
    /// Messages are formatted with named placeholders, not positional ones: every rule supplies
    /// <see cref="ValidationMessagePlaceholders.PropertyName"/> plus whichever of its own arguments it has
    /// (e.g. <c>{MaxLength}</c>), and a message uses the ones it needs and ignores the rest.
    /// </remarks>
    public static class ValidationMessageKeys
    {
        public const string NotEmpty = "NotEmpty";
        public const string NotNull = "NotNull";

        /// <summary>
        /// A value type was left at its default, i.e. nothing was ever chosen. Distinct from
        /// <see cref="NotEmpty"/>, which is about a value that is present but has no content.
        /// </summary>
        public const string NotDefault = "NotDefault";
        public const string NotNaN = "NotNaN";

        public const string MinimumLength = "MinimumLength";
        public const string MaximumLength = "MaximumLength";
        public const string Length = "Length";
        public const string LengthBetween = "LengthBetween";
        public const string Matches = "Matches";
        public const string EmailAddress = "EmailAddress";

        public const string GreaterThan = "GreaterThan";
        public const string GreaterThanOrEqualTo = "GreaterThanOrEqualTo";
        public const string LessThan = "LessThan";
        public const string LessThanOrEqualTo = "LessThanOrEqualTo";
        public const string Between = "Between";
        public const string EqualTo = "EqualTo";
        public const string NotEqualTo = "NotEqualTo";

        /// <summary>
        /// The comparison keys for the form which compares against another property of the same object.
        /// They are separate keys because their message names that property
        /// (<see cref="ValidationMessagePlaceholders.OtherPropertyName"/>) rather than a value.
        /// </summary>
        public const string GreaterThanOtherProperty = "GreaterThanOtherProperty";

        public const string GreaterThanOrEqualToOtherProperty = "GreaterThanOrEqualToOtherProperty";
        public const string LessThanOtherProperty = "LessThanOtherProperty";
        public const string LessThanOrEqualToOtherProperty = "LessThanOrEqualToOtherProperty";
        public const string EqualToOtherProperty = "EqualToOtherProperty";
        public const string NotEqualToOtherProperty = "NotEqualToOtherProperty";

        public const string MultipleOf = "MultipleOf";

        public const string InThePast = "InThePast";
        public const string InTheFuture = "InTheFuture";

        public const string IsInEnum = "IsInEnum";

        public const string MinimumCount = "MinimumCount";
        public const string MaximumCount = "MaximumCount";
        public const string NoDuplicates = "NoDuplicates";
    }
}
