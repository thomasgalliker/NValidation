namespace NValidation.TestData.Validators
{
    public sealed class ManufacturerValidator : Validator<Manufacturer>
    {
        internal const int NameMaximumLength = 100;
        internal const int CountryCodeLength = 3;

        public ManufacturerValidator()
        {
            this.Property(m => m.Name)
                .NotEmpty()
                .MaximumLength(NameMaximumLength);

            this.Property(m => m.CountryCode)
                .NotEmpty()
                .Length(CountryCodeLength);

            this.Property(m => m.FoundedDate)
                .InThePast();

            this.Property(m => m.ContactEmail)
                .EmailAddress();
        }
    }
}
