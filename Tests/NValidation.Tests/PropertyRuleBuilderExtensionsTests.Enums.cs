namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Theory]
        [InlineData(CarCondition.Unknown, true)] // a defined member, even if it means "none"
        [InlineData(CarCondition.Used, true)]
        [InlineData((CarCondition)99, false)]
        public async Task IsInEnum_RejectsValuesOutsideTheEnum(CarCondition condition, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Condition).IsInEnum();

            var car = Cars.Car();
            car.Condition = condition;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task IsInEnum_RejectsANegativeValue()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Condition).IsInEnum();

            var car = Cars.Car();
            car.Condition = (CarCondition)(-1);

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Condition", "Condition has an invalid value.");
        }

        /// <summary>
        /// A combination of declared flags is a legitimate value even though it is not itself a declared
        /// member, so it has to be accepted.
        /// </summary>
        [Fact]
        public async Task IsInEnum_AcceptsACombinationOfFlags()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Equipment).IsInEnum();

            var car = Cars.Car();
            car.Equipment = CarEquipment.AirConditioning | CarEquipment.TowBar | CarEquipment.SunRoof;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task IsInEnum_AcceptsTheEmptyFlagCombination()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Equipment).IsInEnum();

            var car = Cars.Car();
            car.Equipment = CarEquipment.None;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// A bit no member declares is not a combination of anything, so it is still rejected.
        /// </summary>
        [Fact]
        public async Task IsInEnum_RejectsAFlagNoMemberDeclares()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Equipment).IsInEnum();

            var car = Cars.Car();
            car.Equipment = CarEquipment.AirConditioning | (CarEquipment)64;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("Equipment", "Equipment has an invalid value.");
        }

        /// <summary>
        /// A negative member is legal, and the flags support has to read it as the bits it sets rather
        /// than converting it — converting a negative value to an unsigned one throws.
        /// </summary>
        [Theory]
        [InlineData(GearDirection.Reverse, true)]
        [InlineData(GearDirection.Neutral, true)]
        [InlineData(GearDirection.Forward, true)]
        [InlineData((GearDirection)99, false)]
        public async Task IsInEnum_OnAnEnumWithANegativeMember_JudgesItWithoutThrowing(GearDirection gearDirection, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.GearDirection).IsInEnum();

            // Act
            var result = await validator.ValidateAsync(new Car { GearDirection = gearDirection });

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task IsInEnum_ReportsIsInEnum()
        {
            // Arrange
            var validator = new TestValidator<Car>(ErrorCodeProvider.Instance);
            validator.Property(c => c.Condition).IsInEnum();

            // Act
            var result = await validator.ValidateAsync(new Car { Condition = (CarCondition)99 });

            // Assert
            result.ShouldReport("Condition", "IsInEnum");
        }

    }
}
