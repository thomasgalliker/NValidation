namespace NValidation.Tests
{
    /// <summary>
    /// Covers the validator base class itself — how a property's error code is derived, how a rule chain
    /// runs, and how a nested validator is merged — independently of any concrete rule.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidatorTests
    {
        [Fact]
        public async Task ValidateAsync_WithValidInstance_Succeeds()
        {
            // Arrange
            var validator = new VinNotEmptyValidator();

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
            var validator = new VinNotEmptyValidator();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be(nameof(Car.Vin));
        }

        /// <summary>
        /// A nested path becomes a dotted code, which is the convention callers bind to.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_TakesTheErrorCode_FromANestedPropertyExpression()
        {
            // Arrange
            var validator = new ModelNameNotEmptyValidator();
            var car = new Car { Model = new CarModel() };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("Model.Name");
        }

        /// <summary>
        /// A property reports at most one message: once a rule fails, the rest of its chain is skipped.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_StopsTheChain_AtTheFirstFailingRule()
        {
            // Arrange
            var validator = new VinNotEmptyAndBoundedValidator();
            var car = new Car { Vin = "   " };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle();
        }

        [Fact]
        public async Task ValidateAsync_ReportsEveryFailingRule_WhenTheChainContinuesOnFailure()
        {
            // Arrange
            var validator = new VinContinueOnFailureValidator();
            var car = new Car { Vin = "wrong" };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Select(error => error.Message).Should().BeEquivalentTo(
                VinContinueOnFailureValidator.FirstMessage,
                VinContinueOnFailureValidator.SecondMessage);
        }

        [Fact]
        public async Task ValidateAsync_WithMessage_ReplacesTheMessageOfTheRuleItFollows()
        {
            // Arrange
            var validator = new VinWithMessageValidator("Please tell us the VIN.");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle()
                .Which.Should().Match<ValidationError>(
                    error => error.Code == nameof(Car.Vin) && error.Message == "Please tell us the VIN.");
        }

        /// <summary>
        /// It belongs to one rule, not to the whole chain, so the other rules keep the shared wording.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithMessage_LeavesTheOtherRulesOfTheChainAlone()
        {
            // Arrange
            var validator = new VinWithMessageOnTheFirstRuleValidator();
            var car = new Car { Vin = "far too long" };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle()
                .Which.Message.Should().Be("Vin must not exceed 3 characters.");
        }

        [Fact]
        public async Task ValidateAsync_WithMessage_ResolvesADeferredMessage_WhileValidating()
        {
            // Arrange
            var message = "first";
            var validator = new VinWithDeferredMessageValidator(() => message);

            // Act
            message = "second";
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("second");
        }

        /// <summary>
        /// A rule which reports its own codes (one error per element of a collection, say) keeps them;
        /// only the wording is replaced.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithMessage_KeepsTheCodeARuleReportsUnderItself()
        {
            // Arrange
            var validator = new FeatureIdsCustomCodeWithMessageValidator("the replacement");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle()
                .Which.Should().Match<ValidationError>(
                    error => error.Code == FeatureIdsCustomCodeWithMessageValidator.Code && error.Message == "the replacement");
        }

        [Fact]
        public void WithMessage_WithoutARuleToApplyItTo_Throws()
        {
            // Act
            var act = () => new VinWithMessageAndNoRuleValidator();

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// The opt-in which replaces the C# property name in the wording — the code is what callers bind
        /// to, so it must not follow.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_DisplayName_NamesThePropertyInTheMessage_WithoutChangingTheCode()
        {
            // Arrange
            var validator = new VinDisplayNameValidator("Vehicle identification number");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle()
                .Which.Should().Match<ValidationError>(
                    error => error.Code == nameof(Car.Vin) && error.Message == "Vehicle identification number is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithoutADisplayName_NamesThePropertyByItsCode()
        {
            // Arrange
            var validator = new VinNotEmptyValidator();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("Vin is required.");
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
            var validator = new VinDeferredDisplayNameValidator(() => displayName);

            // Act
            displayName = "second";
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("second is required.");
        }

        [Theory]
        [InlineData(true, false)] // the condition holds, so the empty VIN is reported
        [InlineData(false, true)] // it does not, so the property is not validated at all
        public async Task ValidateAsync_WithWhen_AppliesTheRules_OnlyWhenTheConditionHolds(bool isSold, bool expectedToSucceed)
        {
            // Arrange
            var validator = new VinRequiredWhenSoldValidator();
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
            var validator = new VinRequiredUnlessUnsoldValidator();
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
            var validator = new VinChainRequiredWhenSoldValidator();
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
            var validator = new VinRequiredWhenSoldAndModelledValidator();
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
            var validator = new VinRequiredWhenSoldAndModelledValidator();
            var car = new Car { SoldDate = DateTime.UtcNow, Model = new CarModel() };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
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
            var validator = new ModelNameNotEmptyValidator();
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
            var validator = new ManufacturerNameNotEmptyValidator();

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
            var validator = new ManufacturerNameNotEmptyValidator();
            var car = Cars.Car();
            car.Model!.Manufacturer!.Name = "";

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("Model.Manufacturer.Name");
        }

        /// <summary>
        /// The property is not even read when the condition does not hold, which is what makes a chain
        /// on a nested path safe when the object in between may be absent.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithWhen_DoesNotReadTheProperty_WhenTheConditionDoesNotHold()
        {
            // Arrange
            var validator = new ModelNameRequiredWhenModelPresentValidator();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateAsync_WithoutAnInstance_Throws()
        {
            // Arrange
            var validator = new VinNotEmptyValidator();

            // Act
            var act = () => validator.ValidateAsync(null!).AsTask();

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ValidateAsync_WithACancelledToken_Throws()
        {
            // Arrange
            var validator = new VinNotEmptyValidator();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            // Act
            var act = () => validator.ValidateAsync(Cars.Car(), cancellation.Token).AsTask();

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        /// <summary>
        /// The error code comes from a property path, so anything else has to be rejected where the rule
        /// is declared rather than producing a nonsensical code at runtime.
        /// </summary>
        [Fact]
        public void Property_WithAnExpressionWhichIsNotAProperty_Throws()
        {
            // Act
            var act = () => new NotAPropertyValidator();

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
            var validator = new VinNotEmptyValidator();

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
            var validator = new VinSuspendingValidator();

            // Act
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("Vin");
        }

        /// <summary>
        /// The convenience for a caller that would rather treat a failure as an exception than as a
        /// result to inspect.
        /// </summary>
        [Fact]
        public async Task ValidateAndThrowAsync_WithAnInvalidInstance_Throws()
        {
            // Arrange
            var validator = new VinNotEmptyValidator();
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
            var validator = new VinNotEmptyValidator();

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
            var validator = new VinNotEmptyValidator();

            // Act
            var act = () => validator.Messages = null!;

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
