namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Theory]
        [InlineData(25.00, true)]
        [InlineData(25.05, true)]
        [InlineData(25.03, false)]
        public async Task MultipleOf_RequiresAnExactMultiple(double purchasePrice, bool expectedToSucceed)
        {
            // Arrange
            var validator = new PurchasePriceMultipleOfValidator(0.05m);
            var car = Cars.Car();
            car.PurchasePrice = (decimal)purchasePrice;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(-25.05, true)] // a negative multiple is still a multiple
        [InlineData(-25.03, false)]
        public async Task MultipleOf_JudgesANegativeValue_TheSameWay(double purchasePrice, bool expectedToSucceed)
        {
            // Arrange
            var validator = new PurchasePriceMultipleOfValidator(0.05m);
            var car = Cars.Car();
            car.PurchasePrice = (decimal)purchasePrice;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(25.05d, true)]
        [InlineData(25.03d, false)]
        public async Task MultipleOf_WithANullableProperty_JudgesOnlyAValueThatIsThere(double? tradeInValue, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TradeInValueMultipleOfValidator(0.05m);
            var car = Cars.Car();
            car.TradeInValue = tradeInValue == null ? null : (decimal)tradeInValue.Value;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(120, true)]
        [InlineData(125, false)]
        public async Task MultipleOf_WithAWholeNumber_RequiresAnExactMultiple(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageMultipleOfValidator(12);
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// A zero step divides by zero. The mistake is in the rule, not in the data, so it surfaces
        /// where the rule is declared rather than on the first request that happens to reach it.
        /// </summary>
        [Fact]
        public void MultipleOf_WithAZeroStep_ThrowsWhileTheRuleIsDeclared()
        {
            // Act
            var act = () => new PurchasePriceMultipleOfValidator(0m);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        /// <inheritdoc cref="MultipleOf_WithAZeroStep_ThrowsWhileTheRuleIsDeclared" path="/summary"/>
        [Fact]
        public void MultipleOf_WithAZeroWholeNumberStep_ThrowsWhileTheRuleIsDeclared()
        {
            // Act
            var act = () => new MileageMultipleOfValidator(0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task NotNaN_AcceptsAMeasuredFigure()
        {
            // Arrange
            var validator = new FuelConsumptionNotNaNValidator();

            // Act
            var result = await validator.ValidateAsync(Cars.CarModel());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task NotNaN_TreatsNaN_AsMissing()
        {
            // Arrange
            var validator = new FuelConsumptionNotNaNValidator();
            var carModel = Cars.CarModel();
            carModel.FuelConsumption = double.NaN;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        /// <summary>
        /// An infinity is a value, not a missing measurement, so this rule has nothing to say about it —
        /// a range rule is what rejects it.
        /// </summary>
        [Theory]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public async Task NotNaN_AcceptsAnInfinity(double fuelConsumption)
        {
            // Arrange
            var validator = new FuelConsumptionNotNaNValidator();
            var carModel = Cars.CarModel();
            carModel.FuelConsumption = fuelConsumption;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task NotNaN_ReportsItsOwnMessage()
        {
            // Arrange
            var validator = new FuelConsumptionNotNaNValidator();
            var carModel = Cars.CarModel();
            carModel.FuelConsumption = double.NaN;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("FuelConsumption must be a number.");
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(213.5f, true)]
        public async Task NotNaN_WithANullableSingle_JudgesOnlyAValueThatIsThere(float? topSpeed, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TopSpeedNotNaNValidator();
            var carModel = Cars.CarModel();
            carModel.TopSpeed = topSpeed;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task NotNaN_WithANullableSingle_RejectsNaN()
        {
            // Arrange
            var validator = new TopSpeedNotNaNValidator();
            var carModel = Cars.CarModel();
            carModel.TopSpeed = float.NaN;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task MultipleOf_ReportsMultipleOf()
        {
            // Act
            var result = await new PurchasePriceMultipleOfValidator(0.05m).ValidateForKeysAsync(new Car { PurchasePrice = 0.03m });

            // Assert
            result.ShouldReport(nameof(Car.PurchasePrice), ValidationMessageKeys.MultipleOf);
        }

        [Fact]
        public async Task NotNaN_ReportsNotNaN_NotNotEmpty()
        {
            // Act
            var result = await new FuelConsumptionNotNaNValidator().ValidateForKeysAsync(new CarModel { FuelConsumption = double.NaN });

            // Assert
            result.ShouldReport(nameof(CarModel.FuelConsumption), ValidationMessageKeys.NotNaN);
        }

    }
}
