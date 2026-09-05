namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)] // the bound itself is excluded
        [InlineData(1, true)]
        public async Task GreaterThan_ExcludesTheBound(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageGreaterThanValidator(0);
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)] // the bound itself is included
        [InlineData(1, true)]
        public async Task GreaterThanOrEqualTo_IncludesTheBound(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageGreaterThanOrEqualToValidator(0);
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(99, true)]
        [InlineData(100, false)] // the bound itself is excluded
        [InlineData(101, false)]
        public async Task LessThan_ExcludesTheBound(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageLessThanValidator(100);
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(99, true)]
        [InlineData(100, true)] // the bound itself is included
        [InlineData(101, false)]
        public async Task LessThanOrEqualTo_IncludesTheBound(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageLessThanOrEqualToValidator(100);
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(-5, false)]
        [InlineData(-4, true)]
        public async Task GreaterThan_WorksWithANegativeBound(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageGreaterThanValidator(-5);
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(0.01, true)]
        public async Task GreaterThan_WithADecimal_ExcludesTheBound(double purchasePrice, bool expectedToSucceed)
        {
            // Arrange
            var validator = new PurchasePriceGreaterThanValidator(0m);
            var car = Cars.Car();
            car.PurchasePrice = (decimal)purchasePrice;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// The comparison rules are written once over <see cref="IComparable{T}"/>, so a type the
        /// library never mentions by name works just as well.
        /// </summary>
        [Theory]
        [InlineData(1_000L, false)]
        [InlineData(1_001L, true)]
        public async Task GreaterThan_WorksWithALong(long unitsProduced, bool expectedToSucceed)
        {
            // Arrange
            var validator = new UnitsProducedGreaterThanValidator(1_000L);
            var carModel = Cars.CarModel();
            carModel.UnitsProduced = unitsProduced;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <inheritdoc cref="GreaterThan_WorksWithALong" path="/summary"/>
        [Theory]
        [InlineData(24, true)]
        [InlineData(25, false)]
        public async Task LessThanOrEqualTo_WorksWithATimeSpan(int hours, bool expectedToSucceed)
        {
            // Arrange
            var validator = new ServiceIntervalLessThanOrEqualToValidator(TimeSpan.FromHours(24));
            var car = Cars.Car();
            car.ServiceInterval = TimeSpan.FromHours(hours);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <inheritdoc cref="GreaterThan_WorksWithALong" path="/summary"/>
        [Theory]
        [InlineData(2019, false)]
        [InlineData(2020, true)] // the bound itself
        [InlineData(2021, true)]
        public async Task GreaterThanOrEqualTo_WorksWithADateTimeOffset(int year, bool expectedToSucceed)
        {
            // Arrange
            var bound = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var validator = new RegisteredAtGreaterThanOrEqualToValidator(bound);
            var car = Cars.Car();
            car.RegisteredAt = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(0d, false)]
        [InlineData(0.01d, true)]
        public async Task GreaterThan_WithANullableProperty_JudgesOnlyAValueThatIsThere(double? tradeInValue, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TradeInValueGreaterThanValidator(0m);
            var car = Cars.Car();
            car.TradeInValue = tradeInValue == null ? null : (decimal)tradeInValue.Value;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(199f, true)]
        [InlineData(200f, false)]
        public async Task LessThan_WithANullableProperty_JudgesOnlyAValueThatIsThere(float? topSpeed, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TopSpeedLessThanValidator(200f);
            var carModel = Cars.CarModel();
            carModel.TopSpeed = topSpeed;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        // --- against another property ---------------------------------------------------------

        [Theory]
        [InlineData(-1, false)] // sold before it was registered
        [InlineData(0, true)] // the same day
        [InlineData(1, true)]
        public async Task GreaterThanOrEqualTo_ComparesAgainstAnotherProperty(int daysAfterRegistration, bool expectedToSucceed)
        {
            // Arrange
            var validator = new SoldDateAfterFirstRegistrationValidator();
            var car = Cars.Car();
            car.SoldDate = car.FirstRegistration.AddDays(daysAfterRegistration);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task GreaterThanOrEqualTo_AgainstAnotherProperty_SkipsAMissingValue()
        {
            // Arrange
            var validator = new SoldDateAfterFirstRegistrationValidator();
            var car = Cars.Car();
            car.SoldDate = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue("a missing date is left to NotEmpty");
        }

        /// <summary>
        /// The other side of the comparison may be the nullable one, in which case there is nothing to
        /// compare against and the rule has nothing to say.
        /// </summary>
        [Fact]
        public async Task LessThanOrEqualTo_AgainstAMissingOtherProperty_Passes()
        {
            // Arrange
            var validator = new FirstRegistrationBeforeSoldDateValidator();
            var car = Cars.Car();
            car.SoldDate = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task LessThanOrEqualTo_AgainstAnOtherProperty_ReportsTheBrokenComparison()
        {
            // Arrange
            var validator = new FirstRegistrationBeforeSoldDateValidator();
            var car = Cars.Car();
            car.SoldDate = car.FirstRegistration.AddDays(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be(nameof(Car.FirstRegistration));
        }

        [Fact]
        public async Task GreaterThan_WithBothSidesNullable_PassesWhenEitherIsMissing()
        {
            // Arrange
            var validator = new WarrantyEndsOnAfterSoldDateValidator();
            var car = Cars.Car();
            car.WarrantyEndsOn = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task GreaterThan_WithBothSidesNullable_ComparesWhenBothAreThere()
        {
            // Arrange
            var validator = new WarrantyEndsOnAfterSoldDateValidator();
            var car = Cars.Car();
            car.WarrantyEndsOn = car.SoldDate!.Value.AddDays(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Theory]
        [InlineData(99_999, true)]
        [InlineData(100_000, true)] // exactly the limit
        [InlineData(100_001, false)]
        public async Task LessThanOrEqualTo_WithNeitherSideNullable_ComparesTheTwoProperties(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageWithinWarrantyValidator();
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// The message names the property it was compared against, so the reader knows which two fields
        /// disagree.
        /// </summary>
        [Fact]
        public async Task GreaterThanOrEqualTo_NamesTheOtherProperty_ByItsCode()
        {
            // Arrange
            var validator = new SoldDateAfterFirstRegistrationValidator();
            var car = Cars.Car();
            car.SoldDate = car.FirstRegistration.AddDays(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should()
                .Be("SoldDate must be greater than or equal to FirstRegistration.");
        }

        [Fact]
        public async Task GreaterThanOrEqualTo_NamesTheOtherProperty_ByItsDisplayName()
        {
            // Arrange
            var validator = new SoldDateAfterNamedFirstRegistrationValidator();
            var car = Cars.Car();
            car.SoldDate = car.FirstRegistration.AddDays(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should()
                .Be("SoldDate must be greater than or equal to the registration date.");
        }

        // --- ranges ------------------------------------------------------------------------------

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)] // the lower bound is included by default
        [InlineData(5, true)]
        [InlineData(9, true)] // and so is the upper one
        [InlineData(10, false)]
        public async Task Between_IncludesBothBoundsByDefault(int seatCount, bool expectedToSucceed)
        {
            // Arrange
            var validator = new SeatCountBetweenValidator(1, 9);
            var carModel = Cars.CarModel();
            carModel.SeatCount = seatCount;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(1, false)] // the lower bound is excluded
        [InlineData(5, true)]
        [InlineData(9, false)] // and so is the upper one
        public async Task Between_ExcludesBothBounds_WhenAskedTo(int seatCount, bool expectedToSucceed)
        {
            // Arrange
            var validator = new SeatCountBetweenExclusiveValidator(1, 9, inclusive: false);
            var carModel = Cars.CarModel();
            carModel.SeatCount = seatCount;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(1, true, true, true)] // the lower bound is allowed when included
        [InlineData(1, false, true, false)] // and rejected when excluded
        [InlineData(9, true, true, true)] // the upper bound is allowed when included
        [InlineData(9, true, false, false)] // and rejected when excluded
        [InlineData(0, true, true, false)] // below either way
        [InlineData(10, true, true, false)] // above either way
        [InlineData(5, false, false, true)] // strictly inside passes whatever the bounds do
        public async Task Between_AppliesEachBoundOnItsOwn(int seatCount, bool inclusiveFrom, bool inclusiveTo, bool expectedToSucceed)
        {
            // Arrange
            var validator = new SeatCountBetweenBoundsValidator(1, 9, inclusiveFrom, inclusiveTo);
            var carModel = Cars.CarModel();
            carModel.SeatCount = seatCount;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(0d, false)]
        [InlineData(500d, true)]
        public async Task Between_WithANullableProperty_JudgesOnlyAValueThatIsThere(double? tradeInValue, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TradeInValueBetweenValidator(1m, 999m);
            var car = Cars.Car();
            car.TradeInValue = tradeInValue == null ? null : (decimal)tradeInValue.Value;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public void Between_WithALowerBoundAboveTheUpperOne_Throws()
        {
            // Act
            var act = () => new SeatCountBetweenValidator(9, 1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // --- equality ----------------------------------------------------------------------------

        [Theory]
        [InlineData(42, true)]
        [InlineData(43, false)]
        public async Task EqualTo_RequiresTheValue(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageEqualToValidator(42);
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(42, false)]
        [InlineData(43, true)]
        public async Task NotEqualTo_RejectsTheValue(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageNotEqualToValidator(42);
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(42d, true)]
        [InlineData(43d, false)]
        public async Task EqualTo_WithANullableProperty_JudgesOnlyAValueThatIsThere(double? tradeInValue, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TradeInValueEqualToValidator(42m);
            var car = Cars.Car();
            car.TradeInValue = tradeInValue == null ? null : (decimal)tradeInValue.Value;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("abc", StringComparison.Ordinal, true)]
        [InlineData("ABC", StringComparison.Ordinal, false)]
        [InlineData("ABC", StringComparison.OrdinalIgnoreCase, true)]
        public async Task EqualTo_WithText_HonoursTheComparison(string vin, StringComparison comparison, bool expectedToSucceed)
        {
            // Arrange
            var validator = new VinEqualToValidator("abc", comparison);
            var car = Cars.Car();
            car.Vin = vin;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(100_000, true)]
        [InlineData(99_999, false)]
        public async Task EqualTo_ComparesAgainstAnotherProperty(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new MileageEqualToWarrantyLimitValidator();
            var car = Cars.Car();
            car.Mileage = mileage;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task GreaterThan_ReportsGreaterThan()
        {
            // Act
            var result = await new MileageGreaterThanValidator(10).ValidateForKeysAsync(new Car { Mileage = 5 });

            // Assert
            result.ShouldReport(nameof(Car.Mileage), ValidationMessageKeys.GreaterThan);
        }

        [Fact]
        public async Task GreaterThanOrEqualTo_ReportsGreaterThanOrEqualTo()
        {
            // Act
            var result = await new MileageGreaterThanOrEqualToValidator(10).ValidateForKeysAsync(new Car { Mileage = 5 });

            // Assert
            result.ShouldReport(nameof(Car.Mileage), ValidationMessageKeys.GreaterThanOrEqualTo);
        }

        [Fact]
        public async Task LessThan_ReportsLessThan()
        {
            // Act
            var result = await new MileageLessThanValidator(10).ValidateForKeysAsync(new Car { Mileage = 20 });

            // Assert
            result.ShouldReport(nameof(Car.Mileage), ValidationMessageKeys.LessThan);
        }

        [Fact]
        public async Task LessThanOrEqualTo_ReportsLessThanOrEqualTo()
        {
            // Act
            var result = await new MileageLessThanOrEqualToValidator(10).ValidateForKeysAsync(new Car { Mileage = 20 });

            // Assert
            result.ShouldReport(nameof(Car.Mileage), ValidationMessageKeys.LessThanOrEqualTo);
        }

        [Fact]
        public async Task Between_ReportsBetween()
        {
            // Act
            var result = await new SeatCountBetweenValidator(2, 5).ValidateForKeysAsync(new CarModel { SeatCount = 9 });

            // Assert
            result.ShouldReport(nameof(CarModel.SeatCount), ValidationMessageKeys.Between);
        }

        [Fact]
        public async Task EqualTo_ReportsEqualTo()
        {
            // Act
            var result = await new MileageEqualToValidator(10).ValidateForKeysAsync(new Car { Mileage = 20 });

            // Assert
            result.ShouldReport(nameof(Car.Mileage), ValidationMessageKeys.EqualTo);
        }

        [Fact]
        public async Task NotEqualTo_ReportsNotEqualTo()
        {
            // Act
            var result = await new MileageNotEqualToValidator(10).ValidateForKeysAsync(new Car { Mileage = 10 });

            // Assert
            result.ShouldReport(nameof(Car.Mileage), ValidationMessageKeys.NotEqualTo);
        }

        [Fact]
        public async Task GreaterThanOrEqualTo_AgainstAnotherProperty_ReportsGreaterThanOrEqualToOtherProperty()
        {
            // Arrange
            var car = new Car
            {
                FirstRegistration = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                SoldDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            // Act
            var result = await new SoldDateAfterFirstRegistrationValidator().ValidateForKeysAsync(car);

            // Assert
            result.ShouldReport(nameof(Car.SoldDate), ValidationMessageKeys.GreaterThanOrEqualToOtherProperty);
        }

        [Fact]
        public async Task LessThanOrEqualTo_AgainstAnotherProperty_ReportsLessThanOrEqualToOtherProperty()
        {
            // Arrange
            var car = new Car
            {
                FirstRegistration = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                SoldDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            // Act
            var result = await new FirstRegistrationBeforeSoldDateValidator().ValidateForKeysAsync(car);

            // Assert
            result.ShouldReport(nameof(Car.FirstRegistration), ValidationMessageKeys.LessThanOrEqualToOtherProperty);
        }

    }
}
