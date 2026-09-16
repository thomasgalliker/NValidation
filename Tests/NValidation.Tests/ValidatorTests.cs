namespace NValidation.Tests
{
    /// <summary>
    /// Covers the validator base class itself — how a property's property name is derived, how a rule chain
    /// runs, and how a nested validator is merged — independently of any concrete rule.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidatorTests
    {
        /// <summary>
        /// The message of a probe rule which always passes, so it never reaches a result: what the probe
        /// records is whether it was run at all.
        /// </summary>
        private const string ProbeNeverReports = "the probe never reports";

        [Fact]
        public async Task ValidateAsync_WithValidInstance_Succeeds()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_TakesTheErrorCode_FromThePropertyExpression()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// A nested path becomes a dotted propertyName, which is the convention callers bind to.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_TakesTheErrorCode_FromANestedPropertyExpression()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Name).NotEmpty();

            var car = new Car { Model = new CarModel() };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Model.Name", "Model.Name is required.");
        }

        /// <summary>
        /// A property reports at most one message: once a rule fails, the rest of its chain is skipped.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_StopsTheChain_AtTheFirstFailingRule()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().MaximumLength(3);

            var car = new Car { Vin = "   " };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task ValidateAsync_ReportsEveryFailingRule_WhenTheChainAsksForAll()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.All)
                .Must(vin => vin != "wrong", "first")
                .Must(vin => vin != "wrong", "second");

            var car = new Car { Vin = "wrong" };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([new("Vin", "first"), new("Vin", "second")]);
        }

        /// <summary>
        /// The whole feature in one table: what each combination of the two axes reports, over two
        /// properties which each break two rules. The rows are the behaviours a caller can ask for, so
        /// a change to either axis that is not intended turns this red.
        /// </summary>
        [Theory]
        [InlineData(null, null, "Vin", "RegistrationPlate")] // the defaults: every property, one message each
        [InlineData(ValidationBehavior.All, ValidationBehavior.StopAtFirstError, "Vin", "RegistrationPlate")]
        [InlineData(ValidationBehavior.All, ValidationBehavior.All, "Vin", "Vin", "RegistrationPlate", "RegistrationPlate")]
        [InlineData(ValidationBehavior.StopAtFirstError, ValidationBehavior.StopAtFirstError, "Vin")]
        public async Task ValidateAsync_ReportsWhatTheValidationBehaviorsAskFor(
            ValidationBehavior? classBehavior,
            ValidationBehavior? propertyBehavior,
            params string[] expectedCodes)
        {
            // Arrange
            var validator = new TwoFailingPropertiesValidator(classBehavior, propertyBehavior);

            // Act
            var result = await validator.ValidateAsync(TwoFailingPropertiesValidator.BrokenCar());

            // Assert
            result.ShouldReport(expectedCodes.Select(ExpectedError.Any));
        }

        /// <summary>
        /// A run told to stop at the first error reports exactly one, whatever the other axis says.
        /// Anything else would make a setting by that name a trap.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_StoppingAtTheFirstError_OverridesTheChainAxis()
        {
            // Arrange
            var validator = new TwoFailingPropertiesValidator(
                ValidationBehavior.StopAtFirstError, ValidationBehavior.All);

            // Act
            var result = await validator.ValidateAsync(TwoFailingPropertiesValidator.BrokenCar());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// The properties after a stop are not merely left out of the result — they are never looked
        /// at, which is the point of asking a run to stop.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_StoppingAtTheFirstError_NeverReachesTheNextProperty()
        {
            // Arrange
            var reached = false;

            var validator = new TestValidator<Car>();
            validator.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            validator.Property(c => c.Vin).NotEmpty();

            validator.Property(c => c.RegistrationPlate).Must(
                _ =>
                {
                    reached = true;
                    return true;
                },
                ProbeNeverReports);

            // Act
            await validator.ValidateAsync(new Car());

            // Assert
            reached.Should().BeFalse("a run that stopped does not go on to judge the rest");
        }

        /// <inheritdoc cref="ValidateAsync_StoppingAtTheFirstError_NeverReachesTheNextProperty" path="/summary"/>
        [Fact]
        public async Task ValidateAsync_ReportingEverything_ReachesTheNextProperty()
        {
            // Arrange
            var reached = false;

            var validator = new TestValidator<Car>();
            validator.ValidationBehaviors.Class = ValidationBehavior.All;

            validator.Property(c => c.Vin).NotEmpty();

            validator.Property(c => c.RegistrationPlate).Must(
                _ =>
                {
                    reached = true;
                    return true;
                },
                ProbeNeverReports);

            // Act
            await validator.ValidateAsync(new Car());

            // Assert
            reached.Should().BeTrue();
        }

        /// <summary>
        /// The one exception to a stopping run: a chain which asked for all of its own rules is let
        /// finish, so a caller can be told everything about the first field that is wrong. The run
        /// still stops afterwards.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_StoppingAtTheFirstError_LetsAChainThatAskedForAllFinish()
        {
            // Arrange
            var reached = false;

            var validator = new TestValidator<Car>();
            validator.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            validator.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.All)
                .NotEmpty()
                .MaximumLength(TwoFailingPropertiesValidator.MaximumLength);

            validator.Property(c => c.RegistrationPlate).Must(
                _ =>
                {
                    reached = true;
                    return true;
                },
                ProbeNeverReports);

            // Act
            var result = await validator.ValidateAsync(TwoFailingPropertiesValidator.BrokenCar());

            // Assert
            result.ShouldReport([
                new("Vin", "Vin is required."),
                new("Vin", "Vin must not exceed 3 characters.")]);

            reached.Should().BeFalse("the run still stops once that chain has had its say");
        }

        /// <summary>
        /// The override works in both directions. Its stopping direction is the half the old boolean
        /// could not express, and it is the only way to hold one chain back in a validator that reports
        /// everything else.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_AChainAskingToStop_ReportsOnceInAValidatorThatReportsAll()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.ValidationBehaviors.Property = ValidationBehavior.All;

            validator.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.StopAtFirstError)
                .NotEmpty()
                .MaximumLength(TwoFailingPropertiesValidator.MaximumLength);

            validator.Property(c => c.RegistrationPlate)
                .NotEmpty()
                .MaximumLength(TwoFailingPropertiesValidator.MaximumLength);

            // Act
            var result = await validator.ValidateAsync(TwoFailingPropertiesValidator.BrokenCar());

            // Assert
            result.ShouldReport([
                ExpectedError.Any("Vin"),                            // held back to one by the override
                ExpectedError.Any("RegistrationPlate"),
                ExpectedError.Any("RegistrationPlate")]);            // still reports both
        }

        /// <summary>
        /// An axis left unset takes what the level above settled, so naming one never silently changes
        /// the other.
        /// </summary>
        [Theory]
        [InlineData(ValidationBehavior.All, null, "Vin", "RegistrationPlate")] // the chain axis keeps its default of one message per property
        [InlineData(null, ValidationBehavior.All, "Vin", "Vin", "RegistrationPlate", "RegistrationPlate")] // the run axis keeps its default of every property
        public async Task ValidateAsync_NamingOneValidationBehavior_LeavesTheOtherInheriting(
            ValidationBehavior? classBehavior,
            ValidationBehavior? propertyBehavior,
            params string[] expectedCodes)
        {
            // Arrange
            var validator = new TwoFailingPropertiesValidator(classBehavior, propertyBehavior);

            // Act
            var result = await validator.ValidateAsync(TwoFailingPropertiesValidator.BrokenCar());

            // Assert
            result.ShouldReport(expectedCodes.Select(ExpectedError.Any));
        }

        /// <summary>
        /// It is resolved while validating, not while the rules are declared, so whether it is written
        /// before or after the rules makes no difference.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_AppliesTheValidationBehaviors_DeclaredAfterTheRules()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().MaximumLength(TwoFailingPropertiesValidator.MaximumLength);
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            validator.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            // Act
            var result = await validator.ValidateAsync(TwoFailingPropertiesValidator.BrokenCar());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// A property whose condition does not hold reported nothing, so there is nothing for a
        /// stopping run to stop on and the property after it is still judged.
        /// </summary>
        [Theory]
        [InlineData(false, "RegistrationPlate", "RegistrationPlate is required.")]
        [InlineData(true, "Vin", "Vin is required.")]
        public async Task ValidateAsync_StoppingAtTheFirstError_IsNotStoppedByASkippedProperty(
            bool sold,
            string expectedCode,
            string expectedMessage)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            validator.Property(c => c.Vin).NotEmpty().When(c => c.SoldDate != null);
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            var car = new Car { SoldDate = sold ? new DateTime(2024, 1, 1) : null };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport(expectedCode, expectedMessage);
        }

        /// <summary>
        /// Stopping caps how far a run goes, not how many messages it may carry back. A run is stopped
        /// between rules, and one rule can report several at once — so a composed validator's findings
        /// are passed on whole rather than truncated to make the count come out at one.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_StoppingAtTheFirstError_DoesNotTruncateWhatOneRuleReported()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();
            modelValidator.Property(m => m.SeatCount).GreaterThan(0);

            var validator = new TestValidator<Car>();
            validator.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            validator.Property(c => c.Model).SetValidator(modelValidator);
            validator.Property(c => c.Vin).NotEmpty();

            var car = new Car { Model = new CarModel() };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            // Vin is absent: the run still stops once that rule has reported
            result.ShouldReport([
                new("Model.Name", "Name is required."),
                new("Model.SeatCount", "SeatCount must be greater than 0.")]);
        }

        /// <summary>
        /// A composed validator decides for itself: it stops at its own first error without cutting the
        /// parent short, and the parent's willingness to report everything does not overrule it.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_AComposedValidator_DecidesForItself()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            modelValidator.Property(m => m.Name).NotEmpty();
            modelValidator.Property(m => m.SeatCount).GreaterThan(0);

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).SetValidator(modelValidator);
            validator.Property(c => c.Vin).NotEmpty();

            var car = new Car { Model = new CarModel() };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([new("Model.Name", "Name is required."), new("Vin", "Vin is required.")]);
        }

        [Fact]
        public async Task ValidateAsync_WithMessage_ReplacesTheMessageOfTheRuleItFollows()
        {
            // Arrange
            const string message = "Please tell us the VIN.";

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithMessage(message);

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", message);
        }

        /// <summary>
        /// It belongs to one rule, not to the whole chain, so the other rules keep the shared wording.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithMessage_LeavesTheOtherRulesOfTheChainAlone()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.All)
                .NotEmpty().WithMessage("custom")
                .MaximumLength(3);

            var car = new Car { Vin = "far too long" };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Vin", "Vin must not exceed 3 characters.");
        }

        [Fact]
        public async Task ValidateAsync_WithMessage_ResolvesADeferredMessage_WhileValidating()
        {
            // Arrange
            var message = "first";

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithMessage(() => message);

            // Act
            message = "second";
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "second");
        }

        /// <summary>
        /// A rule which reports its own property names (one error per element of a collection, say) keeps them;
        /// only the wording is replaced.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithMessage_KeepsTheCodeARuleReportsUnderItself()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds)
                .Add(context => context.AddError(new ValidationError("FeatureIds[0]", "the original message")))
                .WithMessage("the replacement");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("FeatureIds[0]", "the replacement");
        }

        /// <summary>
        /// A message of the caller's own is a template like the shipped ones, substituted against exactly
        /// the arguments the rule supplies. Handing it back unsubstituted would render its braces into
        /// the response, and naming a placeholder is the first thing anyone tries.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithMessage_SubstitutesThePlaceholdersTheRuleSupplies()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .MinimumLength(17)
                .WithMessage("{PropertyName} needs {MinLength} characters.");

            var car = Cars.Car();
            car.Vin = "TOOSHORT";

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Vin", "Vin needs 17 characters.");
        }

        /// <summary>
        /// The display name is what a message calls the property, so an overridden message gets it too.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithMessage_SubstitutesTheDisplayName()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .WithDisplayName("Vehicle ID")
                .NotEmpty()
                .WithMessage("{PropertyName} is missing.");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vehicle ID is missing.");
        }

        [Fact]
        public async Task ValidateAsync_WithMessage_CanNameTheObjectBeingValidated()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .Must(vin => vin == null, "replaced below")
                .WithMessage(car => $"The VIN {car.Vin} is already registered.");

            var car = Cars.Car();
            car.Vin = "WVWZZZ1JZXW000001";

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Vin", "The VIN WVWZZZ1JZXW000001 is already registered.");
        }

        [Fact]
        public async Task ValidateAsync_WithMessage_CanNameTheValueThatFailed()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage)
                .GreaterThan(100_000)
                .WithMessage((_, mileage) => $"{mileage} km is not enough.");

            var car = Cars.Car();
            car.Mileage = 42;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Mileage", "42 km is not enough.");
        }

        /// <summary>
        /// What a composed validator found is its own judgement: it reported several things, each under
        /// its own name, and one replacement wording cannot stand for all of them. Refused where it is
        /// written rather than producing the same sentence under every one of those property names.
        /// </summary>
        [Fact]
        public void WithMessage_AfterSetValidator_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();

            // Act
            var act = () => validator.Property(c => c.Model)
                .SetValidator(modelValidator)
                .WithMessage("the model is invalid");

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*composed validator*");
        }

        /// <summary>
        /// Every shipped rule names itself, so a client can tell two failures of one property apart
        /// without reading English — which three of the built-in messages make impossible, since
        /// NotEmpty, NotNull and NotDefault all render "is required".
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ReportsTheCodeOfTheRuleThatFailed()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).MinimumLength(17);

            var car = Cars.Car();
            car.Vin = "SHORT";

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReportErrorCode("Vin", "MinimumLength");
        }

        /// <summary>
        /// The arguments the message was rendered from, for a caller which logs the failure in parts.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_CarriesTheArgumentsTheMessageWasRenderedFrom()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).WithDisplayName("Vehicle ID").MinimumLength(17);

            var car = Cars.Car();
            car.Vin = "SHORT";

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            var arguments = result.Errors.Single().Arguments!;
            arguments["PropertyName"].Should().Be("Vehicle ID");
            arguments["MinLength"].Should().Be(17);
        }

        /// <summary>
        /// One token: it names the rule a client branches on and selects the text the host resolves, so
        /// a rule of the caller's own is localizable without a second modifier.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithErrorCode_NamesTheRuleAndResolvesTheMessageUnderIt()
        {
            // Arrange
            var validator = new TestValidator<Car>(new StubMessageProvider("SwissPlate", "The plate must be Swiss."));
            validator.Property(c => c.Vin)
                .Must(vin => vin != null && vin.StartsWith("CH", StringComparison.Ordinal))
                .WithErrorCode("SwissPlate");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReportErrorCode("Vin", "SwissPlate");
            result.Errors.Single().Message.Should().Be("The plate must be Swiss.");
        }

        /// <summary>
        /// Where the failure is reported and which rule reported it are separate answers, so overriding
        /// one leaves the other alone.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithPropertyName_MovesThePathAndLeavesTheCode()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).WithPropertyName("vehicleId").NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReportErrorCode("vehicleId", "NotEmpty");
        }

        /// <summary>
        /// A failure a composed validator reported keeps its own identity while its path is prefixed,
        /// so the rule that fired is still legible from the outside.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithANestedValidator_KeepsTheCodeOfTheRuleThatFailed()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).SetValidator(modelValidator);

            var car = Cars.Car();
            car.Model!.Name = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReportErrorCode("Model.Name", "NotEmpty");
        }

        /// <summary>
        /// The same for an element of a collection, whose path also gains its position.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithElementRules_KeepsTheCodeOfTheRuleThatFailed()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReportErrorCode("ServiceHistory[0].Workshop", "NotEmpty");
        }

        /// <summary>
        /// Answers one named code and falls back to the built-in English for the rest.
        /// </summary>
        private sealed class StubMessageProvider(string errorCode, string message) : IValidationMessageProvider
        {
            public string GetMessage(string code, IReadOnlyDictionary<string, object?> arguments)
            {
                return string.Equals(code, errorCode, StringComparison.Ordinal)
                    ? message
                    : DefaultValidationMessageProvider.Instance.GetMessage(code, arguments);
            }
        }

        [Fact]
        public void WithErrorCode_WithABlankCode_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).NotEmpty().WithErrorCode("  ");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithErrorCode_WithoutARuleToApplyItTo_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).WithErrorCode("VIN_REQUIRED");

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// For the same reason <c>WithMessage</c> is: a composed validator reported several things, each
        /// under its own code, and one code cannot stand for all of them.
        /// </summary>
        [Fact]
        public void WithErrorCode_AfterSetValidator_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();

            // Act
            var act = () => validator.Property(c => c.Model)
                .SetValidator(modelValidator)
                .WithErrorCode("MODEL_INVALID");

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*composed validator*");
        }

        /// <summary>
        /// The escape hatch for a hot path: the same rule, without the expression tree behind it.
        /// </summary>
        [Fact]
        public async Task Property_NamedAndReadDirectly_ReportsUnderThatName()
        {
            // Arrange
            var validator = new NamedPropertyCarValidator();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReportErrorCode("Vin", "NotEmpty");
        }

        /// <summary>
        /// Without a reachability predicate the accessor would dereference whatever the payload omitted,
        /// so the overload that takes one is what makes a path through another object safe.
        /// </summary>
        [Fact]
        public async Task Property_NamedWithAReachabilityPredicate_SkipsWhatIsNotThere()
        {
            // Arrange
            var validator = new NamedNestedPropertyCarValidator();

            // Act
            var absent = await validator.ValidateAsync(new Car());
            var present = await validator.ValidateAsync(new Car { Model = new CarModel { Name = null } });

            // Assert
            absent.Errors.Should().BeEmpty();
            present.ShouldReportErrorCode("Model.Name", "NotEmpty");
        }

        [Fact]
        public void Property_NamedWithABlankName_Throws()
        {
            // Act
            var act = () => new BlankNameCarValidator();

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        private sealed class NamedPropertyCarValidator : Validator<Car>
        {
            public NamedPropertyCarValidator()
            {
                this.Property("Vin", static c => c.Vin).NotEmpty();
            }
        }

        private sealed class NamedNestedPropertyCarValidator : Validator<Car>
        {
            public NamedNestedPropertyCarValidator()
            {
                this.Property("Model.Name", static c => c.Model!.Name, static c => c.Model != null).NotEmpty();
            }
        }

        private sealed class BlankNameCarValidator : Validator<Car>
        {
            public BlankNameCarValidator()
            {
                this.Property("  ", static c => c.Vin).NotEmpty();
            }
        }

        [Fact]
        public void WithMessage_WithoutARuleToApplyItTo_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).WithMessage("nothing to apply this to");

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// The opt-in which replaces the C# property name in the wording — the propertyName is what callers bind
        /// to, so it must not follow.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_DisplayName_NamesThePropertyInTheMessage_WithoutChangingTheCode()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).WithDisplayName("Vehicle identification number").NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vehicle identification number is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithoutADisplayName_NamesThePropertyByItsCode()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// A localized display name is a resource, and which text it resolves to depends on the culture
        /// at the time of validation rather than of the construction.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_DisplayName_ResolvesADeferredNameWhileValidating()
        {
            // Arrange
            var displayName = "first";

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).WithDisplayName(() => displayName).NotEmpty();

            // Act
            displayName = "second";
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "second is required.");
        }

        [Theory]
        [InlineData(true, false)] // the condition holds, so the empty VIN is reported
        [InlineData(false, true)] // it does not, so the property is not validated at all
        public async Task ValidateAsync_WithWhen_AppliesTheRules_OnlyWhenTheConditionHolds(bool isSold, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().When(c => c.SoldDate != null);

            var car = new Car { SoldDate = isSold ? DateTime.UtcNow : null };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(true, true)] // the condition holds, so the rules are skipped
        [InlineData(false, false)]
        public async Task ValidateAsync_WithUnless_SkipsTheRules_WhenTheConditionHolds(bool isUnsold, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().Unless(c => c.SoldDate == null);

            var car = new Car { SoldDate = isUnsold ? null : DateTime.UtcNow };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// The condition covers the whole chain, not just the rule it happens to follow.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithWhen_AppliesToEveryRuleOfTheChain()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.All)
                .NotEmpty()
                .MaximumLength(3)
                .When(c => c.SoldDate != null);

            var car = new Car { Vin = "far too long" };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateAsync_WithSeveralConditions_SkipsTheRules_WhenOnlyOneHolds()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .NotEmpty()
                .When(c => c.SoldDate != null)
                .When(c => c.Model != null);

            var car = new Car { SoldDate = DateTime.UtcNow };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateAsync_WithSeveralConditions_AppliesTheRules_WhenAllOfThemHold()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .NotEmpty()
                .When(c => c.SoldDate != null)
                .When(c => c.Model != null);

            var car = new Car { SoldDate = DateTime.UtcNow, Model = new CarModel() };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// A chain declared through an object the payload omitted reports nothing, rather than throwing
        /// a NullReferenceException and turning a bad request into a server error. Whether the object
        /// has to be there at all is a rule of its own.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithANestedPath_SkipsTheChain_WhenTheObjectInBetweenIsMissing()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Name).NotEmpty();

            var car = Cars.Car();
            car.Model = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue("a name that is not there cannot be judged");
        }

        /// <summary>
        /// Every object on the way is asked, shallowest first, so a deeper one is never read through a
        /// shallower one that is missing.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithADeepNestedPath_SkipsTheChain_WhateverIsMissing()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Manufacturer!.Name).NotEmpty();

            var withoutManufacturer = Cars.Car();
            withoutManufacturer.Model!.Manufacturer = null;

            var withoutModel = Cars.Car();
            withoutModel.Model = null;

            // Act
            var missingManufacturer = await validator.ValidateAsync(withoutManufacturer);
            var missingModel = await validator.ValidateAsync(withoutModel);

            // Assert
            missingManufacturer.Succeeded.Should().BeTrue();
            missingModel.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// The guard only decides whether the chain can run; a path that is reachable is judged exactly
        /// as before.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithANestedPath_ReportsTheFailure_WhenThePathIsReachable()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Manufacturer!.Name).NotEmpty();

            var car = Cars.Car();
            car.Model!.Manufacturer!.Name = "";

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Model.Manufacturer.Name", "Model.Manufacturer.Name is required.");
        }

        /// <summary>
        /// The property is not even read when the condition does not hold, which is what makes a chain
        /// on a nested path safe when the object in between may be absent.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithWhen_DoesNotReadTheProperty_WhenTheConditionDoesNotHold()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Name).NotEmpty().When(c => c.Model != null);

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateAsync_WithoutAnInstance_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            // Act
            var act = () => validator.ValidateAsync(null!).AsTask();

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ValidateAsync_WithACancelledToken_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            // Act
            var act = () => validator.ValidateAsync(Cars.Car(), cancellation.Token).AsTask();

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        /// <summary>
        /// The property name comes from a property path, so anything else has to be rejected where the rule
        /// is declared rather than producing a nonsensical propertyName at runtime.
        /// </summary>
        [Fact]
        public void Property_WithAnExpressionWhichIsNotAProperty_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin!.Length + 1).GreaterThan(0);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// A validator constructed without the DI registration still produces readable messages.
        /// </summary>
        [Fact]
        public void Messages_DefaultToTheBuiltInProvider()
        {
            // Act
            var validator = new TestValidator<Car>();

            // Assert
            validator.Messages.Should().BeOfType<DefaultValidationMessageProvider>();
        }

        /// <summary>
        /// A rule that really suspends — one that queries something — is awaited like any other, which
        /// is the whole reason there is no synchronous entry point to refuse it.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithARuleThatSuspends_AwaitsIt()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).AddAsync(async (context, cancellationToken) =>
            {
                await Task.Delay(1, cancellationToken);

                context.AddError(new ValidationError(context.PropertyName, "checked elsewhere"));
            });

            // Act
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.ShouldReport("Vin", "checked elsewhere");
        }

        /// <summary>
        /// The convenience for a caller that would rather treat a failure as an exception than as a
        /// result to inspect.
        /// </summary>
        [Fact]
        public async Task ValidateAndThrowAsync_WithAnInvalidInstance_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            var car = Cars.Car();
            car.Vin = "";

            // Act
            var act = () => validator.ValidateAndThrowAsync(car).AsTask();

            // Assert
            var exception = await act.Should().ThrowAsync<ValidationException>();
            exception.Which.Errors.Should().ContainKey("Vin");
        }

        [Fact]
        public async Task ValidateAndThrowAsync_WithAValidInstance_Returns()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            // Act
            var act = () => validator.ValidateAndThrowAsync(Cars.Car()).AsTask();

            // Assert
            await act.Should().NotThrowAsync();
        }

        /// <summary>
        /// The provider is the seam the DI registration writes through, so a null would only surface
        /// much later, while validating.
        /// </summary>
        [Fact]
        public void Messages_CannotBeSetToNull()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Messages = null!;

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Two properties which each break two rules, so a run can be told apart both by how many messages
        /// it reports and by which properties they name. Both axes are taken through the constructor
        /// because what is under test is the setting rather than the rules.
        /// </summary>
        /// <summary>
        /// The non-generic entry point is what the ASP.NET Core filter calls, having resolved a validator
        /// by a parameter's runtime type.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ThroughTheNonGenericInterface_ValidatesTheInstance()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            // Act
            var result = await ((IValidator)validator).ValidateAsync(new Car());

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// A validator may serve more than one payload — <c>ValidatorTypeInfo</c> says so and the
        /// registration registers every closed interface it finds. Before the base class declared the
        /// non-generic member, such a type inherited two equally specific defaults and did not compile
        /// at all (CS8705), so the shape the registration supports could not be written.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ThroughTheNonGenericInterface_DispatchesOnTheInstanceType()
        {
            // Arrange
            IValidator validator = new CarAndManufacturerValidator();

            // Act
            var car = await validator.ValidateAsync(new Car());
            var manufacturer = await validator.ValidateAsync(new Manufacturer());

            // Assert
            car.ShouldReport("Vin", "Vin is required.");
            manufacturer.ShouldReport("Name", "Name is required.");
        }

        /// <summary>
        /// A single-payload validator handed something else says so, rather than reporting a failure that
        /// was never judged.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_ThroughTheNonGenericInterface_RefusesAnInstanceOfAnotherType()
        {
            // Arrange
            IValidator validator = new TestValidator<Car>();

            // Act
            var act = async () => await validator.ValidateAsync(new Manufacturer());

            // Assert
            (await act.Should().ThrowAsync<InvalidCastException>())
                .WithMessage("*validates*Car*cannot validate an instance of*Manufacturer*");
        }

        /// <summary>
        /// Validates a <see cref="Car"/> through the base class and a <see cref="Manufacturer"/> by hand.
        /// It re-implements the non-generic member because only it knows about both payloads.
        /// </summary>
        private sealed class CarAndManufacturerValidator : Validator<Car>, IValidator<Manufacturer>
        {
            public CarAndManufacturerValidator()
            {
                this.Property(c => c.Vin).NotEmpty();
            }

            public ValueTask<ValidationResult> ValidateAsync(Manufacturer instance, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(instance);

                return ValueTask.FromResult(string.IsNullOrWhiteSpace(instance.Name)
                    ? ValidationResult.FromValidationErrors(new ValidationError("Name", "Name is required."))
                    : ValidationResult.Success);
            }

            ValueTask<ValidationResult> IValidator.ValidateAsync(object instance, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(instance);

                return instance switch
                {
                    Car car => this.ValidateAsync(car, cancellationToken),
                    Manufacturer manufacturer => this.ValidateAsync(manufacturer, cancellationToken),
                    _ => throw new InvalidCastException($"Cannot validate an instance of {instance.GetType()}."),
                };
            }
        }

        private sealed class TwoFailingPropertiesValidator : Validator<Car>
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
    }
}
