namespace NValidation.TestData.Validators
{
    public sealed class ServiceRecordValidator : Validator<ServiceRecord>
    {
        public ServiceRecordValidator()
        {
            this.Property(r => r.Workshop)
                .NotEmpty()
                .MaximumLength(100);

            this.Property(r => r.Mileage)
                .GreaterThanOrEqualTo(0);

            this.Property(r => r.Cost)
                .GreaterThan(0m);
        }
    }
}
