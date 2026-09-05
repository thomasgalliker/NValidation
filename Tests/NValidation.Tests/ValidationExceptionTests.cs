namespace NValidation.Tests
{
    /// <summary>
    /// The exception a caller raises instead of inspecting a failed result. Its invariant — a validation
    /// failure always carries at least one error — is what lets a host treat "has errors" as the whole
    /// signal that a request failed validation.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationExceptionTests
    {
        [Fact]
        public void Constructor_FromValidationResult_CarriesTheErrorsGroupedByCode()
        {
            // Arrange
            var validationResult = ValidationResult.FromValidationErrors(
                new ValidationError("Vin", "The VIN is required."),
                new ValidationError("Vin", "The VIN must be exactly 17 characters long."));

            // Act
            var exception = new ValidationException(validationResult);

            // Assert
            exception.Errors.Should().ContainSingle();
            exception.Errors["Vin"].Should().BeEquivalentTo(
                "The VIN is required.",
                "The VIN must be exactly 17 characters long.");
        }

        [Fact]
        public void Constructor_BuildsTheMessageFromTheErrors()
        {
            // Arrange
            var validationResult = ValidationResult.FromValidationErrors(
                new ValidationError("Vin", "The VIN is required."),
                new ValidationError("Mileage", "The mileage must be greater than or equal to 0."));

            // Act
            var exception = new ValidationException(validationResult);

            // Assert
            exception.Message.Should().Contain("The VIN is required.");
            exception.Message.Should().Contain("The mileage must be greater than or equal to 0.");
        }

        [Fact]
        public void Constructor_WithEmptyErrorsDictionary_Throws()
        {
            // Arrange
            var emptyErrors = new Dictionary<string, string[]>();

            // Act
            var act = () => new ValidationException(emptyErrors);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithNullValidationResult_Throws()
        {
            // Act
            var act = () => new ValidationException((ValidationResult)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// The dictionary an exception is built from stays the caller's to change: what the exception
        /// reports must not change after it was thrown.
        /// </summary>
        [Fact]
        public void Constructor_DoesNotAliasTheCallersDictionary()
        {
            // Arrange
            var errors = new Dictionary<string, string[]> { ["Vin"] = ["original"] };
            var exception = new ValidationException(errors);

            // Act
            errors["Vin"] = ["changed afterwards"];
            errors["Mileage"] = ["added afterwards"];

            // Assert
            exception.Errors.Should().ContainSingle();
            exception.Errors["Vin"].Should().ContainSingle().Which.Should().Be("original");
        }
    }
}
