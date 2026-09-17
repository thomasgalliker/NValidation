namespace NValidation.Tests
{
    /// <summary>
    /// The behaviour the library promises about things that are absent, about what a rule's arguments
    /// may be, and about what a chain of collection rules costs. Each of these is a claim the
    /// documentation makes, pinned so it cannot quietly stop being true.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ReviewFixesTests
    {
        /// <summary>
        /// The compared property is reached through <c>Model</c>, which the payload omitted. The
        /// validated property is on the root, so the chain's own guard does not apply — without a guard
        /// on the compared side this dereferenced nothing and turned a bad request into a 500.
        /// </summary>
        [Fact]
        public async Task Comparison_WithTheComparedPropertyBehindAMissingObject_Passes()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).LessThanOrEqualTo(c => c.Model!.WarrantyMileageCap);

            var car = Cars.Car();
            car.Model = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue("nothing to compare against passes, as an absent value does");
        }

        [Fact]
        public async Task Comparison_WithTheComparedPropertyReachable_StillReports()
        {
            // Arrange
            var validator = new TestValidator<Car>(ErrorCodeProvider.Instance);
            validator.Property(c => c.Mileage).LessThanOrEqualTo(c => c.Model!.WarrantyMileageCap);

            var car = Cars.Car();
            car.Model!.WarrantyMileageCap = 10_000;
            car.Mileage = 42_000;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Mileage", "LessThanOrEqualToOtherProperty");
        }

        [Fact]
        public async Task NotEqualTo_WithAnotherProperty_ReportsWhenTheyMatch()
        {
            // Arrange
            var validator = new TestValidator<Car>(ErrorCodeProvider.Instance);
            validator.Property(c => c.Mileage).NotEqualTo(c => c.WarrantyMileageLimit);

            var car = Cars.Car();
            car.Mileage = car.WarrantyMileageLimit;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Mileage", "NotEqualToOtherProperty");
        }

        [Fact]
        public async Task NotEqualTo_WithAnotherProperty_PassesWhenTheyDiffer()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).NotEqualTo(c => c.WarrantyMileageLimit);

            var car = Cars.Car();
            car.Mileage = car.WarrantyMileageLimit - 1;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Theory]
        [InlineData("ZH 1", "ZH 1", false)]
        [InlineData("ZH 1", "BE 2", true)]
        [InlineData(null, "BE 2", true)] // absent is left to NotEmpty
        public async Task NotEqualTo_WithAnotherTextProperty_JudgesOnlyAValueThatIsThere(
            string? plate, string? previousPlate, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.RegistrationPlate).NotEqualTo(c => c.PreviousRegistrationPlate);

            var car = Cars.Car();
            car.RegistrationPlate = plate;
            car.PreviousRegistrationPlate = previousPlate;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task EqualTo_WithAnotherNullableProperty_ReportsWhenTheyDiffer()
        {
            // Arrange
            var validator = new TestValidator<Car>(ErrorCodeProvider.Instance);
            validator.Property(c => c.SoldDate).EqualTo(c => c.WarrantyEndsOn);

            var car = Cars.Car();
            car.SoldDate = new DateTime(2023, 9, 15, 0, 0, 0, DateTimeKind.Utc);
            car.WarrantyEndsOn = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("SoldDate", "EqualToOtherProperty");
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(15_000, true)]
        [InlineData(15_001, false)]
        public async Task MultipleOf_WithANullableWholeNumber_JudgesOnlyAValueThatIsThere(int? serviceIntervalKm, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceIntervalKm).MultipleOf(1_000);

            var car = Cars.Car();
            car.ServiceIntervalKm = serviceIntervalKm;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotNull
        [InlineData(CarCondition.Used, true)]
        [InlineData((CarCondition)99, false)]
        public async Task IsInEnum_WithANullableEnum_JudgesOnlyAValueThatIsThere(CarCondition? intakeCondition, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.IntakeCondition).IsInEnum();

            var car = Cars.Car();
            car.IntakeCondition = intakeCondition;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// A missing value passes, as it does for every other rule about a string, even though
        /// <c>string.Equals(null, "x")</c> is false.
        /// </summary>
        [Theory]
        [InlineData(null, true)]
        [InlineData("ZH 100 200", true)]
        [InlineData("BE 300 400", false)]
        public async Task EqualTo_WithText_JudgesOnlyAValueThatIsThere(string? plate, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.RegistrationPlate).EqualTo("ZH 100 200");

            var car = Cars.Car();
            car.RegistrationPlate = plate;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("BE 300 400", true)]
        [InlineData("ZH 100 200", false)]
        public async Task NotEqualTo_WithText_JudgesOnlyAValueThatIsThere(string? plate, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.RegistrationPlate).NotEqualTo("ZH 100 200");

            var car = Cars.Car();
            car.RegistrationPlate = plate;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// A chain of collection rules asks one question each, so it walks the sequence once per rule.
        /// The count is pinned here so the documented cost and the behaviour cannot drift apart.
        /// </summary>
        [Fact]
        public async Task CollectionRules_WalkTheSequence_OncePerRule()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceMileages)
                .WithValidationBehavior(ValidationBehavior.All)
                .NotEmpty()
                .MinimumCount(1)
                .MaximumCount(2);

            var sequence = new CountingSequence([1, 2]);
            var car = Cars.Car();
            car.ServiceMileages = sequence;

            // Act
            await validator.ValidateAsync(car);

            // Assert
            sequence.Passes.Should().Be(3, "NotEmpty, MinimumCount and MaximumCount each ask once");
        }

        /// <summary>
        /// One entry past the cap settles the question, so the rest of the sequence is not walked.
        /// </summary>
        [Fact]
        public async Task MaximumCount_StopsAtTheEntryThatBustsTheCap()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceMileages).MaximumCount(2);

            var sequence = new CountingSequence([1, 2, 3, 4, 5]);
            var car = Cars.Car();
            car.ServiceMileages = sequence;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceMileages", "ServiceMileages must not contain more than 2 entries.");
            sequence.Enumerated.Should().Be(3, "the third entry is what busts a cap of two");
        }

        [Fact]
        public void WithPropertyName_WithABlankName_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).WithPropertyName("  ");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithDisplayName_WithABlankName_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).WithDisplayName("  ");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// A rule whose argument cannot describe anything is a mistake at the call site, not a rule that
        /// quietly fails every value.
        /// </summary>
        [Fact]
        public void RuleArguments_WhichCannotDescribeAnything_AreRefusedWhereTheyAreWritten()
        {
            // Arrange
            var builder = default(PropertyRuleBuilder<Car, string?>);
            var collection = default(PropertyRuleBuilder<Car, List<Guid>?>);
            var number = default(PropertyRuleBuilder<Car, int>);

            // Act
            var acts = new Action[]
            {
                () => builder.MinimumLength(-1),
                () => builder.MaximumLength(-1),
                () => builder.Length(-1),
                () => builder.Length(-1, 5),
                () => collection.MinimumCount(-1),
                () => collection.MaximumCount(-1),
                () => number.MultipleOf(0),
                () => number.MultipleOf(-1),
            };

            // Assert
            acts.Should().AllSatisfy(act => act.Should().Throw<ArgumentOutOfRangeException>());
        }
    }
}
