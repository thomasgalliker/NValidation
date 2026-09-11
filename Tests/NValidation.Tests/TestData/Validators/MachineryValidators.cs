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

    internal sealed class VinReportsEveryFailingRuleValidator : Validator<Car>
    {
        internal const string FirstMessage = "first";
        internal const string SecondMessage = "second";

        public VinReportsEveryFailingRuleValidator()
        {
            this.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.All)
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
                .WithValidationBehavior(ValidationBehavior.All)
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
                .WithValidationBehavior(ValidationBehavior.All)
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

    /// <summary>
    /// Two properties which each break two rules, so a run can be told apart both by how many messages
    /// it reports and by which properties they name. Both axes are taken through the constructor
    /// because what is under test is the setting rather than the rules.
    /// </summary>
    internal sealed class TwoFailingPropertiesValidator : Validator<Car>
    {
        /// <summary>
        /// Blank, and longer than the cap: one value which breaks both rules of a chain, which is what
        /// makes the within-a-chain axis observable at all.
        /// </summary>
        internal const string BreaksBothRules = "      ";

        internal const int MaximumLength = 3;

        public TwoFailingPropertiesValidator(ValidationBehavior? classBehavior, ValidationBehavior? propertyBehavior)
        {
            this.ValidationBehaviors.Class = classBehavior;
            this.ValidationBehaviors.Property = propertyBehavior;

            this.Property(c => c.Vin).NotEmpty().MaximumLength(MaximumLength);
            this.Property(c => c.RegistrationPlate).NotEmpty().MaximumLength(MaximumLength);
        }

        /// <summary>
        /// A car whose every property this validator judges is wrong in every way it can be.
        /// </summary>
        internal static Car BrokenCar()
        {
            return new Car { Vin = BreaksBothRules, RegistrationPlate = BreaksBothRules };
        }
    }

    /// <summary>
    /// Reports on its first property and records whether the second was ever looked at, so a run which
    /// stopped is told apart from one which judged everything and merely reported less.
    /// </summary>
    internal sealed class SecondPropertyProbeValidator : Validator<Car>
    {
        public SecondPropertyProbeValidator(ValidationBehavior classBehavior, Action onSecondProperty)
        {
            this.ValidationBehaviors.Class = classBehavior;

            this.Property(c => c.Vin).NotEmpty();

            this.Property(c => c.RegistrationPlate).Must(
                _ =>
                {
                    onSecondProperty();
                    return true;
                },
                "the probe never reports");
        }
    }

    /// <summary>
    /// A run which stops at the first error, over a chain which asked for all of its own rules — the
    /// one place "everything about the first field that is wrong" is reachable. The property after it
    /// records whether the run carried on regardless.
    /// </summary>
    internal sealed class ChainOverridingAStoppingRunValidator : Validator<Car>
    {
        public ChainOverridingAStoppingRunValidator(Action onSecondProperty)
        {
            this.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            this.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.All)
                .NotEmpty()
                .MaximumLength(TwoFailingPropertiesValidator.MaximumLength);

            this.Property(c => c.RegistrationPlate).Must(
                _ =>
                {
                    onSecondProperty();
                    return true;
                },
                "the probe never reports");
        }
    }

    /// <summary>
    /// Declares its behaviour after its rules, which must make no difference: the setting is resolved
    /// while validating, not while the rules are declared.
    /// </summary>
    internal sealed class BehaviorAfterTheRulesValidator : Validator<Car>
    {
        public BehaviorAfterTheRulesValidator()
        {
            this.Property(c => c.Vin).NotEmpty().MaximumLength(TwoFailingPropertiesValidator.MaximumLength);
            this.Property(c => c.RegistrationPlate).NotEmpty();

            this.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;
        }
    }

    /// <summary>
    /// A stopping run whose first property only applies to a car that has been sold. Where the
    /// condition does not hold that property reports nothing, so there is nothing to stop the run and
    /// the second property is reached.
    /// </summary>
    internal sealed class StoppingRunWithAConditionalPropertyValidator : Validator<Car>
    {
        public StoppingRunWithAConditionalPropertyValidator()
        {
            this.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            this.Property(c => c.Vin).NotEmpty().When(c => c.SoldDate != null);
            this.Property(c => c.RegistrationPlate).NotEmpty();
        }
    }

    /// <summary>
    /// Stops at its own first error, to be composed into a parent which does not.
    /// </summary>
    internal sealed class StoppingCarModelValidator : Validator<CarModel>
    {
        public StoppingCarModelValidator()
        {
            this.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            this.Property(m => m.Name).NotEmpty();
            this.Property(m => m.SeatCount).GreaterThan(0);
        }
    }

    /// <summary>
    /// Reports everything, and composes a child which stops at its own first error — so the test can
    /// prove the two decisions are independent in both directions.
    /// </summary>
    internal sealed class CarWithAStoppingModelValidator : Validator<Car>
    {
        public CarWithAStoppingModelValidator()
        {
            this.Property(c => c.Model).SetValidator(new StoppingCarModelValidator());
            this.Property(c => c.Vin).NotEmpty();
        }
    }

    /// <summary>
    /// Reports everything about the model it is given, to be composed into a parent which stops.
    /// </summary>
    internal sealed class ReportingCarModelValidator : Validator<CarModel>
    {
        public ReportingCarModelValidator()
        {
            this.Property(m => m.Name).NotEmpty();
            this.Property(m => m.SeatCount).GreaterThan(0);
        }
    }

    /// <summary>
    /// A stopping run whose very first rule is a whole validator of its own. One rule can report more
    /// than one message, and a run is stopped between rules — so what the child found is passed on
    /// whole and the stop takes effect only afterwards.
    /// </summary>
    internal sealed class StoppingRunComposingAReportingValidator : Validator<Car>
    {
        public StoppingRunComposingAReportingValidator()
        {
            this.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            this.Property(c => c.Model).SetValidator(new ReportingCarModelValidator());
            this.Property(c => c.Vin).NotEmpty();
        }
    }

    /// <summary>
    /// Reports every rule of every property, except for the one chain which asked to stop — the
    /// mirror of <see cref="ChainOverridingAStoppingRunValidator"/>, so the override is proven to work
    /// in both directions rather than only as the old boolean did.
    /// </summary>
    internal sealed class ChainStoppingWithinAReportingValidator : Validator<Car>
    {
        public ChainStoppingWithinAReportingValidator()
        {
            this.ValidationBehaviors.Property = ValidationBehavior.All;

            this.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.StopAtFirstError)
                .NotEmpty()
                .MaximumLength(TwoFailingPropertiesValidator.MaximumLength);

            this.Property(c => c.RegistrationPlate)
                .NotEmpty()
                .MaximumLength(TwoFailingPropertiesValidator.MaximumLength);
        }
    }
}
