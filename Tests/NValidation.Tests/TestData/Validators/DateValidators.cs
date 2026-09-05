namespace NValidation.TestData.Validators
{
    internal sealed class FirstRegistrationInThePastValidator : Validator<Car>
    {
        public FirstRegistrationInThePastValidator(TimeProvider timeProvider)
        {
            this.Property(c => c.FirstRegistration).InThePast(timeProvider);
        }
    }

    internal sealed class SoldDateInThePastValidator : Validator<Car>
    {
        public SoldDateInThePastValidator(TimeProvider timeProvider)
        {
            this.Property(c => c.SoldDate).InThePast(timeProvider);
        }
    }

    internal sealed class RegisteredAtInThePastValidator : Validator<Car>
    {
        public RegisteredAtInThePastValidator(TimeProvider timeProvider)
        {
            this.Property(c => c.RegisteredAt).InThePast(timeProvider);
        }
    }

    internal sealed class NextServiceAtInTheFutureValidator : Validator<Car>
    {
        public NextServiceAtInTheFutureValidator(TimeProvider timeProvider)
        {
            this.Property(c => c.NextServiceAt).InTheFuture(timeProvider);
        }
    }

    internal sealed class WarrantyEndsOnInTheFutureValidator : Validator<Car>
    {
        public WarrantyEndsOnInTheFutureValidator(TimeProvider timeProvider)
        {
            this.Property(c => c.WarrantyEndsOn).InTheFuture(timeProvider);
        }
    }
}
