namespace NValidation.Tests
{
    /// <summary>
    /// Covers <see cref="NValidationOptions.Default"/>: what it reaches, what outranks it, and the
    /// freeze that stops it changing under a run which is already reading it.
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
        // In the constructor as well as Dispose: an earlier test in another collection has already
        // validated something, which froze the defaults.
        public NValidationDefaultsTests()
        {
            NValidationOptions.Default.Reset();
        }

        public void Dispose()
        {
            NValidationOptions.Default.Reset();
        }

        [Fact]
        public async Task MessageProvider_ReachesAValidatorConstructedWithNew()
        {
            // Arrange
            NValidationOptions.Default.MessageProvider = new StubMessageProvider();

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
            NValidationOptions.Default.MessageProvider = new StubMessageProvider();

            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "NotEmpty");
        }

        /// <summary>
        /// Resolved while validating, so a default set after the validator was constructed still
        /// reaches it.
        /// </summary>
        [Fact]
        public async Task MessageProvider_SetAfterTheValidatorWasConstructed_StillReachesIt()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            NValidationOptions.Default.MessageProvider = new StubMessageProvider();

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
            NValidationOptions.Default.ValidationBehaviors.Property = ValidationBehavior.All;

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
            NValidationOptions.Default.ValidationBehaviors.Property = ValidationBehavior.All;

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop)
                    .Must(workshop => workshop == "Aurora", "Workshop must be Aurora.")
                    .Must(workshop => workshop == "Northgate", "Workshop must be Northgate."));

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
            NValidationOptions.Default.ValidationBehaviors.Property = ValidationBehavior.All;

            var validator = new TwoRuleValidator();
            validator.ValidationBehaviors.Property = ValidationBehavior.StopAtFirstError;

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "Name must be Aurora.");
        }

        /// <summary>
        /// Naming one axis leaves the other inheriting, exactly as it does on every other level.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_NamingOneAxis_LeavesTheOtherInheriting()
        {
            // Arrange
            NValidationOptions.Default.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;

            var validator = new TwoRuleValidator();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert — the chain axis keeps its built-in default of one message per property
            result.ShouldReport("Name", "Name must be Aurora.");
        }

        [Fact]
        public async Task IsReadOnly_IsFalseUntilSomethingValidates()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();

            // Act
            var before = NValidationOptions.Default.IsReadOnly;
            await validator.ValidateAsync(new Manufacturer());

            // Assert
            before.Should().BeFalse();
            NValidationOptions.Default.IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public async Task MessageProvider_SetAfterSomethingValidated_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();
            await validator.ValidateAsync(new Manufacturer());

            // Act
            var act = () => NValidationOptions.Default.MessageProvider = new StubMessageProvider();

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*already been used*Reset()*");
        }

        [Fact]
        public async Task ValidationBehaviors_SetAfterSomethingValidated_Throw()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotEmpty();
            await validator.ValidateAsync(new Manufacturer());

            // Act
            var setClass = () => NValidationOptions.Default.ValidationBehaviors.Class = ValidationBehavior.All;
            var setProperty = () => NValidationOptions.Default.ValidationBehaviors.Property = ValidationBehavior.All;

            // Assert
            setClass.Should().Throw<InvalidOperationException>().WithMessage("*already been used*Reset()*");
            setProperty.Should().Throw<InvalidOperationException>().WithMessage("*already been used*Reset()*");
        }

        /// <summary>
        /// For a host which would rather a misplaced configuration call failed at startup than whenever
        /// validation first happens to run.
        /// </summary>
        [Fact]
        public void MakeReadOnly_FreezesWithoutWaitingForAValidation()
        {
            // Act
            NValidationOptions.Default.MakeReadOnly();

            // Assert
            NValidationOptions.Default.IsReadOnly.Should().BeTrue();

            var act = () => NValidationOptions.Default.MessageProvider = new StubMessageProvider();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task Reset_PutsTheBuiltInDefaultsBackAndAllowsChangesAgain()
        {
            // Arrange
            NValidationOptions.Default.MessageProvider = new StubMessageProvider();
            NValidationOptions.Default.ValidationBehaviors.Property = ValidationBehavior.All;
            NValidationOptions.Default.MakeReadOnly();

            // Act
            NValidationOptions.Default.Reset();

            // Captured before validating, which freezes them again.
            var isReadOnly = NValidationOptions.Default.IsReadOnly;
            var messageProvider = NValidationOptions.Default.MessageProvider;
            var propertyBehavior = NValidationOptions.Default.ValidationBehaviors.Property;

            var validator = new TwoRuleValidator();
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            isReadOnly.Should().BeFalse();
            messageProvider.Should().BeOfType<DefaultValidationMessageProvider>();
            propertyBehavior.Should().BeNull();
            result.ShouldReport("Name", "Name must be Aurora.");
        }

        [Fact]
        public void MessageProvider_CannotBeSetToNull()
        {
            // Act
            var act = () => NValidationOptions.Default.MessageProvider = null!;

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// The copy does not go on sharing with what it was copied from, and is not frozen by it.
        /// </summary>
        [Fact]
        public void CopyConstructor_TakesTheValuesWithoutTheFreeze()
        {
            // Arrange
            NValidationOptions.Default.MessageProvider = new StubMessageProvider();
            NValidationOptions.Default.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;
            NValidationOptions.Default.MakeReadOnly();

            // Act
            var copy = new NValidationOptions(NValidationOptions.Default);
            copy.ValidationBehaviors.Property = ValidationBehavior.All;

            // Assert
            copy.IsReadOnly.Should().BeFalse();
            copy.MessageProvider.Should().BeOfType<StubMessageProvider>();
            copy.ValidationBehaviors.Class.Should().Be(ValidationBehavior.StopAtFirstError);
            NValidationOptions.Default.ValidationBehaviors.Property.Should().BeNull();
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
