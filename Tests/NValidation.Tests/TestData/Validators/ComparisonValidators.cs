namespace NValidation.TestData.Validators
{
    // --- against a value, non-nullable receiver -------------------------------------------------

    internal sealed class MileageGreaterThanValidator : Validator<Car>
    {
        public MileageGreaterThanValidator(int value)
        {
            this.Property(c => c.Mileage).GreaterThan(value);
        }
    }

    internal sealed class MileageGreaterThanOrEqualToValidator : Validator<Car>
    {
        public MileageGreaterThanOrEqualToValidator(int value)
        {
            this.Property(c => c.Mileage).GreaterThanOrEqualTo(value);
        }
    }

    internal sealed class MileageLessThanValidator : Validator<Car>
    {
        public MileageLessThanValidator(int value)
        {
            this.Property(c => c.Mileage).LessThan(value);
        }
    }

    internal sealed class MileageLessThanOrEqualToValidator : Validator<Car>
    {
        public MileageLessThanOrEqualToValidator(int value)
        {
            this.Property(c => c.Mileage).LessThanOrEqualTo(value);
        }
    }

    internal sealed class PurchasePriceGreaterThanValidator : Validator<Car>
    {
        public PurchasePriceGreaterThanValidator(decimal value)
        {
            this.Property(c => c.PurchasePrice).GreaterThan(value);
        }
    }

    /// <summary>
    /// A <see cref="long"/> subject: the comparison rules are written once over
    /// <see cref="IComparable{T}"/>, not once per numeric type, and this is what proves it.
    /// </summary>
    internal sealed class UnitsProducedGreaterThanValidator : Validator<CarModel>
    {
        public UnitsProducedGreaterThanValidator(long value)
        {
            this.Property(m => m.UnitsProduced).GreaterThan(value);
        }
    }

    /// <summary>
    /// A <see cref="TimeSpan"/> subject — neither a number nor a date.
    /// </summary>
    internal sealed class ServiceIntervalLessThanOrEqualToValidator : Validator<Car>
    {
        public ServiceIntervalLessThanOrEqualToValidator(TimeSpan value)
        {
            this.Property(c => c.ServiceInterval).LessThanOrEqualTo(value);
        }
    }

    /// <summary>
    /// A <see cref="DateTimeOffset"/> subject.
    /// </summary>
    internal sealed class RegisteredAtGreaterThanOrEqualToValidator : Validator<Car>
    {
        public RegisteredAtGreaterThanOrEqualToValidator(DateTimeOffset value)
        {
            this.Property(c => c.RegisteredAt).GreaterThanOrEqualTo(value);
        }
    }

    // --- against a value, nullable receiver -----------------------------------------------------

    internal sealed class TradeInValueGreaterThanValidator : Validator<Car>
    {
        public TradeInValueGreaterThanValidator(decimal value)
        {
            this.Property(c => c.TradeInValue).GreaterThan(value);
        }
    }

    internal sealed class TopSpeedLessThanValidator : Validator<CarModel>
    {
        public TopSpeedLessThanValidator(float value)
        {
            this.Property(m => m.TopSpeed).LessThan(value);
        }
    }

    // --- against another property ---------------------------------------------------------------

    /// <summary>
    /// Nullable receiver, non-nullable other property.
    /// </summary>
    internal sealed class SoldDateAfterFirstRegistrationValidator : Validator<Car>
    {
        public SoldDateAfterFirstRegistrationValidator()
        {
            this.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);
        }
    }

    /// <summary>
    /// The same pair the other way round: non-nullable receiver, nullable other property.
    /// </summary>
    internal sealed class FirstRegistrationBeforeSoldDateValidator : Validator<Car>
    {
        public FirstRegistrationBeforeSoldDateValidator()
        {
            this.Property(c => c.FirstRegistration).LessThanOrEqualTo(c => c.SoldDate);
        }
    }

    /// <summary>
    /// Both sides nullable.
    /// </summary>
    internal sealed class WarrantyEndsOnAfterSoldDateValidator : Validator<Car>
    {
        public WarrantyEndsOnAfterSoldDateValidator()
        {
            this.Property(c => c.WarrantyEndsOn).GreaterThan(c => c.SoldDate);
        }
    }

    /// <summary>
    /// Neither side nullable.
    /// </summary>
    internal sealed class MileageWithinWarrantyValidator : Validator<Car>
    {
        public MileageWithinWarrantyValidator()
        {
            this.Property(c => c.Mileage).LessThanOrEqualTo(c => c.WarrantyMileageLimit);
        }
    }

    /// <summary>
    /// The compared property declares a display name, which the message must use instead of its code.
    /// </summary>
    internal sealed class SoldDateAfterNamedFirstRegistrationValidator : Validator<Car>
    {
        public SoldDateAfterNamedFirstRegistrationValidator()
        {
            this.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);
            this.Property(c => c.FirstRegistration).WithDisplayName("the registration date");
        }
    }

    // --- ranges ----------------------------------------------------------------------------------

    internal sealed class SeatCountBetweenValidator : Validator<CarModel>
    {
        public SeatCountBetweenValidator(int from, int to)
        {
            this.Property(m => m.SeatCount).Between(from, to);
        }
    }

    internal sealed class SeatCountBetweenExclusiveValidator : Validator<CarModel>
    {
        public SeatCountBetweenExclusiveValidator(int from, int to, bool inclusive)
        {
            this.Property(m => m.SeatCount).Between(from, to, inclusive);
        }
    }

    internal sealed class SeatCountBetweenBoundsValidator : Validator<CarModel>
    {
        public SeatCountBetweenBoundsValidator(int from, int to, bool inclusiveFrom, bool inclusiveTo)
        {
            this.Property(m => m.SeatCount).Between(from, to, inclusiveFrom, inclusiveTo);
        }
    }

    internal sealed class TradeInValueBetweenValidator : Validator<Car>
    {
        public TradeInValueBetweenValidator(decimal from, decimal to)
        {
            this.Property(c => c.TradeInValue).Between(from, to);
        }
    }

    // --- equality ---------------------------------------------------------------------------------

    internal sealed class MileageEqualToValidator : Validator<Car>
    {
        public MileageEqualToValidator(int value)
        {
            this.Property(c => c.Mileage).EqualTo(value);
        }
    }

    internal sealed class MileageNotEqualToValidator : Validator<Car>
    {
        public MileageNotEqualToValidator(int value)
        {
            this.Property(c => c.Mileage).NotEqualTo(value);
        }
    }

    internal sealed class TradeInValueEqualToValidator : Validator<Car>
    {
        public TradeInValueEqualToValidator(decimal value)
        {
            this.Property(c => c.TradeInValue).EqualTo(value);
        }
    }

    internal sealed class VinEqualToValidator : Validator<Car>
    {
        public VinEqualToValidator(string? value, StringComparison comparison)
        {
            this.Property(c => c.Vin).EqualTo(value, comparison);
        }
    }

    internal sealed class MileageEqualToWarrantyLimitValidator : Validator<Car>
    {
        public MileageEqualToWarrantyLimitValidator()
        {
            this.Property(c => c.Mileage).EqualTo(c => c.WarrantyMileageLimit);
        }
    }
}
