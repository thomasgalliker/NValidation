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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).GreaterThan(0);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).GreaterThanOrEqualTo(0);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).LessThan(100);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).LessThanOrEqualTo(100);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).GreaterThan(-5);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.PurchasePrice).GreaterThan(0m);

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
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.UnitsProduced).GreaterThan(1_000L);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceInterval).LessThanOrEqualTo(TimeSpan.FromHours(24));

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.RegisteredAt).GreaterThanOrEqualTo(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.TradeInValue).GreaterThan(0m);

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
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.TopSpeed).LessThan(200f);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FirstRegistration).LessThanOrEqualTo(c => c.SoldDate);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FirstRegistration).LessThanOrEqualTo(c => c.SoldDate);

            var car = Cars.Car();
            car.SoldDate = car.FirstRegistration.AddDays(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("FirstRegistration", "FirstRegistration must be less than or equal to SoldDate.");
        }

        [Fact]
        public async Task GreaterThan_WithBothSidesNullable_PassesWhenEitherIsMissing()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.WarrantyEndsOn).GreaterThan(c => c.SoldDate);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.WarrantyEndsOn).GreaterThan(c => c.SoldDate);

            var car = Cars.Car();
            car.WarrantyEndsOn = car.SoldDate!.Value.AddDays(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("WarrantyEndsOn", "WarrantyEndsOn must be greater than SoldDate.");
        }

        [Theory]
        [InlineData(99_999, true)]
        [InlineData(100_000, true)] // exactly the limit
        [InlineData(100_001, false)]
        public async Task LessThanOrEqualTo_WithNeitherSideNullable_ComparesTheTwoProperties(int mileage, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).LessThanOrEqualTo(c => c.WarrantyMileageLimit);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);

            var car = Cars.Car();
            car.SoldDate = car.FirstRegistration.AddDays(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("SoldDate", "SoldDate must be greater than or equal to FirstRegistration.");
        }

        [Fact]
        public async Task GreaterThanOrEqualTo_NamesTheOtherProperty_ByItsDisplayName()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);
            validator.Property(c => c.FirstRegistration).WithDisplayName("the registration date");

            var car = Cars.Car();
            car.SoldDate = car.FirstRegistration.AddDays(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("SoldDate", "SoldDate must be greater than or equal to the registration date.");
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
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.SeatCount).Between(1, 9);

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
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.SeatCount).Between(1, 9, inclusive: false);

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
            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.SeatCount).Between(1, 9, inclusiveFrom, inclusiveTo);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.TradeInValue).Between(1m, 999m);

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
            // Arrange
            var validator = new TestValidator<CarModel>();

            // Act
            var act = () => validator.Property(m => m.SeatCount).Between(9, 1);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).EqualTo(42);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).NotEqualTo(42);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.TradeInValue).EqualTo(42m);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).EqualTo("abc", comparison);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).EqualTo(c => c.WarrantyMileageLimit);

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
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.Mileage).GreaterThan(10);

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 5 });

            // Assert
            result.ShouldReport("Mileage", "GreaterThan");
        }

        [Fact]
        public async Task GreaterThanOrEqualTo_ReportsGreaterThanOrEqualTo()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.Mileage).GreaterThanOrEqualTo(10);

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 5 });

            // Assert
            result.ShouldReport("Mileage", "GreaterThanOrEqualTo");
        }

        [Fact]
        public async Task LessThan_ReportsLessThan()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.Mileage).LessThan(10);

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 20 });

            // Assert
            result.ShouldReport("Mileage", "LessThan");
        }

        [Fact]
        public async Task LessThanOrEqualTo_ReportsLessThanOrEqualTo()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.Mileage).LessThanOrEqualTo(10);

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 20 });

            // Assert
            result.ShouldReport("Mileage", "LessThanOrEqualTo");
        }

        [Fact]
        public async Task Between_ReportsBetween()
        {
            // Arrange
            var validator = new TestValidator<CarModel>(MessageKeyProvider.Instance);
            validator.Property(m => m.SeatCount).Between(2, 5);

            // Act
            var result = await validator.ValidateAsync(new CarModel { SeatCount = 9 });

            // Assert
            result.ShouldReport("SeatCount", "Between");
        }

        [Fact]
        public async Task EqualTo_ReportsEqualTo()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.Mileage).EqualTo(10);

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 20 });

            // Assert
            result.ShouldReport("Mileage", "EqualTo");
        }

        [Fact]
        public async Task NotEqualTo_ReportsNotEqualTo()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.Mileage).NotEqualTo(10);

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 10 });

            // Assert
            result.ShouldReport("Mileage", "NotEqualTo");
        }

        [Fact]
        public async Task GreaterThanOrEqualTo_AgainstAnotherProperty_ReportsGreaterThanOrEqualToOtherProperty()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);

            var car = new Car
            {
                FirstRegistration = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                SoldDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("SoldDate", "GreaterThanOrEqualToOtherProperty");
        }

        [Fact]
        public async Task LessThanOrEqualTo_AgainstAnotherProperty_ReportsLessThanOrEqualToOtherProperty()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.FirstRegistration).LessThanOrEqualTo(c => c.SoldDate);

            var car = new Car
            {
                FirstRegistration = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                SoldDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("FirstRegistration", "LessThanOrEqualToOtherProperty");
        }

    }
}
