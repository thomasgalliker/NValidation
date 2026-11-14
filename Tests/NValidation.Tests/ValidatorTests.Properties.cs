namespace NValidation.Tests
{
    public partial class ValidatorTests
    {
        [Fact]
        public async Task ValidateAsync_WithProperties_RunsTheirChainsAlone()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.RegistrationPlate).NotEmpty();
            validator.Property(c => c.Mileage).GreaterThan(0);

            var options = new NValidationOptions { ValidationProperties = ["Vin", "Mileage"] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport([
                new("Vin", "Vin is required."),
                new("Mileage", "Mileage must be greater than 0.")]);
        }

        /// <summary>
        /// The names often come from a client, which spells them the way its payload does.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithProperties_IgnoresCase()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            var options = new NValidationOptions { ValidationProperties = ["vin"] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// A name that matches nothing may come from a client, so it is passed by rather than refused: a
        /// mistake there must not become a failed request.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithAPropertyNoChainIsFor_ReportsNothing()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();

            var options = new NValidationOptions { ValidationProperties = ["Unknown"] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_WithAPropertyOfANestedValidator_RunsOnlyThatChainOfIt()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();
            modelValidator.Property(m => m.Manufacturer).NotNull();

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.Model).NotNull().SetValidator(modelValidator);

            var options = new NValidationOptions { ValidationProperties = ["Model.Name"] };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("Model.Name", "Name is required.");
        }

        /// <summary>
        /// A chain on the way to a selected property hands its value on and does nothing else: what it
        /// checks of its own is about a property nobody asked about.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithAPropertyBelowAChain_DoesNotRunTheRulesOfThatChain()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).NotNull().SetValidator(modelValidator);

            var options = new NValidationOptions { ValidationProperties = ["Model.Name"] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_WithANestedObjectSelected_ValidatesItWhole()
        {
            // Arrange
            var modelValidator = new TestValidator<CarModel>();
            modelValidator.Property(m => m.Name).NotEmpty();
            modelValidator.Property(m => m.Manufacturer).NotNull();

            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty();
            validator.Property(c => c.Model).NotNull().SetValidator(modelValidator);

            var options = new NValidationOptions { ValidationProperties = ["Model"] };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport([
                new("Model.Name", "Name is required."),
                new("Model.Manufacturer", "Manufacturer is required.")]);
        }

        [Fact]
        public async Task ValidateAsync_WithAChainDeclaredThroughANestedPath_MatchesItsWholeName()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model!.Name).NotEmpty();
            validator.Property(c => c.Model!.BasePrice).NotNull();

            var options = new NValidationOptions { ValidationProperties = ["Model.Name"] };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("Model.Name", "Model.Name is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithAPropertyOfTheEntries_AppliesToEveryEntry()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .MinimumCount(3)
                .ForEach(record =>
                {
                    record.Property(r => r.Workshop).NotEmpty();
                    record.Property(r => r.Cost).GreaterThan(0m);
                });

            var car = new Car { ServiceHistory = [new ServiceRecord(), new ServiceRecord()] };

            var options = new NValidationOptions { ValidationProperties = ["ServiceHistory.Workshop"] };

            // Act
            var result = await validator.ValidateAsync(car, options);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop is required."),
                new("ServiceHistory[1].Workshop", "Workshop is required.")]);
        }

        [Fact]
        public async Task ValidateAsync_WithAChainReportedUnderANameOfItsOwn_MatchesThatName()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithPropertyName("Identification");

            var byReportedName = new NValidationOptions { ValidationProperties = ["Identification"] };
            var byMemberName = new NValidationOptions { ValidationProperties = ["Vin"] };

            // Act
            var reported = await validator.ValidateAsync(new Car(), byReportedName);
            var member = await validator.ValidateAsync(new Car(), byMemberName);

            // Assert
            reported.ShouldReport("Identification", "Vin is required.");
            member.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_WithPropertiesAndGroups_RunsAChainOnlyWhereBothSelectIt()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).NotEmpty().WithGroup("Listing");
            validator.Property(c => c.RegistrationPlate).NotEmpty().WithGroup("Listing");
            validator.Property(c => c.Mileage).GreaterThan(0);

            var options = new NValidationOptions
            {
                ValidationGroups = ValidationGroups.Only("Listing"),
                ValidationProperties = ["Vin", "Mileage"],
            };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is required.");
        }

        [Fact]
        public async Task ValidateAsync_WithPropertiesOnAnAwaitingValidator_RunsTheirChainsAlone()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).MustAsync((vin, _) => ValueTask.FromResult(vin != null));
            validator.Property(c => c.RegistrationPlate).NotEmpty();

            var options = new NValidationOptions { ValidationProperties = ["Vin"] };

            // Act
            var result = await validator.ValidateAsync(new Car(), options);

            // Assert
            result.ShouldReport("Vin", "Vin is not valid.");
        }

        /// <summary>
        /// A validator written by hand is handed what lies below the chain composing it, as a derived one is.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithProperties_HandAValidatorWrittenByHandWhatLiesBelowIt()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Model).SetValidator(new PropertiesReportingValidator());

            var options = new NValidationOptions { ValidationProperties = ["Model.Name", "Vin"] };

            // Act
            var result = await validator.ValidateAsync(new Car { Model = new CarModel() }, options);

            // Assert
            result.ShouldReport("Model.Selection", "Name");
        }

        /// <summary>
        /// Written by hand, so it is reached through the options a run is turned back into; it reports the
        /// properties it was limited to.
        /// </summary>
        private sealed class PropertiesReportingValidator : IValidator<CarModel>
        {
            public ValueTask<ValidationResult> ValidateAsync(CarModel instance, CancellationToken cancellationToken = default)
            {
                return new ValueTask<ValidationResult>(
                    ValidationResult.FromValidationErrors(new ValidationError("Selection", "Every property")));
            }

            public ValueTask<ValidationResult> ValidateAsync(
                CarModel instance, NValidationOptions options, CancellationToken cancellationToken = default)
            {
                return new ValueTask<ValidationResult>(ValidationResult.FromValidationErrors(
                    new ValidationError("Selection", options.ValidationProperties?.ToString() ?? "Every property")));
            }
        }
    }
}
