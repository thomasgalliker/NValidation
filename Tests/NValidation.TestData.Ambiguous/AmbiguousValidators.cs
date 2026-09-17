namespace NValidation.TestData.Ambiguous
{
    /// <summary>
    /// One of two validators for <see cref="AmbiguousPayload"/>. They differ in what they report, so a
    /// test can tell which one a registration resolved to.
    /// </summary>
    public sealed class FirstAmbiguousValidator : Validator<AmbiguousPayload>
    {
        public const string PropertyName = "First";

        public FirstAmbiguousValidator()
        {
            this.Property(p => p.Name).WithPropertyName(PropertyName).NotEmpty();
        }
    }

    public sealed class SecondAmbiguousValidator : Validator<AmbiguousPayload>
    {
        public const string PropertyName = "Second";

        public SecondAmbiguousValidator()
        {
            this.Property(p => p.Name).WithPropertyName(PropertyName).NotEmpty();
        }
    }
}
