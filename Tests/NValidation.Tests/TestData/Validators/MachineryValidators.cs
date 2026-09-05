namespace NValidation.TestData.Validators
{
    /// <summary>
    /// Validators whose subject is the rule-chain machinery rather than any one rule: how a code is
    /// derived, when a chain stops, and how a message or a condition is applied.
    /// </summary>
    internal sealed class ModelNameNotEmptyValidator : Validator<Car>
    {
        public ModelNameNotEmptyValidator()
        {
            this.Property(c => c.Model!.Name).NotEmpty();
        }
    }

    internal sealed class VinNotEmptyAndBoundedValidator : Validator<Car>
    {
        public VinNotEmptyAndBoundedValidator()
        {
            this.Property(c => c.Vin).NotEmpty().MaximumLength(3);
        }
    }

    internal sealed class VinContinueOnFailureValidator : Validator<Car>
    {
        internal const string FirstMessage = "first";
        internal const string SecondMessage = "second";

        public VinContinueOnFailureValidator()
        {
            this.Property(c => c.Vin)
                .ContinueOnFailure()
                .Must(vin => vin != "wrong", FirstMessage)
                .Must(vin => vin != "wrong", SecondMessage);
        }
    }

    internal sealed class VinWithMessageValidator : Validator<Car>
    {
        public VinWithMessageValidator(string message)
        {
            this.Property(c => c.Vin).NotEmpty().WithMessage(message);
        }
    }

    internal sealed class VinWithDeferredMessageValidator : Validator<Car>
    {
        public VinWithDeferredMessageValidator(Func<string> message)
        {
            this.Property(c => c.Vin).NotEmpty().WithMessage(message);
        }
    }

    /// <summary>
    /// The message belongs to the rule it follows, so the second rule of the chain keeps the shared
    /// wording.
    /// </summary>
    internal sealed class VinWithMessageOnTheFirstRuleValidator : Validator<Car>
    {
        public VinWithMessageOnTheFirstRuleValidator()
        {
            this.Property(c => c.Vin)
                .ContinueOnFailure()
                .NotEmpty().WithMessage("custom")
                .MaximumLength(3);
        }
    }

    /// <summary>
    /// A rule which reports a code of its own keeps it; only the wording is replaced.
    /// </summary>
    internal sealed class FeatureIdsCustomCodeWithMessageValidator : Validator<Car>
    {
        internal const string Code = "FeatureIds[0]";

        public FeatureIdsCustomCodeWithMessageValidator(string message)
        {
            this.Property(c => c.FeatureIds)
                .Add(context => context.AddError(new ValidationError(Code, "the original message")))
                .WithMessage(message);
        }
    }

    /// <summary>
    /// Declares a message with no rule in front of it, which is a mistake in the validator rather than
    /// in the data.
    /// </summary>
    internal sealed class VinWithMessageAndNoRuleValidator : Validator<Car>
    {
        public VinWithMessageAndNoRuleValidator()
        {
            this.Property(c => c.Vin).WithMessage("nothing to apply this to");
        }
    }

    internal sealed class VinDisplayNameValidator : Validator<Car>
    {
        public VinDisplayNameValidator(string displayName)
        {
            this.Property(c => c.Vin).WithDisplayName(displayName).NotEmpty();
        }
    }

    internal sealed class VinDeferredDisplayNameValidator : Validator<Car>
    {
        public VinDeferredDisplayNameValidator(Func<string> displayName)
        {
            this.Property(c => c.Vin).WithDisplayName(displayName).NotEmpty();
        }
    }

    /// <summary>
    /// Only a car that has actually been sold has to carry a VIN.
    /// </summary>
    internal sealed class VinRequiredWhenSoldValidator : Validator<Car>
    {
        public VinRequiredWhenSoldValidator()
        {
            this.Property(c => c.Vin).NotEmpty().When(c => c.SoldDate != null);
        }
    }

    /// <inheritdoc cref="VinRequiredWhenSoldValidator"/>
    internal sealed class VinRequiredUnlessUnsoldValidator : Validator<Car>
    {
        public VinRequiredUnlessUnsoldValidator()
        {
            this.Property(c => c.Vin).NotEmpty().Unless(c => c.SoldDate == null);
        }
    }

    /// <summary>
    /// The condition covers the whole chain, not just the rule it happens to follow.
    /// </summary>
    internal sealed class VinChainRequiredWhenSoldValidator : Validator<Car>
    {
        public VinChainRequiredWhenSoldValidator()
        {
            this.Property(c => c.Vin)
                .ContinueOnFailure()
                .NotEmpty()
                .MaximumLength(3)
                .When(c => c.SoldDate != null);
        }
    }

    internal sealed class VinRequiredWhenSoldAndModelledValidator : Validator<Car>
    {
        public VinRequiredWhenSoldAndModelledValidator()
        {
            this.Property(c => c.Vin)
                .NotEmpty()
                .When(c => c.SoldDate != null)
                .When(c => c.Model != null);
        }
    }

    /// <summary>
    /// A chain two objects deep, so a guard has more than one thing to find missing.
    /// </summary>
    internal sealed class ManufacturerNameNotEmptyValidator : Validator<Car>
    {
        public ManufacturerNameNotEmptyValidator()
        {
            this.Property(c => c.Model!.Manufacturer!.Name).NotEmpty();
        }
    }

    /// <summary>
    /// A chain on a nested path, guarded by the condition that makes the path reachable at all.
    /// </summary>
    internal sealed class ModelNameRequiredWhenModelPresentValidator : Validator<Car>
    {
        public ModelNameRequiredWhenModelPresentValidator()
        {
            this.Property(c => c.Model!.Name).NotEmpty().When(c => c.Model != null);
        }
    }

    /// <summary>
    /// Reaches the same property as <see cref="MileageGreaterThanValidator"/> but through a conversion,
    /// so the compiled accessor has a different delegate type for the same property path.
    /// </summary>
    internal sealed class MileageAsObjectValidator : Validator<Car>
    {
        public MileageAsObjectValidator()
        {
            this.Property(c => (object)c.Mileage).NotNull();
        }
    }

    /// <summary>
    /// Declares a rule for something that is not a property, so there is no code to report under.
    /// </summary>
    internal sealed class NotAPropertyValidator : Validator<Car>
    {
        public NotAPropertyValidator()
        {
            this.Property(c => c.Vin!.Length + 1).GreaterThan(0);
        }
    }

    internal sealed class VinErrorCodeValidator : Validator<Car>
    {
        public VinErrorCodeValidator(string errorCode)
        {
            this.Property(c => c.Vin).WithErrorCode(errorCode).NotEmpty();
        }
    }

    /// <summary>
    /// The override is declared on a nested path, which is where it earns its keep: the client's field
    /// is not shaped like the model's.
    /// </summary>
    internal sealed class NestedNameErrorCodeValidator : Validator<Car>
    {
        public NestedNameErrorCodeValidator()
        {
            this.Property(c => c.Model!.Name).WithErrorCode("manufacturerName").NotEmpty();
        }
    }

    /// <summary>
    /// Carries both overrides, so the test can prove they are independent: one changes what the failure
    /// is reported under, the other only what the message calls it.
    /// </summary>
    internal sealed class VinErrorCodeAndDisplayNameValidator : Validator<Car>
    {
        public VinErrorCodeAndDisplayNameValidator()
        {
            this.Property(c => c.Vin)
                .WithErrorCode("vehicleId")
                .WithDisplayName("Vehicle identification number")
                .NotEmpty();
        }
    }

    /// <summary>
    /// A rule which reports under a code of its own, to prove the override does not overwrite it.
    /// </summary>
    internal sealed class FeatureIdsErrorCodeWithCustomCodeValidator : Validator<Car>
    {
        public FeatureIdsErrorCodeWithCustomCodeValidator()
        {
            this.Property(c => c.FeatureIds)
                .WithErrorCode("features")
                .Add(context => context.AddError(new ValidationError("features[0]", "the first entry is wrong")));
        }
    }
}
