namespace NValidation.TestData.Validators
{
    /// <summary>
    /// What a whole car has to satisfy, and the one validator the sample API exposes. Deliberately not a
    /// list of one-rule properties: it declares a plain rule, a rule of its own, a nested validator, a
    /// comparison against a sibling property, and a collection whose entries have a validator of their
    /// own — so a scenario test can exercise all of those at once, the way a real payload does.
    /// </summary>
    public sealed class CarValidator : Validator<Car>
    {
        public const int VinLength = 17;

        public const int MaximumServiceRecords = 20;

        public CarValidator(IValidator<CarModel> carModelValidator, IValidator<ServiceRecord> serviceRecordValidator)
        {
            // A null VIN is already reported by NotEmpty, and the chain stops there, so the predicate
            // only has to describe the shape of a VIN which is present.
            this.Property(c => c.Vin)
                .NotEmpty()
                .Must(vin => vin == null || vin.Trim().Length == VinLength, "The VIN must be exactly 17 characters long.");

            this.Property(c => c.Model)
                .NotNull()
                .SetValidator(carModelValidator);

            this.Property(c => c.Mileage)
                .GreaterThanOrEqualTo(0);

            this.Property(c => c.FirstRegistration)
                .WithDisplayName("Registration date")
                .NotDefault();

            this.Property(c => c.SoldDate)
                .GreaterThanOrEqualTo(c => c.FirstRegistration);

            this.Property(c => c.FeatureIds)
                .NoDuplicates();

            // The history is capped, checked against the car it belongs to, and then each entry is
            // judged by the service record's own validator. A car without a history has nothing to
            // answer for, which is why every rule here passes a missing collection.
            this.Property(c => c.ServiceHistory)
                .MaximumCount(MaximumServiceRecords)
                .Must(
                    (c, history) => history == null || history.All(record => record.Mileage <= c.Mileage),
                    "A service cannot be recorded at a higher mileage than the car has reached.")
                .ForEach(serviceRecordValidator);
        }
    }
}
