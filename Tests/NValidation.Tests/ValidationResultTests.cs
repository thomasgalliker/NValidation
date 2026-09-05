namespace NValidation.Tests
{
    /// <summary>
    /// The result type validators return: success or failure, and the code-grouped view of the errors
    /// which both the exception and a directly-reported failure are built from.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationResultTests
    {
        [Fact]
        public void Success_HasNoErrors()
        {
            // Arrange
            var result = ValidationResult.Success;

            // Act
            var succeeded = result.Succeeded;

            // Assert
            succeeded.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void FromValidationErrors_WithNoErrors_IsSuccess()
        {
            // Act
            var result = ValidationResult.FromValidationErrors();

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public void FromValidationErrors_WithErrors_IsFailure()
        {
            // Act
            var result = ValidationResult.FromValidationErrors(new ValidationError("Vin", "The VIN is required."));

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("Vin");
        }

        [Fact]
        public void FromValidationErrors_WithAnEnumerable_IsFailure()
        {
            // Arrange
            var errors = new List<ValidationError> { new("Vin", "The VIN is required.") };

            // Act
            var result = ValidationResult.FromValidationErrors(errors);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().ContainSingle();
        }

        [Fact]
        public void ToErrorsDictionary_GroupsTheMessagesByCode()
        {
            // Arrange
            var result = ValidationResult.FromValidationErrors(
                new ValidationError("SoldDate", "The sold date must not be earlier than 'Registration date'."),
                new ValidationError("Vin", "The VIN is required."),
                new ValidationError("Vin", "The VIN must be exactly 17 characters long."));

            // Act
            var errors = result.ToErrorsDictionary();

            // Assert
            errors.Should().HaveCount(2);
            errors["SoldDate"].Should().ContainSingle()
                .Which.Should().Be("The sold date must not be earlier than 'Registration date'.");
            errors["Vin"].Should().BeEquivalentTo(
                "The VIN is required.",
                "The VIN must be exactly 17 characters long.");
        }

        [Fact]
        public void ToErrorsDictionary_OnSuccess_IsEmpty()
        {
            // Act
            var errors = ValidationResult.Success.ToErrorsDictionary();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ThrowIfInvalid_OnSuccess_DoesNotThrow()
        {
            // Arrange
            var result = ValidationResult.Success;

            // Act
            var act = () => result.ThrowIfInvalid();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ThrowIfInvalid_OnFailure_ThrowsWithErrorsGroupedByCode()
        {
            // Arrange
            var result = ValidationResult.FromValidationErrors(
                new ValidationError("SoldDate", "The sold date must not be earlier than 'Registration date'."),
                new ValidationError("Vin", "The VIN is required."),
                new ValidationError("Vin", "The VIN must be exactly 17 characters long."));

            // Act
            var act = () => result.ThrowIfInvalid();

            // Assert
            var exception = act.Should().Throw<ValidationException>().Which;
            exception.Errors.Should().ContainKey("SoldDate");
            exception.Errors["SoldDate"].Should().ContainSingle()
                .Which.Should().Be("The sold date must not be earlier than 'Registration date'.");
            exception.Errors["Vin"].Should().BeEquivalentTo(
                "The VIN is required.",
                "The VIN must be exactly 17 characters long.");
        }

        /// <summary>
        /// The array a caller passes stays theirs to change, so the result has to hold a copy.
        /// </summary>
        [Fact]
        public void FromValidationErrors_DoesNotAliasTheCallersArray()
        {
            // Arrange
            var errors = new[] { new ValidationError("Vin", "original") };
            var result = ValidationResult.FromValidationErrors(errors);

            // Act
            errors[0] = new ValidationError("Vin", "changed afterwards");

            // Assert
            result.Errors.Should().ContainSingle().Which.Message.Should().Be("original");
        }
    }
}
