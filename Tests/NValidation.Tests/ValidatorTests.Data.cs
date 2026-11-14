using NValidation.Internals;

namespace NValidation.Tests
{
    public partial class ValidatorTests
    {
        /// <summary>
        /// What the registration hands a validator it constructed is a rung of the ladder like any other,
        /// data included.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithDataHandedByTheRegistration_HandsItToTheRules()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage);

            ((IValidationRegistrationTarget)validator).Options =
                new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 250_000 });

            // Assert
            result.ShouldReport("Mileage", "Mileage is not valid.");
        }

        [Fact]
        public async Task ValidateAsync_WithDataHandedByTheRegistration_IsReplacedByTheDataOfTheCall()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage);

            ((IValidationRegistrationTarget)validator).Options =
                new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            var options = new NValidationOptions { ValidationData = [new ListingPolicy(300_000, false)] };

            // Act
            var result = await validator.ValidateAsync(new Car { Mileage = 250_000 }, options);

            // Assert
            result.Errors.Should().BeEmpty();
        }
    }
}
