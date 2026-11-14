namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderTests
    {
        [Fact]
        public async Task PropertyName_ReportsTheFailureUnderTheOverride()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).WithPropertyName("vehicleId").NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("vehicleId", "Vin is required.");
        }

        /// <summary>
        /// The point of the override: a client field which is not shaped like the model's path.
        /// </summary>
        [Fact]
        public async Task PropertyName_ReplacesTheWholeMemberPath_NotOnlyItsLastSegment()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Name).WithPropertyName("manufacturerName").NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() });

            // Assert
            result.ShouldReport("manufacturerName", "Model.Name is required.");
        }

        /// <summary>
        /// Without the override the member path is what a failure is reported under, which is what keeps
        /// the override opt-in.
        /// </summary>
        [Fact]
        public async Task PropertyName_WhenNotDeclared_ReportsUnderTheMemberPath()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Name).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() });

            // Assert
            result.ShouldReport("Model.Name", "Model.Name is required.");
        }

        /// <summary>
        /// The two overrides are independent: one is what a caller binds to, the other is what a reader
        /// sees.
        /// </summary>
        [Fact]
        public async Task PropertyName_DoesNotChangeWhatTheMessageCallsTheProperty()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .WithPropertyName("vehicleId")
                .WithDisplayName("Vehicle identification number")
                .NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("vehicleId", "Vehicle identification number is required.");
        }

        /// <summary>
        /// A rule which reports per element chooses its own name, and the property-level override must
        /// not overwrite it.
        /// </summary>
        [Fact]
        public async Task PropertyName_LeavesANameARuleReportsUnderItself()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.FeatureIds)
                .WithPropertyName("features")
                .Add(context => context.AddError(new ValidationError("features[0]", "the first entry is wrong")));

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.ShouldReport("features[0]", "the first entry is wrong");
        }

        [Fact]
        public void PropertyName_WithoutAName_Throws()
        {
            // Arrange
            var validator = new TestValidator<Car>();

            // Act
            var act = () => validator.Property(c => c.Vin).WithPropertyName(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
