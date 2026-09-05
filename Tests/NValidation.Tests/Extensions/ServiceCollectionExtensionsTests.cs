namespace NValidation.Tests.Extensions
{
    /// <summary>
    /// Covers how validation is wired up: the defaults, how a host replaces them, and — most
    /// importantly — that a registered validator is actually handed the configured message provider.
    /// Without that last one a validator would quietly answer in the core's built-in English.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddNValidation_RegistersTheBuiltInMessageProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation();

            // Act
            var messageProvider = Resolve<IValidationMessageProvider>(services);

            // Assert
            messageProvider.Should().BeOfType<DefaultValidationMessageProvider>();
        }

        /// <summary>
        /// The defaults are registered with TryAdd, so a host which registered its own provider first
        /// keeps it.
        /// </summary>
        [Fact]
        public void AddNValidation_KeepsAProviderTheHostRegisteredFirst()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<IValidationMessageProvider, TestMessageProvider>();
            services.AddNValidation();

            // Act
            var messageProvider = Resolve<IValidationMessageProvider>(services);

            // Assert
            messageProvider.Should().BeOfType<TestMessageProvider>();
        }

        [Fact]
        public void MessageProvider_ReplacesTheDefault()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o.MessageProvider = typeof(TestMessageProvider));

            // Act
            var messageProvider = Resolve<IValidationMessageProvider>(services);

            // Assert
            messageProvider.Should().BeOfType<TestMessageProvider>();
        }

        [Fact]
        public async Task AddValidator_HandsTheConfiguredMessageProvider_ToTheValidator()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.MessageProvider = typeof(TestMessageProvider);
                o.AddValidator<Manufacturer, ManufacturerValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().OnlyContain(error => error.Message == TestMessageProvider.Message);
        }

        /// <summary>
        /// A validator with no configured provider answers in the built-in English, which is the failure
        /// the test above guards against.
        /// </summary>
        [Fact]
        public async Task AddValidator_WithoutAConfiguredProvider_AnswersInTheBuiltInEnglish()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o.AddValidator<Manufacturer, ManufacturerValidator>());

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.Errors.Should().Contain(error => error.Message == "Name is required.");
        }

        /// <summary>
        /// A validator may depend on other services — typically the validator of a nested object.
        /// </summary>
        [Fact]
        public async Task AddValidator_ResolvesTheValidatorsOwnDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidator<Manufacturer, ManufacturerValidator>()
                .AddValidator<CarModel, CarModelValidator>());

            var validator = Resolve<IValidator<CarModel>>(services);

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() });

            // Assert
            result.Errors.Should().Contain(error => error.Code == "Manufacturer.Name");
        }

        /// <summary>
        /// A validator which implements <see cref="IValidator{T}"/> by hand brings its own messages, so
        /// the registration leaves it alone rather than needing a method of its own.
        /// </summary>
        [Fact]
        public async Task AddValidator_RegistersAValidatorWhichImplementsTheInterfaceItself()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o.AddValidator<Manufacturer, HandWrittenManufacturerValidator>());

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            validator.Should().BeOfType<HandWrittenManufacturerValidator>();
            result.Errors.Should().ContainSingle().Which.Code.Should().Be("HandWritten");
        }

        /// <summary>
        /// The one-argument form: the validated type is read off the validator's own
        /// <see cref="IValidator{T}"/>, so it does not have to be named twice.
        /// </summary>
        [Fact]
        public async Task AddValidator_InfersTheValidatedType_FromTheValidator()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidator<ManufacturerValidator>()
                .AddValidator<CarModelValidator>());

            var validator = Resolve<IValidator<CarModel>>(services);

            // Act
            var result = await validator.ValidateAsync(new CarModel { Manufacturer = new Manufacturer() });

            // Assert
            validator.Should().BeOfType<CarModelValidator>();
            result.Errors.Should().Contain(error => error.Code == "Manufacturer.Name");
        }

        [Fact]
        public async Task AddValidatorsFromAssembly_RegistersEveryValidatorItFinds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly));

            var validator = Resolve<IValidator<Car>>(services);

            // Act
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            validator.Should().BeOfType<CarValidator>();
            result.Succeeded.Should().BeTrue();
        }

        /// <summary>
        /// A scanned validator is wired up just as fully as an explicitly registered one — its own
        /// dependencies included.
        /// </summary>
        [Fact]
        public async Task AddValidatorsFromAssembly_ResolvesTheDependenciesOfWhatItFinds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly));

            var validator = Resolve<IValidator<Car>>(services);
            var car = Cars.Car();
            car.Model!.Manufacturer!.Name = null;

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.Errors.Should().Contain(error => error.Code == "Model.Manufacturer.Name");
        }

        /// <summary>
        /// Scanning uses TryAdd, so a validator the host registered on purpose is not replaced by
        /// whatever the scan happens to find for the same type.
        /// </summary>
        [Fact]
        public void AddValidatorsFromAssembly_KeepsAnExplicitRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidator<Manufacturer, HandWrittenManufacturerValidator>()
                .AddValidatorsFromAssembly(typeof(CarValidator).Assembly));

            // Act
            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Assert
            validator.Should().BeOfType<HandWrittenManufacturerValidator>();
        }

        [Fact]
        public void AddValidator_WithATypeThatIsNotAValidator_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddNValidation(o => o.AddValidator(typeof(Car)));

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Scoped by default, because a validator may depend on something that is itself scoped and a
        /// longer-lived validator would capture it.
        /// </summary>
        [Fact]
        public void AddValidator_RegistersScoped_ByDefault()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o => o.AddValidator<ManufacturerValidator>());

            // Assert
            var descriptor = services.Single(service => service.ServiceType == typeof(IValidator<Manufacturer>));
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        /// <summary>
        /// A validator declares its rules once and never changes, so a host whose validators have
        /// nothing scoped to capture can pay for that construction once for the process.
        /// </summary>
        [Fact]
        public void ValidatorLifetime_Singleton_BuildsTheValidatorOnceForEveryScope()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.ValidatorLifetime = ServiceLifetime.Singleton;
                o.AddValidator<ManufacturerValidator>();
            });

            var serviceProvider = services.BuildServiceProvider();

            // Act
            using var first = serviceProvider.CreateScope();
            using var second = serviceProvider.CreateScope();

            var one = first.ServiceProvider.GetRequiredService<IValidator<Manufacturer>>();
            var other = second.ServiceProvider.GetRequiredService<IValidator<Manufacturer>>();

            // Assert
            one.Should().BeSameAs(other);
        }

        /// <summary>
        /// Nothing reaches the service collection until the delegate has finished, so a setting governs
        /// every validator however the delegate happens to be ordered.
        /// </summary>
        [Fact]
        public void ValidatorLifetime_AppliesToValidatorsRegisteredBeforeItWasSet()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o =>
            {
                o.AddValidator<ManufacturerValidator>();
                o.ValidatorLifetime = ServiceLifetime.Singleton;
            });

            // Assert
            services.Single(service => service.ServiceType == typeof(IValidator<Manufacturer>))
                .Lifetime.Should().Be(ServiceLifetime.Singleton);
        }

        /// <summary>
        /// One validator whose dependencies do not allow the default still overrides it, so a single
        /// exception does not force the whole host onto the shorter lifetime.
        /// </summary>
        [Fact]
        public void AddValidator_WithALifetime_OverridesTheDefault()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o =>
            {
                o.ValidatorLifetime = ServiceLifetime.Singleton;
                o.AddValidator<ManufacturerValidator>()
                    .AddValidator<CarModelValidator>(ServiceLifetime.Scoped);
            });

            // Assert
            services.Single(service => service.ServiceType == typeof(IValidator<Manufacturer>))
                .Lifetime.Should().Be(ServiceLifetime.Singleton);
            services.Single(service => service.ServiceType == typeof(IValidator<CarModel>))
                .Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        /// <summary>
        /// A scan follows the default like an explicit registration, and can be told otherwise.
        /// </summary>
        [Fact]
        public void AddValidatorsFromAssembly_WithALifetime_RegistersEveryValidatorWithIt()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o => o.AddValidatorsFromAssembly(
                ServiceLifetime.Singleton, typeof(CarValidator).Assembly));

            // Assert
            services.Where(service => service.ServiceType.IsGenericType &&
                                      service.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Should().NotBeEmpty()
                .And.OnlyContain(service => service.Lifetime == ServiceLifetime.Singleton);
        }

        /// <summary>
        /// A message provider is a lookup asked for text, so it is shared by default — a validator of
        /// any lifetime can then be handed it without capturing something shorter-lived.
        /// </summary>
        [Fact]
        public void MessageProvider_IsRegisteredAsASingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o => o.MessageProvider = typeof(TestMessageProvider));

            // Assert
            services.Single(service => service.ServiceType == typeof(IValidationMessageProvider))
                .Lifetime.Should().Be(ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Assembly.GetTypes has no documented order, so picking one of two validators for the same
        /// payload would be arbitrary and reproducible only by luck. The scan says so instead, naming
        /// both, and registers none of them.
        /// </summary>
        [Fact]
        public void AddValidatorsFromAssembly_WithTwoValidatorsForOnePayload_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddNValidation(
                o => o.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensionsTests).Assembly));

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*both validate*")
                .WithMessage("*AddValidator*");
        }

        /// <summary>
        /// A type is only checked when the container comes to resolve it, which is long after the
        /// mistake was made — so the property checks it where it is set.
        /// </summary>
        [Theory]
        [InlineData(typeof(Car))]
        [InlineData(typeof(IValidationMessageProvider))]
        public void MessageProvider_WithATypeThatCannotServe_Throws(Type messageProviderType)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddNValidation(o => o.MessageProvider = messageProviderType);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("MessageProvider");
        }

        /// <summary>
        /// The provider is built by the container, so it may take what it needs through its
        /// constructor rather than being handed over already built.
        /// </summary>
        [Fact]
        public async Task MessageProvider_IsBuiltByTheContainer()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(new MessageProviderDependency("built by the container"));
            services.AddNValidation(o =>
            {
                o.MessageProvider = typeof(DependentMessageProvider);
                o.AddValidator<Manufacturer, ManufacturerValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().OnlyContain(error => error.Message == "built by the container");
        }

        /// <summary>
        /// Singleton validators are handed the provider at construction, so the provider has to outlive
        /// them — the combination the default lifetimes are chosen to make work.
        /// </summary>
        [Fact]
        public async Task ValidatorLifetime_Singleton_ResolvesWithScopeValidationOn()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.ValidatorLifetime = ServiceLifetime.Singleton;
                o.MessageProvider = typeof(TestMessageProvider);
                o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
            });

            var serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

            // Act
            using var scope = serviceProvider.CreateScope();
            var validator = scope.ServiceProvider.GetRequiredService<IValidator<Car>>();
            var result = await validator.ValidateAsync(Cars.Car());

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        private static TService Resolve<TService>(IServiceCollection services)
            where TService : notnull
        {
            var serviceProvider = services.BuildServiceProvider();
            var scope = serviceProvider.CreateScope();

            return scope.ServiceProvider.GetRequiredService<TService>();
        }

        private sealed class HandWrittenManufacturerValidator : IValidator<Manufacturer>
        {
            public ValueTask<ValidationResult> ValidateAsync(Manufacturer instance, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(ValidationResult.FromValidationErrors(new ValidationError("HandWritten", "brings its own message")));
            }
        }

        private sealed record MessageProviderDependency(string Message);

        private sealed class DependentMessageProvider(MessageProviderDependency dependency) : IValidationMessageProvider
        {
            public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
            {
                return dependency.Message;
            }
        }

        private sealed class TestMessageProvider : IValidationMessageProvider
        {
            public const string Message = "message from the configured provider";

            public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
            {
                return Message;
            }
        }
    }
}
