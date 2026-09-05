namespace NValidation.TestData.Validators
{
    internal sealed class VinNotEmptyValidator : Validator<Car>
    {
        public VinNotEmptyValidator()
        {
            this.Property(c => c.Vin).NotEmpty();
        }
    }

    internal sealed class FirstRegistrationNotDefaultValidator : Validator<Car>
    {
        public FirstRegistrationNotDefaultValidator()
        {
            this.Property(c => c.FirstRegistration).NotDefault();
        }
    }

    internal sealed class SoldDateNotDefaultValidator : Validator<Car>
    {
        public SoldDateNotDefaultValidator()
        {
            this.Property(c => c.SoldDate).NotDefault();
        }
    }

    internal sealed class ConditionNotDefaultValidator : Validator<Car>
    {
        public ConditionNotDefaultValidator()
        {
            this.Property(c => c.Condition).NotDefault();
        }
    }

    internal sealed class ModelNotNullValidator : Validator<Car>
    {
        public ModelNotNullValidator()
        {
            this.Property(c => c.Model).NotNull();
        }
    }

    internal sealed class TradeInValueNotNullValidator : Validator<Car>
    {
        public TradeInValueNotNullValidator()
        {
            this.Property(c => c.TradeInValue).NotNull();
        }
    }

    internal sealed class FeatureIdsNotEmptyValidator : Validator<Car>
    {
        public FeatureIdsNotEmptyValidator()
        {
            this.Property(c => c.FeatureIds).NotEmpty();
        }
    }

    /// <summary>
    /// The property is declared as a concrete <see cref="List{T}"/> rather than an interface, which is
    /// what proves the collection rules bind whatever the declared type is.
    /// </summary>
    internal sealed class PreviousOwnerIdsNotEmptyValidator : Validator<Car>
    {
        public PreviousOwnerIdsNotEmptyValidator()
        {
            this.Property(c => c.PreviousOwnerIds).NotEmpty();
        }
    }
}
