namespace NValidation.Tests
{
    /// <summary>
    /// Covers options supplied for one call rather than configured for the process: what they reach,
    /// and what still outranks them.
    /// </summary>
    /// <remarks>
    /// None of these touch <see cref="NValidationOptions.Default"/>, so unlike the tests of the process-wide
    /// defaults they need neither a serialized collection nor a reset between them. That is the point
    /// of passing options rather than configuring them.
    /// </remarks>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class NValidationOptionsTests
    {
        /// <summary>
        /// A nested validator is constructed by its composer, so nothing a registration configures can
        /// be handed to it. What the run was asked for is read, so it arrives.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOptions_ReachANestedValidator()
        {
            // Arrange
            var options = new NValidationOptions();
            options.ValidationBehaviors.Property = ValidationBehavior.All;

            var validator = new ComposingValidator(new TwoRuleValidator());

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() }, options);

            // Assert
            result.ShouldReport([
                new("Manufacturer.Name", "Name must be Aurora."),
                new("Manufacturer.Name", "Name must be spelled out.")]);
        }

        [Fact]
        public async Task ValidateAsync_WithOptions_ReachAnElementChain()
        {
            // Arrange
            var options = new NValidationOptions();
            options.ValidationBehaviors.Property = ValidationBehavior.All;

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop)
                    .Must(workshop => workshop == "Aurora", "Workshop must be Aurora.")
                    .Must(workshop => workshop == "Northgate", "Workshop must be Northgate."));

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = "Elsewhere", Mileage = 10, Cost = 10m }];

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop must be Aurora."),
                new("ServiceHistory[0].Workshop", "Workshop must be Northgate.")]);
        }

        /// <summary>
        /// The run carries a default, not an instruction: a composed validator which declared an axis
        /// for itself still decides for itself.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOptions_AreOutrankedByWhatANestedValidatorDeclared()
        {
            // Arrange
            var options = new NValidationOptions();
            options.ValidationBehaviors.Property = ValidationBehavior.All;

            var nested = new TwoRuleValidator();
            nested.ValidationBehaviors.Property = ValidationBehavior.StopAtFirstError;

            var validator = new ComposingValidator(nested);

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() }, options);

            // Assert
            result.ShouldReport("Manufacturer.Name", "Name must be Aurora.");
        }

        [Fact]
        public async Task ValidateAsync_WithOptions_UsesTheirMessageProvider()
        {
            // Arrange
            var options = new NValidationOptions { MessageProvider = new StubMessageProvider() };

            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer(), options);

            // Assert
            result.ShouldReport("Name", "message from the supplied options");
        }

        /// <summary>
        /// A validator which was handed a provider of its own keeps it, the same way it keeps an axis it
        /// declared for itself.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOptions_AreOutrankedByTheValidatorsOwnMessages()
        {
            // Arrange
            var options = new NValidationOptions { MessageProvider = new StubMessageProvider() };

            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer(), options);

            // Assert
            result.ShouldReport("Name", "NotEmpty");
        }

        /// <summary>
        /// Using them freezes them, exactly as reading <see cref="NValidationOptions.Default"/> does.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOptions_FreezesThem()
        {
            // Arrange
            var options = new NValidationOptions();

            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var before = options.IsReadOnly;
            await validator.ValidateAsync(new Manufacturer(), options);

            // Assert
            before.Should().BeFalse();
            options.IsReadOnly.Should().BeTrue();

            var act = () => options.ValidationBehaviors.Property = ValidationBehavior.All;
            act.Should().Throw<InvalidOperationException>().WithMessage("*already been used*Reset()*");
        }

        [Fact]
        public async Task ValidateAsync_WithNullOptions_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var act = async () => await validator.ValidateAsync(new Manufacturer(), null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void MessageProvider_CannotBeSetToNull()
        {
            // Arrange
            var options = new NValidationOptions();

            // Act
            var act = () => options.MessageProvider = null!;

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        private sealed class StubMessageProvider : IValidationMessageProvider
        {
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return "message from the supplied options";
            }
        }

        private sealed class TwoRuleValidator : Validator<Manufacturer>
        {
            public TwoRuleValidator()
            {
                this.Property(m => m.Name)
                    .Must(name => name == "Aurora", "Name must be Aurora.")
                    .Must(name => name == "Northgate", "Name must be spelled out.");
            }
        }

        private sealed class ComposingValidator : Validator<CarModel>
        {
            public ComposingValidator(IValidator<Manufacturer> manufacturerValidator)
            {
                this.Property(m => m.Manufacturer).SetValidator(manufacturerValidator);
            }
        }
    }
}
