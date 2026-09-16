using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace NValidation.Tests.Extensions
{
    /// <summary>
    /// Covers the <see cref="IConfiguration"/> overloads of <c>AddNValidation</c>: which settings a host
    /// can move into appsettings.json, what a key it left out does, and what a key it got wrong does.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ServiceCollectionExtensionsConfigurationTests
    {
        [Theory]
        [InlineData("Singleton", ServiceLifetime.Singleton)]
        [InlineData("Transient", ServiceLifetime.Transient)]
        [InlineData("singleton", ServiceLifetime.Singleton)] // case-insensitive, as configuration keys are
        public void AddNValidation_BindsTheValidatorLifetime(string configured, ServiceLifetime expected)
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Configuration(("ValidatorLifetime", configured));

            // Act
            services.AddNValidation(configuration, b => b.AddValidator<Manufacturer, ManufacturerValidator>());

            // Assert
            Descriptor<IValidator<Manufacturer>>(services).Lifetime.Should().Be(expected);
        }

        [Fact]
        public async Task AddNValidation_BindsTheValidationBehaviors()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Configuration(("ValidationBehaviors:Property", "All"));

            services.AddNValidation(configuration, b => b.AddValidator<Manufacturer, TwoRuleValidator>());

            var validator = Resolve<IValidator<Manufacturer>>(services);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer());

            // Assert
            result.ShouldReport([
                new("Name", "Name must be Aurora."),
                new("Name", "Name must be spelled out.")]);
        }

        [Fact]
        public void AddNValidation_BindsPromoteSafeValidatorsToSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Configuration(("PromoteSafeValidatorsToSingleton", "true"));

            // Act
            services.AddNValidation(configuration, b => b.AddValidator<Manufacturer, ManufacturerValidator>());

            // Assert — promoted, though nothing named a lifetime and the default is Scoped
            Descriptor<IValidator<Manufacturer>>(services).Lifetime.Should().Be(ServiceLifetime.Singleton);
        }

        /// <summary>
        /// A key the host left out leaves that setting where it was, so a section naming one thing does
        /// not silently reset the others.
        /// </summary>
        [Fact]
        public void AddNValidation_WithAnEmptySection_LeavesEverySettingAlone()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(Configuration(), b => b.AddValidator<Manufacturer, ManufacturerValidator>());

            // Assert
            Descriptor<IValidator<Manufacturer>>(services).Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        /// <summary>
        /// The delegate runs after the section, so code outranks configuration.
        /// </summary>
        [Fact]
        public void AddNValidation_WithBoth_LetsTheDelegateOutrankTheConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Configuration(("ValidatorLifetime", "Singleton"));

            // Act
            services.AddNValidation(configuration, b =>
            {
                b.ValidatorLifetime = ServiceLifetime.Transient;
                b.AddValidator<Manufacturer, ManufacturerValidator>();
            });

            // Assert
            Descriptor<IValidator<Manufacturer>>(services).Lifetime.Should().Be(ServiceLifetime.Transient);
        }

        /// <summary>
        /// A typo is refused where it is written rather than ignored, and the message names what was
        /// permitted.
        /// </summary>
        [Fact]
        public void AddNValidation_WithAValueTheSettingDoesNotPermit_IsRefused()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Configuration(("ValidationBehaviors:Class", "StopAtFirstEror"));

            // Act
            var act = () => services.AddNValidation(configuration);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*StopAtFirstEror*ValidationBehaviors:Class*All, StopAtFirstError*");
        }

        [Fact]
        public void AddNValidation_WithANonBooleanFlag_IsRefused()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Configuration(("PromoteSafeValidatorsToSingleton", "yes"));

            // Act
            var act = () => services.AddNValidation(configuration);

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*yes*true or false*");
        }

        [Fact]
        public void AddNValidation_WithANullConfiguration_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddNValidation((IConfiguration)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        private static ServiceDescriptor Descriptor<TService>(IServiceCollection services)
        {
            return services.Last(descriptor => descriptor.ServiceType == typeof(TService));
        }

        private static TService Resolve<TService>(IServiceCollection services)
            where TService : notnull
        {
            var serviceProvider = services.BuildServiceProvider();
            var scope = serviceProvider.CreateScope();

            return scope.ServiceProvider.GetRequiredService<TService>();
        }

        private static IConfiguration Configuration(params (string Key, string Value)[] values)
        {
            return new StubConfiguration(values.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        /// <summary>
        /// The whole of what the binding asks of a configuration: one indexer. Written here rather than
        /// taken from <c>Microsoft.Extensions.Configuration</c> so the test project keeps its
        /// dependencies, and so that what the binding actually reads stays visible.
        /// </summary>
        private sealed class StubConfiguration(Dictionary<string, string> values) : IConfiguration
        {
            public string? this[string key]
            {
                get => values.GetValueOrDefault(key);
                set => throw new NotSupportedException();
            }

            public IEnumerable<IConfigurationSection> GetChildren() => throw new NotSupportedException();

            public IChangeToken GetReloadToken() => throw new NotSupportedException();

            public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
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
    }
}
