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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceMileages)
                .ForEach(mileage => mileage.Element().GreaterThanOrEqualTo(0));

            var car = Cars.Car();
            car.ServiceMileages = [3, -1];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceMileages[1]", "ServiceMileages[1] must be greater than or equal to 0.");
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
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceInvoiceNumbers).ForEach(number => number.Element().MaximumLength(6));

            var car = Cars.Car();
            car.ServiceInvoiceNumbers = ["INV-01", "INV-0002"];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceInvoiceNumbers[1]", "ServiceInvoiceNumbers[1] must not exceed 6 characters.");
        }

        /// <summary>
        /// A display name declared on the element chain wins, as it does for any other property.
        /// </summary>
        [Fact]
        public async Task Element_WithADisplayName_NamesTheElementByIt()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceMileages)
                .ForEach(mileage => mileage.Element().WithDisplayName("Service mileage").GreaterThanOrEqualTo(0));

            var car = Cars.Car();
            car.ServiceMileages = [-1];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceMileages[0]", "Service mileage must be greater than or equal to 0.");
        }

        /// <summary>
        /// The identity a caller chose is what names the element, in the message as well as in the code.
        /// </summary>
        [Fact]
        public async Task Element_WithAnIndexer_NamesTheElementByTheIdentity()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceMileages)
                .ForEach(mileage => mileage.WithIndexer((_, position) => $"entry-{position}").Element().GreaterThanOrEqualTo(0));

            var car = Cars.Car();
            car.ServiceMileages = [-1];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport(
                "ServiceMileages[entry-0]", "ServiceMileages[entry-0] must be greater than or equal to 0.");
        }
    }
}
