namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Fact]
        public async Task NoDuplicates_AcceptsDistinctEntries()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds).NoDuplicates();

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds).NoDuplicates();

            var car = Cars.Car();
            car.FeatureIds = [1, 2, 1];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("FeatureIds", "FeatureIds must not contain duplicate entries.");
        }

        [Fact]
        public async Task NoDuplicates_AcceptsAMissingCollection()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds).NoDuplicates();

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds).NoDuplicates();

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

            var validator = new TestValidator<Car>();
            validator.Property(c => c.PreviousOwnerIds).NoDuplicates();

            var car = Cars.Car();
            car.PreviousOwnerIds = [ownerId, ownerId];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("PreviousOwnerIds", "PreviousOwnerIds must not contain duplicate entries.");
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)] // exactly the minimum
        [InlineData(4, false)]
        public async Task MinimumCount_RequiresEnoughEntries(int minimumCount, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds).MinimumCount(minimumCount);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds).MinimumCount(1);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds).MaximumCount(maximumCount);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds).MaximumCount(1);

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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceMileages).MaximumCount(2);

            var sequence = new CountingSequence([1, 2, 3]);

            // Act
            var result = await validator.ValidateAsync(new Car { ServiceMileages = sequence });

            // Assert
            result.ShouldReport("ServiceMileages", "ServiceMileages must not contain more than 2 entries.");
        }

        [Fact]
        public async Task MinimumCount_ReportsMinimumCount()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.FeatureIds).MinimumCount(3);

            // Act
            var result = await validator.ValidateAsync(new Car { FeatureIds = [1] });

            // Assert
            result.ShouldReport("FeatureIds", "MinimumCount");
        }

        [Fact]
        public async Task MaximumCount_ReportsMaximumCount()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.FeatureIds).MaximumCount(1);

            // Act
            var result = await validator.ValidateAsync(new Car { FeatureIds = [1, 2, 3] });

            // Assert
            result.ShouldReport("FeatureIds", "MaximumCount");
        }

        [Fact]
        public async Task NoDuplicates_ReportsNoDuplicates()
        {
            // Arrange
            var validator = new TestValidator<Car>(MessageKeyProvider.Instance);
            validator.Property(c => c.FeatureIds).NoDuplicates();

            // Act
            var result = await validator.ValidateAsync(new Car { FeatureIds = [1, 1] });

            // Assert
            result.ShouldReport("FeatureIds", "NoDuplicates");
        }

    }
}
