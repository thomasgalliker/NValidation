namespace NValidation
{
    /// <summary>
    /// The placeholder names a rule makes available to its message. A message uses the ones it needs
    /// and ignores the rest, so the wording — and each translation of it — is free to mention the
    /// property or not.
    /// </summary>
    public static class ValidationMessagePlaceholders
    {
        /// <summary>
        /// The failing property, supplied for every rule. A message shown underneath an already
        /// labelled input usually reads better without it.
        /// </summary>
        public const string PropertyName = "PropertyName";

        /// <summary>
        /// The position of the element a rule is judging, for a message about one entry of a
        /// collection. Zero-based, matching the index its error code reports.
        /// </summary>
        public const string CollectionIndex = "CollectionIndex";

        public const string MinLength = "MinLength";
        public const string MaxLength = "MaxLength";
        public const string Length = "Length";
        public const string Pattern = "Pattern";
        public const string From = "From";
        public const string To = "To";
        public const string Step = "Step";
        public const string MinCount = "MinCount";
        public const string MaxCount = "MaxCount";

        /// <summary>
        /// The value a property was compared against, e.g. the lower bound of a <c>GreaterThan</c>.
        /// </summary>
        public const string OtherValue = "OtherValue";

        /// <summary>
        /// The property a value was compared against, e.g. the start date an end date must follow.
        /// Carries that property's display name when it declared one.
        /// </summary>
        public const string OtherPropertyName = "OtherPropertyName";
    }
}
