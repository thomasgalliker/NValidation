namespace NValidation.TestData.Validators
{
    internal sealed class VinMustBeSeventeenCharactersValidator : Validator<Car>
    {
        internal const string Message = "The VIN must be exactly 17 characters long.";

        public VinMustBeSeventeenCharactersValidator()
        {
            this.Property(c => c.Vin).Must(vin => vin != null && vin.Length == 17, Message);
        }
    }

    /// <summary>
    /// The deferred-message form, for a message which depends on the culture of the current thread and
    /// so has to be read while the rule runs rather than while it is declared.
    /// </summary>
    internal sealed class VinMustDeferredMessageValidator : Validator<Car>
    {
        public VinMustDeferredMessageValidator(Func<string> message)
        {
            this.Property(c => c.Vin).Must(_ => false, message);
        }
    }

    /// <summary>
    /// The form which sees the whole object: a car offered for sale has to carry a price.
    /// </summary>
    internal sealed class ListedCarNeedsAPriceValidator : Validator<Car>
    {
        internal const string Message = "A car listed for sale must have a price.";

        public ListedCarNeedsAPriceValidator()
        {
            this.Property(c => c.PurchasePrice).Must((car, price) => !car.IsListedForSale || price != 0m, Message);
        }
    }

    /// <inheritdoc cref="VinMustDeferredMessageValidator"/>
    internal sealed class PurchasePriceMustDeferredMessageValidator : Validator<Car>
    {
        public PurchasePriceMustDeferredMessageValidator(Func<string> message)
        {
            this.Property(c => c.PurchasePrice).Must((_, _) => false, message);
        }
    }

    /// <summary>
    /// Takes the rule's own arguments, so a test can hand it a null one and watch the guard fire while
    /// the rule is being declared rather than while it runs.
    /// </summary>
    internal sealed class VinMustValidator : Validator<Car>
    {
        public VinMustValidator(Func<string?, bool> predicate, string message)
        {
            this.Property(c => c.Vin).Must(predicate, message);
        }
    }

    /// <summary>
    /// Requires the model and validates it with its own validator, so a nested failure is reported
    /// under the path it sits at.
    /// </summary>
    internal sealed class ModelSetValidatorValidator : Validator<Car>
    {
        public ModelSetValidatorValidator(IValidator<CarModel> carModelValidator)
        {
            this.Property(c => c.Model).SetValidator(carModelValidator);
        }
    }

    /// <summary>
    /// A rule which really does suspend — the case a synchronous run cannot serve and must refuse
    /// rather than block on.
    /// </summary>
    internal sealed class VinSuspendingValidator : Validator<Car>
    {
        public VinSuspendingValidator()
        {
            this.Property(c => c.Vin).AddAsync(async (context, cancellationToken) =>
            {
                await Task.Delay(1, cancellationToken);

                context.AddError(new ValidationError(context.Code, "checked elsewhere"));
            });
        }
    }

    /// <summary>
    /// A model validator with a single rule, so a nested failure is unambiguous.
    /// </summary>
    internal sealed class CarModelNameValidator : Validator<CarModel>
    {
        public CarModelNameValidator()
        {
            this.Property(m => m.Name).NotEmpty();
        }
    }
}
