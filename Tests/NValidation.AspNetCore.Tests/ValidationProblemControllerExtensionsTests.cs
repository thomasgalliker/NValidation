namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// The non-throwing path: an action returns the failure as an <see cref="ObjectResult"/>, which a
    /// host's result pipeline then treats like any other error result.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationProblemControllerExtensionsTests
    {
        [Fact]
        public void ValidationProblem_ReturnsABadRequestObjectResult()
        {
            // Arrange
            var controller = new TestController();
            var validationResult = ValidationResult.FromValidationErrors(new ValidationError("Vin", "The VIN is required."));

            // Act
            var result = controller.CreateValidationProblem(validationResult);

            // Assert
            result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
            problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);

            var errors = (IReadOnlyDictionary<string, string[]>)problemDetails.Extensions["errors"]!;
            errors["Vin"].Should().ContainSingle().Which.Should().Be("The VIN is required.");
        }

        [Fact]
        public void ValidationProblem_OnASuccessfulResult_Throws()
        {
            // Arrange
            var controller = new TestController();

            // Act
            var act = () => controller.CreateValidationProblem(ValidationResult.Success);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// The extension is reached through a controller, because that is how an action calls it.
        /// </summary>
        private sealed class TestController : ControllerBase
        {
            public ObjectResult CreateValidationProblem(ValidationResult validationResult)
            {
                return this.ValidationProblem(validationResult);
            }
        }
    }
}
