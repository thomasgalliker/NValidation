namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// The mapping from a validation failure onto the RFC7807 shape. Everything a host reads off the
    /// response — the status and the per-property messages under <c>errors</c> — is decided here.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationProblemDetailsExtensionsTests
    {
        [Fact]
        public void ToProblemDetails_ReportsBadRequest()
        {
            // Arrange
            var validationResult = ValidationResult.FromValidationErrors(new ValidationError("Vin", "The VIN is required."));

            // Act
            var problemDetails = validationResult.ToProblemDetails();

            // Assert
            problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public void ToProblemDetails_CarriesTheErrorsKeyedByCode()
        {
            // Arrange
            var validationResult = ValidationResult.FromValidationErrors(
                new ValidationError("Vin", "The VIN is required."),
                new ValidationError("Mileage", "The mileage must be greater than or equal to 0."));

            // Act
            var problemDetails = validationResult.ToProblemDetails();

            // Assert
            var errors = problemDetails.Extensions["errors"].Should().BeAssignableTo<IReadOnlyDictionary<string, string[]>>().Subject;
            errors.Should().HaveCount(2);
            errors["Vin"].Should().ContainSingle().Which.Should().Be("The VIN is required.");
            errors["Mileage"].Should().ContainSingle().Which.Should().Be("The mileage must be greater than or equal to 0.");
        }

        /// <summary>
        /// A property which broke several rules keeps every message, so a form can show them together.
        /// </summary>
        [Fact]
        public void ToProblemDetails_KeepsEveryMessageOfAProperty()
        {
            // Arrange
            var validationResult = ValidationResult.FromValidationErrors(
                new ValidationError("Vin", "The VIN is required."),
                new ValidationError("Vin", "The VIN must be exactly 17 characters long."));

            // Act
            var problemDetails = validationResult.ToProblemDetails();

            // Assert
            var errors = (IReadOnlyDictionary<string, string[]>)problemDetails.Extensions["errors"]!;
            errors["Vin"].Should().BeEquivalentTo(
                "The VIN is required.",
                "The VIN must be exactly 17 characters long.");
        }

        /// <summary>
        /// Title, Detail and Type are left unset on purpose, so a host's own problem details pipeline
        /// fills them in exactly as it does for every other error response.
        /// </summary>
        [Fact]
        public void ToProblemDetails_LeavesTheDescriptiveMembersToTheHost()
        {
            // Arrange
            var validationResult = ValidationResult.FromValidationErrors(new ValidationError("Vin", "The VIN is required."));

            // Act
            var problemDetails = validationResult.ToProblemDetails();

            // Assert
            problemDetails.Title.Should().BeNull();
            problemDetails.Detail.Should().BeNull();
            problemDetails.Type.Should().BeNull();
        }

        [Fact]
        public void ToProblemDetails_OnASuccessfulResult_Throws()
        {
            // Act
            var act = () => ValidationResult.Success.ToProblemDetails();

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToProblemDetails_FromAValidationException_CarriesTheSameErrors()
        {
            // Arrange
            var validationException = new ValidationException(
                ValidationResult.FromValidationErrors(new ValidationError("Vin", "The VIN is required.")));

            // Act
            var problemDetails = validationException.ToProblemDetails();

            // Assert
            problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
            var errors = (IReadOnlyDictionary<string, IReadOnlyList<string>>)problemDetails.Extensions["errors"]!;
            errors["Vin"].Should().ContainSingle().Which.Should().Be("The VIN is required.");
        }
    }
}
