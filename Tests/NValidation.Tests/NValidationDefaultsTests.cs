namespace NValidation.Tests
{
    /// <summary>
    /// Covers <see cref="NValidationOptions.Default"/>: what it reaches and what outranks it.
    /// </summary>
    /// <remarks>
    /// The two tests worth reading first are the ones about a nested validator and an element chain.
    /// Neither is constructed by the container, so no setting on a registration can be handed to them;
    /// the defaults are read rather than handed, which is why they arrive.
    /// </remarks>
    [Trait(Traits.Category, Traits.UnitTests)]
    [Collection(Collections.ValidationDefaults)]
    public class NValidationDefaultsTests : IDisposable
    {
        private readonly NValidationOptions original = NValidationOptions.Default;

        // A fresh instance per test, and the original put back afterwards.
        public NValidationDefaultsTests()
        {
            NValidationOptions.Default = new NValidationOptions();
        }

        public void Dispose()
        {
            NValidationOptions.Default = this.original;
        }

        [Fact]
        public async Task MessageProvider_ReachesAValidatorConstructedWithNew()
        {
            // Arrange
            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "message from the default provider");
        }

        /// <summary>
        /// The validator's own provider is more specific, so it wins.
        /// </summary>
        [Fact]
        public async Task MessageProvider_IsOutrankedByTheValidatorsOwn()
        {
            // Arrange
            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "NotEmpty");
        }

        [Fact]
        public async Task MessageProvider_SetAfterTheValidatorWasConstructed_StillReachesIt()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "message from the default provider");
        }

        /// <summary>
        /// The headline: a nested validator is constructed with <c>new</c> inside its composer, so no
        /// registration can hand it anything. The defaults are read, so they arrive.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_ReachANestedValidator()
        {
            // Arrange
            NValidationOptions.Default = new() { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

            var validator = new ComposingValidator(new TwoRuleValidator());

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() });

            // Assert
            result.ShouldReport([
                new("Manufacturer.Name", "Name must be Aurora."),
                new("Manufacturer.Name", "Name must be spelled out.")]);
        }

        /// <summary>
        /// The same for the element chain of a <c>ForEach</c>, which is built where it is declared and
        /// has no handle a registration could reach either.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_ReachAnElementChain()
        {
            // Arrange
            NValidationOptions.Default = new() { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop)
                    .Must(workshop => workshop == "Aurora").WithMessage("Workshop must be Aurora.")
                    .Must(workshop => workshop == "Northgate").WithMessage("Workshop must be Northgate."));

            var car = Cars.Car();
            car.ServiceHistory = [new ServiceRecord { Workshop = "Elsewhere", Mileage = 10, Cost = 10m }];

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop must be Aurora."),
                new("ServiceHistory[0].Workshop", "Workshop must be Northgate.")]);
        }

        /// <summary>
        /// An axis the validator named for itself is more specific, so the default does not displace it.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_AreOutrankedByTheValidatorsOwn()
        {
            // Arrange
            NValidationOptions.Default = new() { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

            var validator = new TwoRuleValidator();
            validator.ValidationBehaviors = new() { Property = ValidationBehavior.StopAtFirstError };

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "Name must be Aurora.");
        }

        [Fact]
        public async Task ValidationBehaviors_NamingOneAxis_LeavesTheOtherInheriting()
        {
            // Arrange
            NValidationOptions.Default = new() { ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError } };

            var validator = new TwoRuleValidator();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert — the chain axis keeps its built-in default of one message per property
            result.ShouldReport("Name", "Name must be Aurora.");
        }

        /// <summary>
        /// Assigning replaces what later runs fall back to, at any time, because it changes a reference
        /// rather than an object.
        /// </summary>
        [Fact]
        public async Task Default_AssignedAfterARun_ReplacesWhatLaterRunsFallBackTo()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            await validator.ValidateAsync(new Manufacturer());

            // Act
            NValidationOptions.Default = new NValidationOptions { MessageProvider = new StubMessageProvider() };

            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "message from the default provider");
        }

        /// <summary>
        /// Options are immutable, so a variant is derived rather than the original changed: what an
        /// earlier run read stays exactly as that run saw it.
        /// </summary>
        [Fact]
        public void With_DerivesOptionsAndLeavesTheOriginalAlone()
        {
            // Arrange
            var original = new NValidationOptions
            {
                MessageProvider = new StubMessageProvider(),
                ValidationBehaviors = new ValidationBehaviors { Class = ValidationBehavior.StopAtFirstError },
            };

            // Act
            var derived = original with
            {
                ValidationBehaviors = original.ValidationBehaviors with
                {
                    Property = ValidationBehavior.All
                }
            };

            // Assert
            derived.MessageProvider.Should().BeSameAs(original.MessageProvider);
            derived.ValidationBehaviors.Class.Should().Be(ValidationBehavior.StopAtFirstError);
            derived.ValidationBehaviors.Property.Should().Be(ValidationBehavior.All);
            original.ValidationBehaviors.Property.Should().BeNull();
        }

        /// <summary>
        /// Options naming nothing leave every setting to the level below, and failing every level the
        /// built-in English answers.
        /// </summary>
        [Fact]
        public async Task Default_NamingNothing_FallsBackToTheBuiltInEnglish()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "Name is required.");
        }

        [Fact]
        public void Default_CannotBeSetToNull()
        {
            // Act
            var act = () => NValidationOptions.Default = null!;

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Options which named only a behavior leave the messages to the level below. Naming one setting
        /// never quietly restates another — the same rule the two axes follow.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOptionsNamingOnlyBehaviors_KeepsTheDefaultsMessageProvider()
        {
            // Arrange
            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty().MaximumLength(3);

            var options = new NValidationOptions { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { Name = "    " }, options);

            // Assert
            result.ShouldReport([
                new("Name", "message from the default provider"),
                new("Name", "message from the default provider")]);
        }

        private sealed class StubMessageProvider : IValidationMessageProvider
        {
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return "message from the default provider";
            }
        }

        /// <summary>
        /// One property, two rules a single value breaks, so the chain axis is observable.
        /// </summary>
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
