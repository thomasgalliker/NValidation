namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Fact]
        public async Task NoDuplicates_AcceptsDistinctEntries()
        {
            // Arrange
            var validator = new FeatureIdsNoDuplicatesValidator();
            var car = Cars.Car();
            car.FeatureIds = [1, 2, 3];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task NoDuplicates_RejectsRepeatedEntries()
        {
            // Arrange
            var validator = new FeatureIdsNoDuplicatesValidator();
            var car = Cars.Car();
            car.FeatureIds = [1, 2, 1];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task NoDuplicates_AcceptsAMissingCollection()
        {
            // Arrange
            var validator = new FeatureIdsNoDuplicatesValidator();
            var car = Cars.Car();
            car.FeatureIds = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue("an absent collection is left to NotNull");
        }

        [Fact]
        public async Task NoDuplicates_AcceptsAnEmptyCollection()
        {
            // Arrange
            var validator = new FeatureIdsNoDuplicatesValidator();
            var car = Cars.Car();
            car.FeatureIds = [];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// The rule is declared for any enumerable, so entries of a reference type in a concretely
        /// declared list are compared just as well.
        /// </summary>
        [Fact]
        public async Task NoDuplicates_WorksOnAConcreteListOfAnotherItemType()
        {
            // Arrange
            var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var validator = new PreviousOwnerIdsNoDuplicatesValidator();
            var car = Cars.Car();
            car.PreviousOwnerIds = [ownerId, ownerId];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)] // exactly the minimum
        [InlineData(4, false)]
        public async Task MinimumCount_RequiresEnoughEntries(int minimumCount, bool expectedToSucceed)
        {
            // Arrange
            var validator = new FeatureIdsMinimumCountValidator(minimumCount);
            var car = Cars.Car();
            car.FeatureIds = [1, 2, 3];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task MinimumCount_AcceptsAMissingCollection()
        {
            // Arrange
            var validator = new FeatureIdsMinimumCountValidator(1);
            var car = Cars.Car();
            car.FeatureIds = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue("an absent collection is left to NotEmpty");
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(3, true)] // exactly the maximum
        [InlineData(4, true)]
        public async Task MaximumCount_CapsTheNumberOfEntries(int maximumCount, bool expectedToSucceed)
        {
            // Arrange
            var validator = new FeatureIdsMaximumCountValidator(maximumCount);
            var car = Cars.Car();
            car.FeatureIds = [1, 2, 3];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task MaximumCount_AcceptsAMissingCollection()
        {
            // Arrange
            var validator = new FeatureIdsMaximumCountValidator(1);
            var car = Cars.Car();
            car.FeatureIds = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// A sequence which can only be walked once is not walked twice by one rule, so a count rule
        /// sees every entry rather than an exhausted iterator.
        /// </summary>
        [Fact]
        public async Task MaximumCount_OnALazySequence_SeesEveryEntry()
        {
            // Arrange
            var sequence = new CountingSequence([1, 2, 3]);
            var validator = new ServiceMileagesMaximumCountValidator(2);

            // Act
            var result = await validator.ValidateAsync(new Car { ServiceMileages = sequence });

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be(nameof(Car.ServiceMileages));
        }

        [Fact]
        public async Task MinimumCount_ReportsMinimumCount()
        {
            // Act
            var result = await new FeatureIdsMinimumCountValidator(3).ValidateForKeysAsync(new Car { FeatureIds = [1] });

            // Assert
            result.ShouldReport(nameof(Car.FeatureIds), ValidationMessageKeys.MinimumCount);
        }

        [Fact]
        public async Task MaximumCount_ReportsMaximumCount()
        {
            // Act
            var result = await new FeatureIdsMaximumCountValidator(1).ValidateForKeysAsync(new Car { FeatureIds = [1, 2, 3] });

            // Assert
            result.ShouldReport(nameof(Car.FeatureIds), ValidationMessageKeys.MaximumCount);
        }

        [Fact]
        public async Task NoDuplicates_ReportsNoDuplicates()
        {
            // Act
            var result = await new FeatureIdsNoDuplicatesValidator().ValidateForKeysAsync(new Car { FeatureIds = [1, 1] });

            // Assert
            result.ShouldReport(nameof(Car.FeatureIds), ValidationMessageKeys.NoDuplicates);
        }

    }
}
