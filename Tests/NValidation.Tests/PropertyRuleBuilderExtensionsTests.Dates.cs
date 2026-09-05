namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Fact]
        public async Task InThePast_AcceptsAnEarlierDate()
        {
            // Arrange
            var timeProvider = TestTimeProvider.AtStartOf2026();
            var validator = new FirstRegistrationInThePastValidator(timeProvider);
            var car = Cars.Car();
            car.FirstRegistration = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task InThePast_RejectsALaterDate()
        {
            // Arrange
            var timeProvider = TestTimeProvider.AtStartOf2026();
            var validator = new FirstRegistrationInThePastValidator(timeProvider);
            var car = Cars.Car();
            car.FirstRegistration = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task InThePast_RejectsThisVeryInstant()
        {
            // Arrange
            var timeProvider = TestTimeProvider.AtStartOf2026();
            var validator = new FirstRegistrationInThePastValidator(timeProvider);
            var car = Cars.Car();
            car.FirstRegistration = timeProvider.GetUtcNow().UtcDateTime;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse("the past is strictly before now");
        }

        /// <summary>
        /// A date deserialized without an offset carries <see cref="DateTimeKind.Unspecified"/>. It is
        /// read as UTC, so the same payload gets the same verdict whatever time zone the host runs in.
        /// </summary>
        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Unspecified)]
        public async Task InThePast_JudgesAnUnspecifiedKind_AsUtc(DateTimeKind kind)
        {
            // Arrange
            var timeProvider = TestTimeProvider.AtStartOf2026();
            var validator = new FirstRegistrationInThePastValidator(timeProvider);
            var car = Cars.Car();

            // One minute before "now" in UTC: read as local time this would land on either side of now
            // depending on the machine's offset.
            car.FirstRegistration = DateTime.SpecifyKind(new DateTime(2025, 12, 31, 23, 59, 0), kind);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task InThePast_AcceptsAMissingDate()
        {
            // Arrange
            var validator = new SoldDateInThePastValidator(TestTimeProvider.AtStartOf2026());
            var car = Cars.Car();
            car.SoldDate = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue("a missing date is left to NotEmpty");
        }

        [Fact]
        public async Task InThePast_WithAnOffset_ComparesTheInstant()
        {
            // Arrange
            var timeProvider = TestTimeProvider.AtStartOf2026();
            var validator = new RegisteredAtInThePastValidator(timeProvider);
            var car = Cars.Car();

            // 00:30 at +02:00 is 22:30 the previous day in UTC, so this is in the past.
            car.RegisteredAt = new DateTimeOffset(2026, 1, 1, 0, 30, 0, TimeSpan.FromHours(2));

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task InTheFuture_AcceptsALaterDate()
        {
            // Arrange
            var validator = new WarrantyEndsOnInTheFutureValidator(TestTimeProvider.AtStartOf2026());
            var car = Cars.Car();
            car.WarrantyEndsOn = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task InTheFuture_RejectsAnEarlierDate()
        {
            // Arrange
            var validator = new WarrantyEndsOnInTheFutureValidator(TestTimeProvider.AtStartOf2026());
            var car = Cars.Car();
            car.WarrantyEndsOn = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task InTheFuture_AcceptsAMissingDate()
        {
            // Arrange
            var validator = new WarrantyEndsOnInTheFutureValidator(TestTimeProvider.AtStartOf2026());
            var car = Cars.Car();
            car.WarrantyEndsOn = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task InTheFuture_WithAnOffset_ComparesTheInstant()
        {
            // Arrange
            var validator = new NextServiceAtInTheFutureValidator(TestTimeProvider.AtStartOf2026());
            var car = Cars.Car();
            car.NextServiceAt = new DateTimeOffset(2027, 3, 1, 9, 0, 0, TimeSpan.FromHours(1));

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// The clock is a seam, so a rule that was in the past becomes one in the future simply by
        /// moving it.
        /// </summary>
        [Fact]
        public async Task InThePast_FollowsTheSuppliedClock()
        {
            // Arrange
            var timeProvider = TestTimeProvider.AtStartOf2026();
            var validator = new FirstRegistrationInThePastValidator(timeProvider);
            var car = Cars.Car();
            car.FirstRegistration = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            var beforeTheDate = await validator.ValidateAsync(car);

            // Act
            timeProvider.Advance(TimeSpan.FromDays(365));
            var afterTheDate = await validator.ValidateAsync(car);

            // Assert
            beforeTheDate.Succeeded.Should().BeFalse();
            afterTheDate.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task InThePast_ReportsInThePast()
        {
            // Arrange
            var car = new Car { SoldDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

            // Act
            var result = await new SoldDateInThePastValidator(TestTimeProvider.AtStartOf2026()).ValidateForKeysAsync(car);

            // Assert
            result.ShouldReport(nameof(Car.SoldDate), ValidationMessageKeys.InThePast);
        }

        [Fact]
        public async Task InTheFuture_ReportsInTheFuture()
        {
            // Arrange
            var car = new Car { NextServiceAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

            // Act
            var result = await new NextServiceAtInTheFutureValidator(TestTimeProvider.AtStartOf2026()).ValidateForKeysAsync(car);

            // Assert
            result.ShouldReport(nameof(Car.NextServiceAt), ValidationMessageKeys.InTheFuture);
        }

    }
}
