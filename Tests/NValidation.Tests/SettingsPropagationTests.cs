namespace NValidation.Tests
{
    /// <summary>
    /// What a composer resolved for itself — its message provider and its behaviors — is inherited by
    /// the validators it composes and by the element chain of a <c>ForEach</c>, unless they declared
    /// otherwise for themselves. The same rule the options of a call and
    /// <see cref="NValidationOptions.Default"/> follow, one level down.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    [Collection(Collections.ValidationDefaults)]
    public class SettingsPropagationTests : IDisposable
    {
        private readonly NValidationOptions original = NValidationOptions.Default;

        // Depends on NValidationOptions.Default carrying nothing, so it installs a fresh instance and
        // puts the original back.
        public SettingsPropagationTests()
        {
            NValidationOptions.Default = new NValidationOptions();
        }

        public void Dispose()
        {
            NValidationOptions.Default = this.original;
        }

        [Fact]
        public async Task ValidateAsync_WhatAComposerDeclaredForItself_ReachesANestedValidatorAsADefault()
        {
            // Arrange
            var validator = new ComposingValidator(new TwoRuleValidator());
            validator.ValidationBehaviors = new() { Property = ValidationBehavior.All };

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() });

            // Assert
            result.ShouldReport([
                new("Manufacturer.Name", "Name must be Aurora."),
                new("Manufacturer.Name", "Name must be spelled out.")]);
        }

        /// <summary>
        /// Inherited is a default, not an instruction: a nested validator which declared an axis for
        /// itself keeps it.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WhatAComposerDeclaredForItself_IsOutrankedByWhatTheNestedValidatorDeclared()
        {
            // Arrange
            var nested = new TwoRuleValidator();
            nested.ValidationBehaviors = new() { Property = ValidationBehavior.StopAtFirstError };

            var validator = new ComposingValidator(nested);
            validator.ValidationBehaviors = new() { Property = ValidationBehavior.All };

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() });

            // Assert
            result.ShouldReport("Manufacturer.Name", "Name must be Aurora.");
        }

        [Fact]
        public async Task ValidateAsync_TheComposersMessageProvider_ReachesANestedValidatorWhichDeclaredNone()
        {
            // Arrange
            var nested = new TestValidator<Manufacturer>();
            nested.Property(m => m.Name).NotEmpty();

            var validator = new ComposingValidator(nested)
            {
                ValidationMessageProvider = ErrorCodeProvider.Instance,
            };

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() });

            // Assert
            result.ShouldReport("Manufacturer.Name", "NotEmpty");
        }

        [Fact]
        public async Task ValidateAsync_TheComposersMessageProvider_IsOutrankedByTheNestedValidatorsOwn()
        {
            // Arrange
            var nested = new TestValidator<Manufacturer>(new NestedMessageProvider());
            nested.Property(m => m.Name).NotEmpty();

            var validator = new ComposingValidator(nested)
            {
                ValidationMessageProvider = ErrorCodeProvider.Instance,
            };

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() });

            // Assert
            result.ShouldReport("Manufacturer.Name", "message from the nested validator's own provider");
        }

        /// <summary>
        /// The element chain of a <c>ForEach</c> is declared inside the composer, so it answers like the
        /// composer: here through the provider the composer was given.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_TheComposersMessageProvider_ReachesAnElementChain()
        {
            // Arrange
            var validator = new TestValidator<Car>(ErrorCodeProvider.Instance);
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = null }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport("ServiceHistory[0].Workshop", "NotEmpty");
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

        private sealed class ComposingValidator : Validator<CarModel>
        {
            public ComposingValidator(IValidator<Manufacturer> manufacturerValidator)
            {
                this.Property(m => m.Manufacturer).SetValidator(manufacturerValidator);
            }
        }
    }
}
