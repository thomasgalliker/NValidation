using System.Reflection;
using NValidation.TestData.Ambiguous;

namespace NValidation.DependencyInjection.Tests
{
    /// <summary>
    /// Covers how validation is wired up: the defaults, how a host replaces them, and — most
    /// importantly — that a registered validator is actually handed the configured message provider.
    /// Without that last one a validator would quietly answer in the core's built-in English.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ServiceCollectionExtensionsTests
    {
        /// <summary>
        /// The one assembly carrying two validators for a single payload. It exists as an assembly of
        /// its own because every other scan target is also scanned by tests which assert that a scan
        /// succeeds, and by the sample application.
        /// </summary>
        private static Assembly AmbiguousAssembly { get; } = typeof(AmbiguousPayload).Assembly;

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
            result.ShouldReport([
                new("Name", "message from the configured provider"),
                new("CountryCode", "message from the configured provider")]);
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
            result.ShouldReport([
                new("Name", "Name is required."),
                new("CountryCode", "CountryCode is required.")]);
        }

        [Fact]
        public async Task AddValidator_ResolvesTheValidatorsOwnDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidator<Manufacturer, ManufacturerValidator>()
                .AddValidator<CarModel, CarModelValidator>());

            var validator = Resolve<IValidator<CarModel>>(services);

            // Only the nested manufacturer is wrong, so what comes back is what the dependency reported.
            var carModel = Cars.CarModel();
            carModel.Manufacturer!.Name = null;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            result.ShouldReport("Manufacturer.Name", "Name is required.");
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
            result.ShouldReport("HandWritten", "brings its own message");
        }

        [Fact]
        public async Task AddValidator_InfersTheValidatedType_FromTheValidator()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidator<ManufacturerValidator>()
                .AddValidator<CarModelValidator>());

            var validator = Resolve<IValidator<CarModel>>(services);

            // Only the nested manufacturer is wrong, so what comes back is what the dependency reported.
            var carModel = Cars.CarModel();
            carModel.Manufacturer!.Name = null;

            // Act
            var result = await validator.ValidateAsync(carModel);

            // Assert
            validator.Should().BeOfType<CarModelValidator>();
            result.ShouldReport("Manufacturer.Name", "Name is required.");
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
            result.ShouldReport("Model.Manufacturer.Name", "Name is required.");
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

        /// <summary>
        /// Which is what the exception's own remedy says to do about it. The scan passes over a payload
        /// an explicit registration already covers, rather than complaining about a choice that was made
        /// — TryAdd would have kept that registration anyway, so refusing the scan only made the advice
        /// untrue.
        /// </summary>
        [Fact]
        public async Task AddValidatorsFromAssembly_WithAnExplicitRegistration_PassesOverThatPayload()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidator<FirstAmbiguousValidator>()
                .AddValidatorsFromAssembly(AmbiguousAssembly));

            var validator = Resolve<IValidator<AmbiguousPayload>>(services);

            // Act
            var result = await validator.ValidateAsync(new AmbiguousPayload());

            // Assert
            validator.Should().BeOfType<FirstAmbiguousValidator>();
            result.ShouldReport("First", "Name is required.");
        }

        /// <summary>
        /// An ambiguous scan is settled by naming the validator to keep, and it stays settled when that
        /// name is written after the scan rather than before it. The scan collects what it found and
        /// leaves the choosing until the whole delegate has run, so it cannot refuse a choice the next
        /// line was about to make.
        /// </summary>
        [Fact]
        public async Task AddValidatorsFromAssembly_WithAnExplicitRegistrationAfterIt_PassesOverThatPayload()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidatorsFromAssembly(AmbiguousAssembly)
                .AddValidator<FirstAmbiguousValidator>());

            var validator = Resolve<IValidator<AmbiguousPayload>>(services);

            // Act
            var result = await validator.ValidateAsync(new AmbiguousPayload());

            // Assert
            validator.Should().BeOfType<FirstAmbiguousValidator>();
            result.ShouldReport("First", "Name is required.");
        }

        /// <summary>
        /// The same, written the other way round. Order independence is the whole point of deferring the
        /// registrations to <c>Apply()</c>, and overriding a scanned validator is the case that needs it
        /// most — a host which scans an assembly and then names its own replacement has said something
        /// unambiguous, whichever line it sits on.
        /// </summary>
        [Fact]
        public void AddValidatorsFromAssembly_KeepsAnExplicitRegistration_WhateverOrderItWasWrittenIn()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidatorsFromAssembly(typeof(CarValidator).Assembly)
                .AddValidator<Manufacturer, HandWrittenManufacturerValidator>());

            // Act
            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Assert
            validator.Should().BeOfType<HandWrittenManufacturerValidator>();
        }

        /// <summary>
        /// Two explicit registrations naming different validators for one payload is a contradiction the
        /// caller has to settle, exactly as an ambiguous scan is. Keeping the first silently would make
        /// the second line read as if it had done something.
        /// </summary>
        [Fact]
        public void AddValidator_WithTwoDifferentValidatorsForOnePayload_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddNValidation(o => o
                .AddValidator<FirstAmbiguousValidator>()
                .AddValidator<SecondAmbiguousValidator>());

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*both validate '{typeof(AmbiguousPayload)}'*");
        }

        [Fact]
        public void AddValidator_WithTheSameValidatorTwice_Registers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o
                .AddValidator<FirstAmbiguousValidator>()
                .AddValidator<FirstAmbiguousValidator>());

            // Act
            var validator = Resolve<IValidator<AmbiguousPayload>>(services);

            // Assert
            validator.Should().BeOfType<FirstAmbiguousValidator>();
        }

        /// <summary>
        /// The whole point: a validator whose only dependencies are other validators can be shared, and
        /// then its rules are built once for the process rather than once per request.
        /// </summary>
        [Fact]
        public void PromoteSafeValidatorsToSingleton_SharesAValidatorWithNoScopedDependency()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.PromoteSafeValidatorsToSingleton = true;
                o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
            });

            // Act
            var descriptor = services.Single(service => service.ServiceType == typeof(IValidator<Car>));

            // Assert
            descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        }

        [Fact]
        public void PromoteSafeValidatorsToSingleton_LeavesAValidatorWithAScopedDependencyAlone()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<MessageProviderDependency>();
            services.AddNValidation(o =>
            {
                o.PromoteSafeValidatorsToSingleton = true;
                o.AddValidator<Car, ScopedDependencyCarValidator>();
            });

            // Act
            var descriptor = services.Single(service => service.ServiceType == typeof(IValidator<Car>));

            // Assert
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        /// <summary>
        /// A validator offering the container a choice of constructors cannot be constructed at all, and
        /// that is settled while the application is starting rather than on the first request that needs
        /// it — the constructor is selected once, at registration.
        /// </summary>
        [Fact]
        public void AddValidator_WithAmbiguousConstructors_ThrowsAtStartup()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<MessageProviderDependency>();

            // Act
            var act = () => services.AddNValidation(o => o.AddValidator<Car, TwoConstructorCarValidator>());

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*constructor*");
        }

        /// <summary>
        /// On unless turned off: a validator which is provably safe to share is shared, because
        /// rebuilding rules that never change is the largest cost this library imposes on a request.
        /// </summary>
        [Fact]
        public void PromoteSafeValidatorsToSingleton_ByDefault_PromotesASafeValidator()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly));

            // Act
            var descriptor = services.Single(service => service.ServiceType == typeof(IValidator<Car>));

            // Assert
            descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        }

        [Fact]
        public void PromoteSafeValidatorsToSingleton_WhenTurnedOff_LeavesTheLifetimeAlone()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.PromoteSafeValidatorsToSingleton = false;
                o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
            });

            // Act
            var descriptor = services.Single(service => service.ServiceType == typeof(IValidator<Car>));

            // Assert
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public async Task PromoteSafeValidatorsToSingleton_StillValidates()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.PromoteSafeValidatorsToSingleton = true;
                o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
            });

            var validator = Resolve<IValidator<Car>>(services);

            // Act
            var result = await validator.ValidateAsync(new Car());

            // Assert
            result.Errors.Should().NotBeEmpty();
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
        /// Scoped is what a validator falls back to, because it may depend on something that is itself
        /// scoped and a longer-lived validator would capture it — but one that provably depends on no
        /// such thing is promoted, so the fallback only shows where promotion declined.
        /// </summary>
        [Fact]
        public void AddValidator_PromotesASafeValidator_ByDefault()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o => o.AddValidator<ManufacturerValidator>());

            // Assert
            var descriptor = services.Single(service => service.ServiceType == typeof(IValidator<Manufacturer>));
            descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddValidator_WithoutPromotion_RegistersScoped_ByDefault()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o =>
            {
                o.PromoteSafeValidatorsToSingleton = false;
                o.AddValidator<ManufacturerValidator>();
            });

            // Assert
            var descriptor = services.Single(service => service.ServiceType == typeof(IValidator<Manufacturer>));
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

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
        /// both, and registers none of them. The message names the payload rather than the
        /// <c>IValidator&lt;T&gt;</c> behind it, which is not what the reader wrote a validator for.
        /// </summary>
        [Fact]
        public void AddValidatorsFromAssembly_WithTwoValidatorsForOnePayload_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddNValidation(o => o.AddValidatorsFromAssembly(AmbiguousAssembly));

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*both validate '{typeof(AmbiguousPayload)}'*")
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
            result.ShouldReport([
                new("Name", "built by the container"),
                new("CountryCode", "built by the container")]);
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

        /// <summary>
        /// Options given to one call are the level above the registration, so a request answered in its
        /// own language gets its own wording even from a validator the container built.
        /// </summary>
        /// <remarks>
        /// The registration hands its provider to every validator it constructs, and a host which
        /// configured none still has one handed to it. It lands in the registration's own slot, not
        /// the one a validator's own declaration uses, which is what leaves these options something
        /// to outrank.
        /// </remarks>
        [Fact]
        public async Task ValidateAsync_WithOptions_OutranksTheRegisteredMessageProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.MessageProvider = typeof(TestMessageProvider);
                o.AddValidator<Manufacturer, ManufacturerValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            var options = new NValidationOptions { MessageProvider = new PerCallMessageProvider() };

            // Act
            var result = await validator.ValidateAsync(new Manufacturer(), options);

            // Assert
            result.ShouldReport([
                new("Name", "message from the options passed to the call"),
                new("CountryCode", "message from the options passed to the call")]);
        }

        /// <summary>
        /// And options which named only a behavior leave the messages to the level below, rather than
        /// putting the built-in English back over the provider the registration configured.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOptionsNamingOnlyBehaviors_KeepsTheRegisteredMessageProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.MessageProvider = typeof(TestMessageProvider);
                o.AddValidator<Manufacturer, ManufacturerValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            var options = new NValidationOptions { ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError } };

            // Act
            var result = await validator.ValidateAsync(new Manufacturer(), options);

            // Assert
            result.ShouldReport("Name", "message from the configured provider");
        }

        /// <summary>
        /// A validator which declared a provider for itself keeps it, exactly as it keeps an axis it
        /// declared: what the registration hands over is the level underneath, not a replacement.
        /// </summary>
        [Fact]
        public async Task AddValidator_DoesNotOverwriteTheValidatorsOwnMessageProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.MessageProvider = typeof(TestMessageProvider);
                o.AddValidator<Manufacturer, OwnMessagesManufacturerValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport("Name", "message from the validator's own provider");
        }

        /// <summary>
        /// The registration's setting reaches a validator which declared nothing of its own, on each
        /// axis independently — a validator built by the container is handed it exactly as it is handed
        /// the configured message provider.
        /// </summary>
        [Theory]
        [InlineData(null, "Name", "CountryCode")] // the defaults: every property, one message each
        [InlineData(ValidationBehavior.StopAtFirstError, "Name")]
        public async Task ValidationBehaviors_Class_ReachesTheValidator(
            ValidationBehavior? classBehavior,
            params string[] expectedCodes)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.ValidationBehaviors = new() { Class = classBehavior };
                o.AddValidator<Manufacturer, ManufacturerValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport(expectedCodes.Select(ExpectedError.Any));
        }

        [Theory]
        [InlineData(null, "Name", "CountryCode")] // one message per property, so the blank name reports NotEmpty alone
        [InlineData(ValidationBehavior.All, "Name", "CountryCode", "CountryCode")] // the blank country code also fails its exact length
        public async Task ValidationBehaviors_Property_ReachesTheValidator(
            ValidationBehavior? propertyBehavior,
            params string[] expectedCodes)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.ValidationBehaviors = new() { Property = propertyBehavior };
                o.AddValidator<Manufacturer, ManufacturerValidator>();
            });

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { CountryCode = " " });

            // Assert
            result.ShouldReport(expectedCodes.Select(ExpectedError.Any));
        }

        /// <summary>
        /// A validator which named an axis for itself keeps it, while the axis it did not name still
        /// takes what the registration configured — the two levels layer per axis rather than wholesale.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_OfTheValidator_WinPerAxis()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                // Both stopping, so a validator which overrules only one of them is visible.
                o.ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError, Property = ValidationBehavior.StopAtFirstError };

                o.AddValidator<Car, ReportsEveryPropertyCarValidator>();
            });

            var validator = Resolve<IValidator<Car>>(services);

            // Act
            var result = await validator.ValidateAsync(new Car { Vin = "      " });

            // Assert
            result.ShouldReport([
                new("Vin", "Vin is required."),
                new("RegistrationPlate", "RegistrationPlate is required.")]);
        }

        /// <summary>
        /// An element builder is built where it is declared rather than by the container, so the
        /// registration hands it nothing directly — but the validator it was declared in was handed the
        /// setting, resolved it, and what a validator resolves is inherited by what it composes.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_ReachAnElementBuilder_ThroughTheValidatorTheyWereHandedTo()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.ValidationBehaviors = new() { Property = ValidationBehavior.All };
                o.AddValidator<Car, ServiceHistoryPlainElementChainValidator>();
            });

            var validator = Resolve<IValidator<Car>>(services);

            // Blank and longer than the cap, so the entry's chain breaks both of its rules.
            var car = new Car { ServiceHistory = [new ServiceRecord { Workshop = "      " }] };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            result.ShouldReport([
                new("ServiceHistory[0].Workshop", "Workshop is required."),
                new("ServiceHistory[0].Workshop", "Workshop must not exceed 3 characters.")]);
        }

        /// <summary>
        /// The chain axis layers the same way the run axis does. Tested on its own because the run-axis
        /// test cannot see it: there the resolved chain behaviour coincides with the built-in default,
        /// so it would pass even if the two levels were consulted in the wrong order.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_OfTheValidator_WinOnThePropertyAxis()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.ValidationBehaviors = new() { Property = ValidationBehavior.All };
                o.AddValidator<Car, StopsItsChainsCarValidator>();
            });

            var validator = Resolve<IValidator<Car>>(services);

            // Only the VIN is wrong, and wrong in both the ways its chain asks about, so what the
            // result holds is decided by the chain axis alone.
            var car = new Car { Vin = "      ", RegistrationPlate = "AB 123" };

            // Act
            var result = await validator.ValidateAsync(car);

            // Assert
            // one message only: the validator's own word outranks the registration's
            result.ShouldReport("Vin", "Vin is required.");
        }

        /// <summary>
        /// The behaviours are copied while the registrations are written, so options a caller kept hold
        /// of and changed afterwards cannot reach back into validators that were already registered.
        /// </summary>
        [Fact]
        public async Task ValidationBehaviors_ChangedAfterRegistration_DoNotReachTheValidator()
        {
            // Arrange
            var services = new ServiceCollection();
            NValidationBuilder? kept = null;

            services.AddNValidation(o =>
            {
                kept = o;
                o.AddValidator<Car, StopsItsChainsCarValidator>();
            });

            kept!.ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError };

            var validator = Resolve<IValidator<Car>>(services);

            // Act
            var result = await validator.ValidateAsync(new Car { Vin = "      " });

            // Assert
            result.ShouldReport([
                new("Vin", "Vin is required."),
                new("RegistrationPlate", "RegistrationPlate is required.")]);
        }

        /// <summary>
        /// Names the chain axis for itself and leaves the run axis inheriting.
        /// </summary>
        private sealed class StopsItsChainsCarValidator : Validator<Car>
        {
            public StopsItsChainsCarValidator()
            {
                this.ValidationBehaviors = new() { Property = ValidationBehavior.StopAtFirstError };

                this.Property(c => c.Vin).NotEmpty().MaximumLength(3);
                this.Property(c => c.RegistrationPlate).NotEmpty();
            }
        }

        /// <summary>
        /// Overrules the registration on the run axis only, so its chain axis still stops and each of
        /// its properties reports once.
        /// </summary>
        private sealed class ReportsEveryPropertyCarValidator : Validator<Car>
        {
            public ReportsEveryPropertyCarValidator()
            {
                this.ValidationBehaviors = new() { Class = ValidationBehavior.All };

                this.Property(c => c.Vin).NotEmpty().MaximumLength(3);
                this.Property(c => c.RegistrationPlate).NotEmpty();
            }
        }

        /// <summary>
        /// Element rules with nothing declared about their behaviour, so a test can prove that an element
        /// builder inherits what the registration configured on the validator it was declared in.
        /// </summary>
        /// <remarks>
        /// A named class rather than an inline one because the container constructs it by type: there is
        /// no instance for the registration to be handed.
        /// </remarks>
        private sealed class ServiceHistoryPlainElementChainValidator : Validator<Car>
        {
            public ServiceHistoryPlainElementChainValidator()
            {
                this.Property(c => c.ServiceHistory)
                    .ForEach(record => record.Property(r => r.Workshop).NotEmpty().MaximumLength(3));
            }
        }

        private static TService Resolve<TService>(IServiceCollection services)
            where TService : notnull
        {
            var serviceProvider = services.BuildServiceProvider();
            var scope = serviceProvider.CreateScope();

            return scope.ServiceProvider.GetRequiredService<TService>();
        }

        /// <summary>
        /// Holds something registered as scoped, which is exactly what a shared validator must not do.
        /// </summary>
        private sealed class ScopedDependencyCarValidator : Validator<Car>
        {
            public ScopedDependencyCarValidator(MessageProviderDependency dependency)
            {
                ArgumentNullException.ThrowIfNull(dependency);

                this.Property(c => c.Vin).NotEmpty();
            }
        }

        /// <summary>
        /// Offers the container a choice, so nothing can be concluded about what it would hold.
        /// </summary>
        private sealed class TwoConstructorCarValidator : Validator<Car>
        {
            public TwoConstructorCarValidator()
            {
                this.Property(c => c.Vin).NotEmpty();
            }

            public TwoConstructorCarValidator(MessageProviderDependency dependency)
                : this()
            {
                ArgumentNullException.ThrowIfNull(dependency);
            }
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
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return dependency.Message;
            }
        }

        private sealed class TestMessageProvider : IValidationMessageProvider
        {
            public const string Message = "message from the configured provider";

            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return Message;
            }
        }

        private sealed class PerCallMessageProvider : IValidationMessageProvider
        {
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return "message from the options passed to the call";
            }
        }

        private sealed class OwnMessageProvider : IValidationMessageProvider
        {
            public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
            {
                return "message from the validator's own provider";
            }
        }

        /// <summary>
        /// Named rather than declared inline because the container constructs it by type.
        /// </summary>
        private sealed class OwnMessagesManufacturerValidator : Validator<Manufacturer>
        {
            public OwnMessagesManufacturerValidator()
            {
                this.ValidationMessageProvider = new OwnMessageProvider();

                this.Property(m => m.Name).NotEmpty();
            }
        }
    }
}
