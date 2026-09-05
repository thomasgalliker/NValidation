namespace NValidation.Tests
{
    /// <summary>
    /// Covers every rule the validation core ships, including the ones no validator in the test domain
    /// uses: they are part of the library surface, so they have to be proven on their own rather than
    /// only through whichever validator happens to call them.
    /// </summary>
    /// <remarks>
    /// Split by subject the same way the rules themselves are. This file holds the two rules that take
    /// the caller's own predicate or validator.
    /// </remarks>
    [Trait(Traits.Category, Traits.UnitTests)]
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Fact]
        public async Task Must_WithASatisfiedPredicate_Succeeds()
        {
            // Arrange
            var validator = new VinMustBeSeventeenCharactersValidator();

            // Act
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Must_ReportsTheSuppliedMessage_UnderThePropertyCode()
        {
            // Arrange
            var validator = new VinMustBeSeventeenCharactersValidator();
            var car = Cars.Car();
            car.Vin = "TOOSHORT";

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle()
                .Which.Should().Match<ValidationError>(
                    error => error.Code == nameof(Car.Vin) && error.Message == VinMustBeSeventeenCharactersValidator.Message);
        }

        /// <summary>
        /// The deferred overload exists for messages which depend on the current request, e.g. a
        /// localized resource: it must be read while validating, not while the rule is declared.
        /// </summary>
        [Fact]
        public async Task Must_ResolvesADeferredMessage_WhileValidating()
        {
            // Arrange
            var message = "first";
            var validator = new VinMustDeferredMessageValidator(() => message);

            // Act
            message = "second";
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("second");
        }

        [Theory]
        [InlineData(false, 0, true)] // not listed, so the price is nobody's business
        [InlineData(true, 0, false)] // listed without a price
        [InlineData(true, 10, true)]
        public async Task Must_CanDecideFromAnotherProperty(bool isListedForSale, decimal purchasePrice, bool expectedToSucceed)
        {
            // Arrange
            var validator = new ListedCarNeedsAPriceValidator();
            var car = Cars.Car();
            car.IsListedForSale = isListedForSale;
            car.PurchasePrice = purchasePrice;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task Must_WithTheWholeObject_ResolvesADeferredMessage_WhileValidating()
        {
            // Arrange
            var message = "first";
            var validator = new PurchasePriceMustDeferredMessageValidator(() => message);

            // Act
            message = "second";
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("second");
        }

        [Fact]
        public void Must_WithoutAPredicate_Throws()
        {
            // Act
            var act = () => new VinMustValidator(null!, "a message");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Must_WithoutAMessage_Throws()
        {
            // Act
            var act = () => new VinMustValidator(vin => vin != null, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// The nested validator keeps its own flat codes; the parent is what prefixes them with the
        /// property the nested object sits on.
        /// </summary>
        [Fact]
        public async Task SetValidator_PrefixesTheErrorsOfTheNestedValidator()
        {
            // Arrange
            var validator = new ModelSetValidatorValidator(new CarModelNameValidator());
            var car = Cars.Car();
            car.Model = new CarModel();

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("Model.Name");
        }

        /// <summary>
        /// The nested validator answers through the provider of the run it was composed into, so a
        /// caller that resolves messages its own way gets that wording all the way down rather than
        /// the built-in English for anything behind a nested validator.
        /// </summary>
        [Fact]
        public async Task SetValidator_ResolvesTheNestedValidatorsMessagesThroughTheRunsProvider()
        {
            // Arrange
            var validator = new ModelSetValidatorValidator(new CarModelNameValidator());
            var car = Cars.Car();
            car.Model = new CarModel();

            // Act
            var result = await validator.ValidateForKeysAsync(car);

            // Assert
            result.ShouldReport("Model.Name", ValidationMessageKeys.NotEmpty);
        }

        [Fact]
        public async Task SetValidator_SkipsTheNestedValidator_WhenTheObjectIsMissing()
        {
            // Arrange
            var validator = new ModelSetValidatorValidator(new CarModelNameValidator());
            var car = Cars.Car();
            car.Model = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue("an absent object is left to NotNull");
        }

        [Fact]
        public void SetValidator_WithoutAValidator_Throws()
        {
            // Act
            var act = () => new ModelSetValidatorValidator(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
