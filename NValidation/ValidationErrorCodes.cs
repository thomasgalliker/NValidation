namespace NValidation
{
    /// <summary>
    /// The error codes of the rules shipped with this validation core. A host resolves them through
    /// its own <see cref="IValidationMessageProvider"/>; rules never reference a resource directly, so
    /// the core carries no dependency on the application's resources.
    /// </summary>
    /// <remarks>
    /// Messages are formatted with named placeholders, not positional ones: every rule supplies
    /// <see cref="ValidationMessagePlaceholders.PropertyName"/> plus whichever of its own arguments it has
    /// (e.g. <c>{MaxLength}</c>), and a message uses the ones it needs and ignores the rest.
    /// </remarks>
    public static class ValidationErrorCodes
    {
        /// <summary>
        /// A rule the caller wrote with <c>Must</c> and did not name. Give it a code of its own with
        /// <c>WithErrorCode</c> where a client has to tell it from another.
        /// </summary>
        public const string Must = "Must";

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

        /// <summary>
        /// A mail address is not under one of the top-level domains the rule allows.
        /// </summary>
        public const string EmailTopLevelDomain = "EmailTopLevelDomain";

        /// <summary>
        /// A mail address is under a top-level domain the rule refuses.
        /// </summary>
        public const string EmailTopLevelDomainNotAllowed = "EmailTopLevelDomainNotAllowed";

        /// <summary>
        /// A value is not a URL of the shape the rule accepts. The refinements that admit a further shape
        /// widen this one failure rather than reporting one each.
        /// </summary>
        public const string Url = "Url";

        /// <summary>
        /// A URL does not use one of the schemes the rule allows. Reported for a value that is otherwise a
        /// well-formed URL, so an <c>ftp://</c> address is told apart from text that is not a URL at all.
        /// </summary>
        public const string UrlScheme = "UrlScheme";

        /// <summary>
        /// A URL does not point at one of the hosts the rule allows, whether named exactly or as a domain
        /// to sit under.
        /// </summary>
        public const string UrlHost = "UrlHost";

        /// <summary>
        /// Text carries one of the terms a rule refuses. The message names none of them: a blocklist
        /// which reports its own entries is one the next value works around.
        /// </summary>
        public const string NotContaining = "NotContaining";

        public const string GreaterThan = "GreaterThan";
        public const string GreaterThanOrEqualTo = "GreaterThanOrEqualTo";
        public const string LessThan = "LessThan";
        public const string LessThanOrEqualTo = "LessThanOrEqualTo";
        public const string Between = "Between";

        /// <summary>
        /// The <c>Between</c> keys for the forms which exclude a bound. They are separate keys because
        /// their message has to name a bound the rule refuses, and a single template cannot branch on
        /// which one that is.
        /// </summary>
        public const string BetweenExclusive = "BetweenExclusive";

        /// <summary>
        /// The lower bound is excluded and the upper one included.
        /// </summary>
        public const string BetweenExclusiveFrom = "BetweenExclusiveFrom";

        /// <summary>
        /// The lower bound is included and the upper one excluded.
        /// </summary>
        public const string BetweenExclusiveTo = "BetweenExclusiveTo";
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

        /// <summary>
        /// A decimal carries more digits than the contract behind it holds.
        /// </summary>
        public const string PrecisionScale = "PrecisionScale";

        /// <summary>
        /// A value is not one of the set the rule allows. Unlike <see cref="NotContaining"/>, the
        /// message names them: an allowlist may say what it allows, while a blocklist that reports its
        /// own entries is one the next value works around.
        /// </summary>
        public const string OneOf = "OneOf";

        public const string InThePast = "InThePast";
        public const string InTheFuture = "InTheFuture";

        public const string IsInEnum = "IsInEnum";

        public const string MinimumCount = "MinimumCount";
        public const string MaximumCount = "MaximumCount";
        public const string NoDuplicates = "NoDuplicates";
    }
}
