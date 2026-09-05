namespace NValidation.TestData.Validators
{
    internal sealed class PurchasePriceMultipleOfValidator : Validator<Car>
    {
        public PurchasePriceMultipleOfValidator(decimal step)
        {
            this.Property(c => c.PurchasePrice).MultipleOf(step);
        }
    }

    internal sealed class TradeInValueMultipleOfValidator : Validator<Car>
    {
        public TradeInValueMultipleOfValidator(decimal step)
        {
            this.Property(c => c.TradeInValue).MultipleOf(step);
        }
    }

    internal sealed class MileageMultipleOfValidator : Validator<Car>
    {
        public MileageMultipleOfValidator(int step)
        {
            this.Property(c => c.Mileage).MultipleOf(step);
        }
    }

    internal sealed class FuelConsumptionNotNaNValidator : Validator<CarModel>
    {
        public FuelConsumptionNotNaNValidator()
        {
            this.Property(m => m.FuelConsumption).NotNaN();
        }
    }

    internal sealed class TopSpeedNotNaNValidator : Validator<CarModel>
    {
        public TopSpeedNotNaNValidator()
        {
            this.Property(m => m.TopSpeed).NotNaN();
        }
    }
}
