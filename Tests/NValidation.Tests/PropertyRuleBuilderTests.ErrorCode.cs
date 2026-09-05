namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderTests
    {
        [Fact]
        public async Task ErrorCode_ReportsTheFailureUnderTheOverride()
        {
            // Arrange
            var validator = new VinErrorCodeValidator("vehicleId");

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("vehicleId");
        }

        /// <summary>
        /// The point of the override: a client field which is not shaped like the model's path.
        /// </summary>
        [Fact]
        public async Task ErrorCode_ReplacesTheWholeMemberPath_NotOnlyItsLastSegment()
        {
            // Arrange
            var validator = new NestedNameErrorCodeValidator();

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() });

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("manufacturerName");
        }

        /// <summary>
        /// Without the override the member path is what a failure is reported under, which is what keeps
        /// the override opt-in.
        /// </summary>
        [Fact]
        public async Task ErrorCode_WhenNotDeclared_ReportsUnderTheMemberPath()
        {
            // Arrange
            var validator = new ModelNameNotEmptyValidator();

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() });

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("Model.Name");
        }

        /// <summary>
        /// The two overrides are independent: one is what a caller binds to, the other is what a reader
        /// sees.
        /// </summary>
        [Fact]
        public async Task ErrorCode_DoesNotChangeWhatTheMessageCallsTheProperty()
        {
            // Arrange
            var validator = new VinErrorCodeAndDisplayNameValidator();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            var error = result.Errors.Should().ContainSingle().Subject;
            error.Code.Should().Be("vehicleId");
            error.Message.Should().Be("Vehicle identification number is required.");
        }

        /// <summary>
        /// A rule which reports per element chooses its own code, and the property-level override must
        /// not overwrite it.
        /// </summary>
        [Fact]
        public async Task ErrorCode_LeavesACodeARuleReportsUnderItself()
        {
            // Arrange
            var validator = new FeatureIdsErrorCodeWithCustomCodeValidator();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("features[0]");
        }

        [Fact]
        public void ErrorCode_WithoutACode_Throws()
        {
            // Act
            var act = () => new VinErrorCodeValidator(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
