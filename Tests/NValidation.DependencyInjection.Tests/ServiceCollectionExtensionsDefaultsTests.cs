namespace NValidation.DependencyInjection.Tests
{
    /// <summary>
    /// Covers where <see cref="NValidationOptions.Default"/> sits relative to <c>AddNValidation</c>: it
    /// is the layer underneath, so a registration which configured nothing falls through to it and a
    /// registration which configured something outranks it.
    /// </summary>
    /// <remarks>
    /// The registration hands a provider over only where the host registered one, so a validator it
    /// built otherwise reads the defaults while validating — including a default assigned after the
    /// container was built.
    /// </remarks>
    [Trait(Traits.Category, Traits.UnitTests)]
    [Collection(Collections.ValidationDefaults)]
    public class ServiceCollectionExtensionsDefaultsTests : IDisposable
    {
        private readonly NValidationOptions original = NValidationOptions.Default;

        public ServiceCollectionExtensionsDefaultsTests()
        {
            NValidationOptions.Default = new NValidationOptions();
        }

        public void Dispose()
        {
            NValidationOptions.Default = this.original;
        }

        /// <summary>
        /// Nothing is registered on the host's behalf: a provider the container hands over would outrank
        /// the options a call is given, and capturing the default at resolution would hide a default
        /// assigned afterwards.
        /// </summary>
        [Fact]
        public void AddNValidation_WithoutAConfiguredProvider_RegistersNoProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation();

            // Act
            var messageProvider = services.BuildServiceProvider().GetService<IValidationMessageProvider>();

            // Assert
            messageProvider.Should().BeNull();
        }

        /// <summary>
        /// The defaults are read while validating, so a default assigned after the container built the
        /// validator still reaches it.
        /// </summary>
        [Fact]
        public async Task AddValidator_WithoutAConfiguredProvider_AnswersThroughADefaultAssignedLater()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(b => b.AddValidator<Manufacturer, ManufacturerValidator>());

            var validator = Resolve<IValidator<Manufacturer>>(services);

            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().AllSatisfy(error => error.Message.Should().Be("message from the default provider"));
        }

        /// <summary>
        /// A validator the container built answers through the defaults, which is what the registration
        /// used to overwrite.
        /// </summary>
        [Fact]
        public async Task AddValidator_WithoutAConfiguredProvider_AnswersThroughTheDefault()
        {
            // Arrange
            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            var services = new ServiceCollection();
            services.AddNValidation(b => b.AddValidator<Manufacturer, ManufacturerValidator>());

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().AllSatisfy(error => error.Message.Should().Be("message from the default provider"));
        }

        /// <summary>
        /// The registration is the more specific level, so what it names wins.
        /// </summary>
        [Fact]
        public void MessageProvider_OfTheRegistration_OutranksTheDefault()
        {
            // Arrange
            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            var services = new ServiceCollection();
            services.AddNValidation(b => b.MessageProvider = typeof(RegisteredMessageProvider));

            // Act
            var messageProvider = Resolve<IValidationMessageProvider>(services);

            // Assert
            messageProvider.Should().BeOfType<RegisteredMessageProvider>();
        }

        /// <summary>
        /// TryAdd, so a host which claimed the service first still keeps it — the defaults do not
        /// displace it either.
        /// </summary>
        [Fact]
        public void AddNValidation_KeepsAProviderTheHostRegisteredFirst_EvenWithADefault()
        {
            // Arrange
            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            var services = new ServiceCollection();
            services.AddSingleton<IValidationMessageProvider, RegisteredMessageProvider>();
            services.AddNValidation();

            // Act
            var messageProvider = Resolve<IValidationMessageProvider>(services);

            // Assert
            messageProvider.Should().BeOfType<RegisteredMessageProvider>();
        }

        /// <summary>
        /// And through a validator, not only through the resolved service: the registration's provider
        /// is the rung above the defaults, so a validator the container built answers with it.
        /// </summary>
        [Fact]
        public async Task AddValidator_WithAConfiguredProvider_OutranksTheDefault()
        {
            // Arrange
            NValidationOptions.Default = new() { MessageProvider = new StubMessageProvider() };

            var services = new ServiceCollection();
            services.AddNValidation(b =>
            {
                b.MessageProvider = typeof(RegisteredMessageProvider);
                b.AddValidator<Manufacturer, ManufacturerValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport([
                new("Name", "message from the registered provider"),
                new("CountryCode", "message from the registered provider")]);
        }

        /// <summary>
        /// A registration which named neither axis leaves both inheriting, so the defaults are reached.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_OfTheDefault_ReachAValidatorTheContainerBuilt()
        {
            // Arrange
            NValidationOptions.Default = new() { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

            var services = new ServiceCollection();
            services.AddNValidation(b => b.AddValidator<Manufacturer, TwoRuleValidator>());

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport([
                new("Name", "Name must be Aurora."),
                new("Name", "Name must be spelled out.")]);
        }

        /// <summary>
        /// And a registration which did name an axis outranks the default on that axis.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_OfTheRegistration_OutrankTheDefault()
        {
            // Arrange
            NValidationOptions.Default = new() { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

            var services = new ServiceCollection();
            services.AddNValidation(b =>
            {
                b.ValidationBehaviors = new() { Property = ValidationBehavior.StopAtFirstError };
                b.AddValidator<Manufacturer, TwoRuleValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "Name must be Aurora.");
        }

        private static TService Resolve<TService>(IServiceCollection services)
            where TService : notnull
        {
            var serviceProvider = services.BuildServiceProvider();
            var scope = serviceProvider.CreateScope();

            return scope.ServiceProvider.GetRequiredService<TService>();
        }

        private sealed class StubMessageProvider : IValidationMessageProvider
        {
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return "message from the default provider";
            }
        }

        private sealed class RegisteredMessageProvider : IValidationMessageProvider
        {
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return "message from the registered provider";
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
    }
}
