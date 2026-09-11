namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        /// <summary>
        /// A rule declared on the element itself names no property, so the message would otherwise open
        /// with the empty string — " must be greater than or equal to 0." The element is named by the
        /// very code the failure is reported under, which is what a reader has to match it to anyway.
        /// </summary>
        [Fact]
        public async Task Element_NamesTheElement_InTheMessage()
        {
            // Arrange
            var validator = new ServiceMileagesElementValidator();
            var car = Cars.Car();
            car.ServiceMileages = [3, -1];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            var error = result.Errors.Should().ContainSingle().Subject;
            error.Code.Should().Be("ServiceMileages[1]");
            error.Message.Should().Be("ServiceMileages[1] must be greater than or equal to 0.");
        }

        /// <summary>
        /// The same for a collection of a reference type. Declaring it at all is the point: the element
        /// chain used to be built for the element's non-nullable type, so every rule written on one
        /// warned at its call site — an error in a host which treats warnings as errors, as this
        /// repository does.
        /// </summary>
        [Fact]
        public async Task Element_OnACollectionOfReferenceTypes_NamesTheElement_InTheMessage()
        {
            // Arrange
            var validator = new ServiceInvoiceNumbersElementValidator();
            var car = Cars.Car();
            car.ServiceInvoiceNumbers = ["INV-01", "INV-0002"];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            var error = result.Errors.Should().ContainSingle().Subject;
            error.Code.Should().Be("ServiceInvoiceNumbers[1]");
            error.Message.Should().Be("ServiceInvoiceNumbers[1] must not exceed 6 characters.");
        }

        /// <summary>
        /// A display name declared on the element chain wins, as it does for any other property.
        /// </summary>
        [Fact]
        public async Task Element_WithADisplayName_NamesTheElementByIt()
        {
            // Arrange
            var validator = new ServiceMileagesElementDisplayNameValidator();
            var car = Cars.Car();
            car.ServiceMileages = [-1];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            var error = result.Errors.Should().ContainSingle().Subject;
            error.Code.Should().Be("ServiceMileages[0]");
            error.Message.Should().Be("Service mileage must be greater than or equal to 0.");
        }

        /// <summary>
        /// The identity a caller chose is what names the element, in the message as well as in the code.
        /// </summary>
        [Fact]
        public async Task Element_WithAnIndexer_NamesTheElementByTheIdentity()
        {
            // Arrange
            var validator = new ServiceMileagesIndexedElementValidator();
            var car = Cars.Car();
            car.ServiceMileages = [-1];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            var error = result.Errors.Should().ContainSingle().Subject;
            error.Code.Should().Be("ServiceMileages[entry-0]");
            error.Message.Should().Be("ServiceMileages[entry-0] must be greater than or equal to 0.");
        }
    }
}
