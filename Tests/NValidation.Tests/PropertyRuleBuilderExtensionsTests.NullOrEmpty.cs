namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("WAUZZZ8V5KA123456", true)]
        public async Task NotEmpty_RequiresANonBlankText(string? vin, bool expectedToSucceed)
        {
            // Arrange
            var validator = new VinNotEmptyValidator();
            var car = Cars.Car();
            car.Vin = vin;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task NotEmpty_ReportsUnderTheNotEmptyKey()
        {
            // Arrange
            var validator = new VinNotEmptyValidator();
            var car = Cars.Car();
            car.Vin = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("Vin is required.");
        }

        /// <summary>
        /// A date carries no null, so the default value is what counts as unset.
        /// </summary>
        [Fact]
        public async Task NotDefault_TreatsADefaultDate_AsMissing()
        {
            // Arrange
            var validator = new FirstRegistrationNotDefaultValidator();
            var car = Cars.Car();
            car.FirstRegistration = default;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task NotDefault_AcceptsADateThatWasSet()
        {
            // Arrange
            var validator = new FirstRegistrationNotDefaultValidator();

            // Act
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// An enum's zero member is what a client sends while nothing was picked.
        /// </summary>
        [Theory]
        [InlineData(CarCondition.Unknown, false)]
        [InlineData(CarCondition.New, true)]
        [InlineData(CarCondition.Used, true)]
        public async Task NotDefault_RequiresAChoice_ToHaveBeenMade(CarCondition condition, bool expectedToSucceed)
        {
            // Arrange
            var validator = new ConditionNotDefaultValidator();
            var car = Cars.Car();
            car.Condition = condition;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// On a nullable property both a missing value and a default one count as unset.
        /// </summary>
        [Fact]
        public async Task NotDefault_WithANullableProperty_TreatsAMissingValue_AsMissing()
        {
            // Arrange
            var validator = new SoldDateNotDefaultValidator();
            var car = Cars.Car();
            car.SoldDate = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task NotDefault_WithANullableProperty_TreatsADefaultValue_AsMissing()
        {
            // Arrange
            var validator = new SoldDateNotDefaultValidator();
            var car = Cars.Car();
            car.SoldDate = default(DateTime);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task NotNull_RequiresTheObject()
        {
            // Arrange
            var validator = new ModelNotNullValidator();
            var car = Cars.Car();
            car.Model = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task NotNull_AcceptsAnObjectThatIsThere()
        {
            // Arrange
            var validator = new ModelNotNullValidator();

            // Act
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(0d, true)]
        [InlineData(42d, true)]
        public async Task NotNull_WithAValueType_RequiresAValue(double? tradeInValue, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TradeInValueNotNullValidator();
            var car = Cars.Car();
            car.TradeInValue = tradeInValue == null ? null : (decimal)tradeInValue.Value;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task NotEmpty_WithACollection_RejectsAnEmptyOne()
        {
            // Arrange
            var validator = new FeatureIdsNotEmptyValidator();
            var car = Cars.Car();
            car.FeatureIds = [];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task NotEmpty_WithACollection_RejectsAMissingOne()
        {
            // Arrange
            var validator = new FeatureIdsNotEmptyValidator();
            var car = Cars.Car();
            car.FeatureIds = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task NotEmpty_WithACollection_AcceptsOneThatHasEntries()
        {
            // Arrange
            var validator = new FeatureIdsNotEmptyValidator();

            // Act
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// The rule is declared for any enumerable, so it binds to a property declared as a concrete
        /// list just as well as to one declared as an interface.
        /// </summary>
        [Fact]
        public async Task NotEmpty_WithAConcreteListProperty_Binds()
        {
            // Arrange
            var validator = new PreviousOwnerIdsNotEmptyValidator();
            var car = Cars.Car();
            car.PreviousOwnerIds = [];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        /// <summary>
        /// A property declared as a bare sequence is enumerated afresh every time it is read, so
        /// knowing it is not empty must cost one entry rather than all of them.
        /// </summary>
        [Fact]
        public async Task NotEmpty_OnALazySequence_StopsAtTheFirstEntry()
        {
            // Arrange
            var sequence = new CountingSequence([1, 2, 3, 4, 5]);
            var validator = new ServiceMileagesNotEmptyValidator();

            // Act
            var result = await validator.ValidateAsync(new Car { ServiceMileages = sequence });

            // Assert
            result.Succeeded.Should().BeTrue();
            sequence.Enumerated.Should().Be(1);
        }

        [Fact]
        public async Task NotEmpty_OnText_ReportsNotEmpty()
        {
            // Act
            var result = await new VinNotEmptyValidator().ValidateForKeysAsync(new Car());

            // Assert
            result.ShouldReport(nameof(Car.Vin), ValidationMessageKeys.NotEmpty);
        }

        [Fact]
        public async Task NotEmpty_OnACollection_ReportsNotEmpty()
        {
            // Act
            var result = await new FeatureIdsNotEmptyValidator().ValidateForKeysAsync(new Car { FeatureIds = [] });

            // Assert
            result.ShouldReport(nameof(Car.FeatureIds), ValidationMessageKeys.NotEmpty);
        }

        [Fact]
        public async Task NotDefault_ReportsNotDefault_NotNotEmpty()
        {
            // Act
            var result = await new FirstRegistrationNotDefaultValidator().ValidateForKeysAsync(new Car());

            // Assert
            result.ShouldReport(nameof(Car.FirstRegistration), ValidationMessageKeys.NotDefault);
        }

        [Fact]
        public async Task NotNull_ReportsNotNull()
        {
            // Act
            var result = await new ModelNotNullValidator().ValidateForKeysAsync(new Car());

            // Assert
            result.ShouldReport(nameof(Car.Model), ValidationMessageKeys.NotNull);
        }

    }
}
