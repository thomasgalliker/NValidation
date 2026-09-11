namespace NValidation.TestData.Ambiguous
{
    /// <summary>
    /// One of two validators for <see cref="AmbiguousPayload"/>. They differ in what they report, so a
    /// test can tell which one a registration resolved to.
    /// </summary>
    public sealed class FirstAmbiguousValidator : Validator<AmbiguousPayload>
    {
        public const string Code = "First";

        public FirstAmbiguousValidator()
        {
            this.Property(p => p.Name).WithErrorCode(Code).NotEmpty();
        }
    }

    /// <inheritdoc cref="FirstAmbiguousValidator"/>
    public sealed class SecondAmbiguousValidator : Validator<AmbiguousPayload>
    {
        public const string Code = "Second";

        public SecondAmbiguousValidator()
        {
            this.Property(p => p.Name).WithErrorCode(Code).NotEmpty();
        }
    }
}
