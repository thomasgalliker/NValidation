namespace NValidation.Tests
{
    /// <summary>
    /// Covers options supplied for one call rather than configured for the process: what they reach,
    /// and what still outranks them.
    /// </summary>
    /// <remarks>
    /// None of these touch <see cref="NValidationOptions.Default"/>, so unlike the tests of the
    /// process-wide defaults they need no serialized collection. That is the point of passing options
    /// rather than configuring them.
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
            var options = new NValidationOptions { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

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
            var options = new NValidationOptions { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop)
                    .Must(workshop => workshop == "Aurora").WithMessage("Workshop must be Aurora.")
                    .Must(workshop => workshop == "Northgate").WithMessage("Workshop must be Northgate."));

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
            var options = new NValidationOptions { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

            var nested = new TwoRuleValidator();
            nested.ValidationBehaviors = new() { Property = ValidationBehavior.StopAtFirstError };

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

        [Fact]
        public async Task ValidateAsync_WithSelectedGroups_ReachANestedValidator()
        {
            // Arrange
            var options = new NValidationOptions { ValidationGroups = "Create" };

            var nested = new TestValidator<Manufacturer>();
            nested.Property(m => m.Name).NotEmpty().WithGroup("Create");

            var validator = new ComposingValidator(nested);

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() }, options);

            // Assert
            result.ShouldReport("Manufacturer.Name", "Name is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithSelectedGroups_ReachAnElementChain()
        {
            // Arrange
            var options = new NValidationOptions { ValidationGroups = "Create" };

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty().WithGroup("Create"));

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null }];

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", "Workshop is required.");
        }

        /// <summary>
        /// A validator written by hand can only be told through options, so what the run resolved is
        /// turned back into options for it — which is the only way the selection reaches one at all.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithSelectedGroups_ReachAValidatorWrittenByHand()
        {
            // Arrange
            var options = new NValidationOptions { ValidationGroups = "Create" };

            var validator = new ComposingValidator(new GroupReportingValidator());

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() }, options);

            // Assert
            result.ShouldReport("Manufacturer.Selection", "Create");
        }

        /// <summary>
        /// A validator written by hand declares nothing the library can see, so it cannot take part in a
        /// selection that leaves the default group out; it is handed the additive form instead.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOnly_HandsAValidatorWrittenByHandTheAdditiveSelection()
        {
            // Arrange
            var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Create") };

            var validator = new TestValidator<CarModel>();
            validator.Property(m => m.Manufacturer).SetValidator(new GroupReportingValidator()).WithGroup("Create");

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() }, options);

            // Assert
            result.ShouldReport("Manufacturer.Selection", "Create");
        }

        [Fact]
        public async Task ValidateAsync_WithData_ReachANestedValidator()
        {
            // Arrange
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, RequiresServiceHistory: true)] };

            var nested = new TestValidator<Manufacturer>();
            nested.Property(m => m.Website).NotEmpty().When<ListingPolicy>((_, policy) => policy.RequiresServiceHistory);

            var validator = new ComposingValidator(nested);

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() }, options);

            // Assert
            result.ShouldReport("Manufacturer.Website", "Website is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithData_ReachAValidatorWrittenByHand()
        {
            // Arrange
            var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };

            var validator = new ComposingValidator(new DataReportingValidator());

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() }, options);

            // Assert
            result.ShouldReport("Manufacturer.Data", "ListingPolicy");
        }

        [Fact]
        public void ValidationData_LeftUnset_IsNull()
        {
            // Arrange
            var options = new NValidationOptions();

            // Assert
            options.ValidationData.Should().BeNull();
        }

        [Fact]
        public void ValidationGroups_LeftUnset_IsNull()
        {
            // Arrange
            var options = new NValidationOptions();

            // Assert
            options.ValidationGroups.Should().BeNull();
        }

        [Fact]
        public async Task ValidateAsync_WithOptionsNamingOnlyGroups_KeepTheMessagesOfTheLevelBelow()
        {
            // Arrange
            var options = new NValidationOptions { ValidationGroups = "Create" };

            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Name).NotEmpty().WithGroup("Create");

            // Act
            var result = await validator.ValidateAsync(new Manufacturer(), options);

            // Assert
            result.ShouldReport("Name", "NotEmpty");
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

        /// <summary>
        /// Fresh options name nothing, so passing them changes only what they were given. That is what
        /// lets a call ask for one setting without restating the others.
        /// </summary>
        [Fact]
        public void MessageProvider_LeftUnset_IsNull()
        {
            // Arrange
            var options = new NValidationOptions();

            // Assert
            options.MessageProvider.Should().BeNull();
        }

        /// <summary>
        /// The same for the messages as for the behaviors: a composed validator which declared a provider
        /// for itself keeps it, and the run's options only reach one which declared nothing.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOptions_AreOutrankedByANestedValidatorsOwnMessageProvider()
        {
            // Arrange
            var options = new NValidationOptions { MessageProvider = new StubMessageProvider() };

            var nested = new TestValidator<Manufacturer>(new NestedMessageProvider());
            nested.Property(m => m.Name).NotEmpty();

            var validator = new ComposingValidator(nested);

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() }, options);

            // Assert
            result.ShouldReport("Manufacturer.Name", "message from the nested validator's own provider");
        }

        private sealed class StubMessageProvider : IValidationMessageProvider
        {
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return "message from the supplied options";
            }
        }

        private sealed class NestedMessageProvider : IValidationMessageProvider
        {
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return "message from the nested validator's own provider";
            }
        }

        private sealed class TwoRuleValidator : Validator<Manufacturer>
        {
            public TwoRuleValidator()
            {
                this.Property(m => m.Name)
                    .Must(name => name == "Aurora").WithMessage("Name must be Aurora.")
                    .Must(name => name == "Northgate").WithMessage("Name must be spelled out.");
            }
        }

        /// <summary>
        /// Written by hand rather than derived, so it is reached through the options a run is turned back
        /// into; it reports what it was asked to select, which is what the test is about.
        /// </summary>
        private sealed class GroupReportingValidator : IValidator<Manufacturer>
        {
            public ValueTask<ValidationResult> ValidateAsync(Manufacturer instance, CancellationToken cancellationToken = default)
            {
                return new ValueTask<ValidationResult>(
                    ValidationResult.FromValidationErrors(new ValidationError("Selection", "None")));
            }

            public ValueTask<ValidationResult> ValidateAsync(
                Manufacturer instance, NValidationOptions options, CancellationToken cancellationToken = default)
            {
                return new ValueTask<ValidationResult>(ValidationResult.FromValidationErrors(
                    new ValidationError("Selection", options.ValidationGroups?.ToString() ?? "None")));
            }
        }

        /// <summary>
        /// Written by hand, so the data reaches it through the options a run is turned back into; it
        /// reports what it was handed.
        /// </summary>
        private sealed class DataReportingValidator : IValidator<Manufacturer>
        {
            public ValueTask<ValidationResult> ValidateAsync(Manufacturer instance, CancellationToken cancellationToken = default)
            {
                return new ValueTask<ValidationResult>(
                    ValidationResult.FromValidationErrors(new ValidationError("Data", "None")));
            }

            public ValueTask<ValidationResult> ValidateAsync(
                Manufacturer instance, NValidationOptions options, CancellationToken cancellationToken = default)
            {
                return new ValueTask<ValidationResult>(ValidationResult.FromValidationErrors(
                    new ValidationError("Data", options.ValidationData?.ToString() ?? "None")));
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
