namespace NValidation.TestData.Validators
{
    public sealed class CarModelValidator : Validator<CarModel>
    {
        internal const int NameMaximumLength = 100;
        internal const int MinimumSeatCount = 1;
        internal const int MaximumSeatCount = 9;

        public CarModelValidator(IValidator<Manufacturer> manufacturerValidator)
        {
            this.Property(m => m.Name)
                .NotEmpty()
                .MaximumLength(NameMaximumLength);

            this.Property(m => m.Manufacturer)
                .NotNull()
                .SetValidator(manufacturerValidator);

            this.Property(m => m.EngineType)
                .IsInEnum();

            this.Property(m => m.SeatCount)
                .Between(MinimumSeatCount, MaximumSeatCount);

            this.Property(m => m.BasePrice)
                .NotNull()
                .GreaterThan(0m);

            this.Property(m => m.FuelConsumption)
                .NotNaN();

            // Only an electric car has a battery, so the whole chain is skipped for every other engine.
            this.Property(m => m.BatteryCapacityKwh)
                .NotNull()
                .GreaterThan(0m)
                .When(m => m.EngineType == EngineType.Electric);
        }
    }
}
