namespace NValidation.TestData.Validators
{
    /// <summary>
    /// The far side of the comparison is reached through <see cref="Car.Model"/>, which a payload may
    /// omit. The validated property is on the root, so the chain's own guard cannot be what skips it.
    /// </summary>
    internal sealed class MileageWithinModelWarrantyValidator : Validator<Car>
    {
        public MileageWithinModelWarrantyValidator()
        {
            this.Property(c => c.Mileage).LessThanOrEqualTo(c => c.Model!.WarrantyMileageCap);
        }
    }

    internal sealed class RegistrationPlateNotEqualToPreviousValidator : Validator<Car>
    {
        public RegistrationPlateNotEqualToPreviousValidator()
        {
            this.Property(c => c.RegistrationPlate).NotEqualTo(c => c.PreviousRegistrationPlate);
        }
    }

    internal sealed class MileageNotEqualToWarrantyLimitValidator : Validator<Car>
    {
        public MileageNotEqualToWarrantyLimitValidator()
        {
            this.Property(c => c.Mileage).NotEqualTo(c => c.WarrantyMileageLimit);
        }
    }

    internal sealed class SoldDateEqualToWarrantyEndValidator : Validator<Car>
    {
        public SoldDateEqualToWarrantyEndValidator()
        {
            this.Property(c => c.SoldDate).EqualTo(c => c.WarrantyEndsOn);
        }
    }

    internal sealed class RegistrationPlateEqualToPreviousValidator : Validator<Car>
    {
        public RegistrationPlateEqualToPreviousValidator()
        {
            this.Property(c => c.RegistrationPlate).EqualTo(c => c.PreviousRegistrationPlate);
        }
    }

    internal sealed class ServiceIntervalMultipleOfValidator : Validator<Car>
    {
        public ServiceIntervalMultipleOfValidator(int step)
        {
            this.Property(c => c.ServiceIntervalKm).MultipleOf(step);
        }
    }

    internal sealed class IntakeConditionIsInEnumValidator : Validator<Car>
    {
        public IntakeConditionIsInEnumValidator()
        {
            this.Property(c => c.IntakeCondition).IsInEnum();
        }
    }

    /// <summary>
    /// A collection of a reference type, declared with the element rules the review found warned at
    /// their call site.
    /// </summary>
    internal sealed class ServiceInvoiceNumbersElementValidator : Validator<Car>
    {
        public ServiceInvoiceNumbersElementValidator()
        {
            this.Property(c => c.ServiceInvoiceNumbers).ForEach(number => number.Element().MaximumLength(6));
        }
    }

    internal sealed class ContactEmailTopLevelDomainInValidator : Validator<Manufacturer>
    {
        public ContactEmailTopLevelDomainInValidator(params string[] topLevelDomains)
        {
            this.Property(m => m.ContactEmail).EmailTopLevelDomainIn(topLevelDomains);
        }
    }

    internal sealed class ContactEmailTopLevelDomainNotInValidator : Validator<Manufacturer>
    {
        public ContactEmailTopLevelDomainNotInValidator(params string[] topLevelDomains)
        {
            this.Property(m => m.ContactEmail).EmailTopLevelDomainNotIn(topLevelDomains);
        }
    }

    internal sealed class NameNotContainingValidator : Validator<Manufacturer>
    {
        public NameNotContainingValidator(params string[] values)
        {
            this.Property(m => m.Name).NotContaining(values);
        }
    }

    internal sealed class NameNotContainingOrdinalValidator : Validator<Manufacturer>
    {
        public NameNotContainingOrdinalValidator(params string[] values)
        {
            this.Property(m => m.Name).NotContaining(StringComparison.Ordinal, values);
        }
    }

    internal sealed class PlateEqualToTextValidator : Validator<Car>
    {
        public PlateEqualToTextValidator(string? value)
        {
            this.Property(c => c.RegistrationPlate).EqualTo(value);
        }
    }

    internal sealed class PlateNotEqualToTextValidator : Validator<Car>
    {
        public PlateNotEqualToTextValidator(string? value)
        {
            this.Property(c => c.RegistrationPlate).NotEqualTo(value);
        }
    }

    internal sealed class ServiceMileagesElementDisplayNameValidator : Validator<Car>
    {
        public ServiceMileagesElementDisplayNameValidator()
        {
            this.Property(c => c.ServiceMileages)
                .ForEach(mileage => mileage.Element().WithDisplayName("Service mileage").GreaterThanOrEqualTo(0));
        }
    }

    internal sealed class ServiceMileagesIndexedElementValidator : Validator<Car>
    {
        public ServiceMileagesIndexedElementValidator()
        {
            this.Property(c => c.ServiceMileages)
                .ForEach(mileage => mileage.WithIndexer((_, position) => $"entry-{position}").Element().GreaterThanOrEqualTo(0));
        }
    }

    /// <summary>
    /// The path walks out of the lambda's parameter through an indexer, so <c>PropertyPath</c> can only
    /// see the trailing member. Two of these would share the path "Length", and with it the compiled
    /// accessor cached under it.
    /// </summary>
    internal sealed class IndexedPathValidator : Validator<Car>
    {
        public IndexedPathValidator()
        {
            this.Property(c => c.ServiceInvoiceNumbers![0].Length).GreaterThan(2);
        }
    }

    /// <inheritdoc cref="IndexedPathValidator"/>
    internal sealed class MethodCallPathValidator : Validator<Car>
    {
        public MethodCallPathValidator()
        {
            this.Property(c => c.Vin!.Trim().Length).GreaterThan(2);
        }
    }

    /// <summary>
    /// Reaches a property of something that is not the validated object at all.
    /// </summary>
    internal sealed class CapturedPathValidator : Validator<Car>
    {
        public CapturedPathValidator(Manufacturer other)
        {
            this.Property(_ => other.Name).NotEmpty();
        }
    }

    /// <inheritdoc cref="CapturedPathValidator"/>
    internal sealed class StaticPathValidator : Validator<Car>
    {
        public StaticPathValidator()
        {
            this.Property(_ => DateTime.Now.Year).GreaterThan(0);
        }
    }

    internal sealed class BlankErrorCodeValidator : Validator<Car>
    {
        public BlankErrorCodeValidator(string errorCode)
        {
            this.Property(c => c.Vin).WithErrorCode(errorCode).NotEmpty();
        }
    }

    internal sealed class BlankDisplayNameValidator : Validator<Car>
    {
        public BlankDisplayNameValidator(string displayName)
        {
            this.Property(c => c.Vin).WithDisplayName(displayName).NotEmpty();
        }
    }

    /// <summary>
    /// The chain the README recommends, over a property that may only be walked once.
    /// </summary>
    internal sealed class ServiceMileagesCollectionChainValidator : Validator<Car>
    {
        public ServiceMileagesCollectionChainValidator()
        {
            this.Property(c => c.ServiceMileages)
                .ContinueOnFailure()
                .NotEmpty()
                .MinimumCount(1)
                .MaximumCount(2);
        }
    }

}
