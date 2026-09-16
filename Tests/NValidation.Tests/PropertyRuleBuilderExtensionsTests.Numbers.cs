namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Theory]
        [InlineData(123.45, true)]    // exactly the shape allowed
        [InlineData(0.5, true)]       // no digits before the point at all
        [InlineData(123.456, false)]  // one digit too many after the point
        [InlineData(1234.5, false)]   // one digit too many before it
        [InlineData(0, true)]
        [InlineData(-123.45, true)]   // the sign is not a digit
        public async Task PrecisionScale_JudgesDigitsBeforeAndAfterThePoint(double purchasePrice, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.PurchasePrice).PrecisionScale(5, 2);

            var car = Cars.Car();
            car.PurchasePrice = (decimal)purchasePrice;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// Trailing zeros are representation, not value: the column behind the property accepts both
        /// spellings of the same number, so the rule judges the number.
        /// </summary>
        [Fact]
        public async Task PrecisionScale_IgnoresTrailingZeros()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.PurchasePrice).PrecisionScale(3, 1);

            var car = Cars.Car();
            car.PurchasePrice = 1.50m;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(1.234, false)]
        [InlineData(1.23, true)]
        public async Task PrecisionScale_WithANullableProperty_JudgesOnlyAValueThatIsThere(double? tradeInValue, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.TradeInValue).PrecisionScale(5, 2);

            var car = Cars.Car();
            car.TradeInValue = (decimal?)tradeInValue;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task PrecisionScale_ReportsTheRuleAndNamesTheShape()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.PurchasePrice).PrecisionScale(5, 2);

            var car = Cars.Car();
            car.PurchasePrice = 123.456m;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReportErrorCode("PurchasePrice", "PrecisionScale");
            result.Errors.Single().Message.Should().Be(
                "PurchasePrice must not have more than 5 digits in total, with at most 2 after the decimal point.");
        }

        [Theory]
        [InlineData(0, 2)]   // a precision of nothing
        [InlineData(-1, 0)]
        [InlineData(2, -1)]  // a negative scale
        [InlineData(2, 3)]   // more decimals than digits
        public void PrecisionScale_WithAnImpossibleShape_ThrowsWhereTheRuleIsDeclared(int precision, int scale)
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.PurchasePrice).PrecisionScale(precision, scale);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(25.00, true)]
        [InlineData(25.05, true)]
        [InlineData(25.03, false)]
        public async Task MultipleOf_RequiresAnExactMultiple(double purchasePrice, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.PurchasePrice).MultipleOf(0.05m);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.PurchasePrice).MultipleOf(0.05m);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.TradeInValue).MultipleOf(0.05m);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).MultipleOf(12);

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
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.PurchasePrice).MultipleOf(0m);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        /// <inheritdoc cref="MultipleOf_WithAZeroStep_ThrowsWhileTheRuleIsDeclared" path="/summary"/>
        [Fact]
        public void MultipleOf_WithAZeroWholeNumberStep_ThrowsWhileTheRuleIsDeclared()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Mileage).MultipleOf(0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task NotNaN_AcceptsAMeasuredFigure()
        {
            // Arrange
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.FuelConsumption).NotNaN();

            // Act
            var result = await validator.ValidateAsync(Cars.CarModel());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task NotNaN_TreatsNaN_AsMissing()
        {
            // Arrange
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.FuelConsumption).NotNaN();

            var carModel = Cars.CarModel();
            carModel.FuelConsumption = double.NaN;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.ShouldReport("FuelConsumption", "FuelConsumption must be a number.");
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
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.FuelConsumption).NotNaN();

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
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.FuelConsumption).NotNaN();

            var carModel = Cars.CarModel();
            carModel.FuelConsumption = double.NaN;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.ShouldReport("FuelConsumption", "FuelConsumption must be a number.");
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(213.5f, true)]
        public async Task NotNaN_WithANullableSingle_JudgesOnlyAValueThatIsThere(float? topSpeed, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.TopSpeed).NotNaN();

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
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.TopSpeed).NotNaN();

            var carModel = Cars.CarModel();
            carModel.TopSpeed = float.NaN;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.ShouldReport("TopSpeed", "TopSpeed must be a number.");
        }

        [Fact]
        public async Task MultipleOf_ReportsMultipleOf()
        {
            // Arrange
            var validator = new TestValidator<Car>(ErrorCodeProvider.Instance);
            validator.Property(c => c.PurchasePrice).MultipleOf(0.05m);

            // Act
            var result = await validator.ValidateAsync(new Car { PurchasePrice = 0.03m });

            // Assert
            result.ShouldReport("PurchasePrice", "MultipleOf");
        }

        [Fact]
        public async Task NotNaN_ReportsNotNaN_NotNotEmpty()
        {
            // Arrange
            var validator = new TestValidator<CarModel>(ErrorCodeProvider.Instance);
            validator.Property(m => m.FuelConsumption).NotNaN();

            // Act
            var result = await validator.ValidateAsync(new CarModel { FuelConsumption = double.NaN });

            // Assert
            result.ShouldReport("FuelConsumption", "NotNaN");
        }

    }
}
