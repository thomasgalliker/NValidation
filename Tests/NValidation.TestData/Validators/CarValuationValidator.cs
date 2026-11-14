namespace NValidation.TestData.Validators
{
    /// <summary>
    /// Registered like every other validator, but run by the action rather than by a filter, for an
    /// endpoint which reports failures in its own shape.
    /// </summary>
    public sealed class CarValuationValidator : Validator<CarValuation>
    {
        public CarValuationValidator()
        {
            this.Property(v => v.Vin)
                .NotEmpty()
                .Length(CarValidator.VinLength);

            this.Property(v => v.Mileage)
                .GreaterThanOrEqualTo(0);
        }
    }
}
