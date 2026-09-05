using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// How the filter reaches MVC's options — and, mostly, that asking for it more than once does not
    /// mean every payload is validated more than once.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class NValidationOptionsExtensionsTests
    {
        [Fact]
        public void AddValidationFilter_AddsTheFilter()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o => o.AddValidationFilter());

            // Assert
            ValidationFiltersOf(services).Should().Be(1);
        }

        /// <summary>
        /// Configure runs every delegate it was given, so a second registration would put a second
        /// filter in the pipeline and validate every payload twice.
        /// </summary>
        [Fact]
        public void AddValidationFilter_CalledTwice_AddsTheFilterOnce()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNValidation(o =>
            {
                o.AddValidationFilter();
                o.AddValidationFilter();
            });

            // Assert
            ValidationFiltersOf(services).Should().Be(1);
        }

        /// <summary>
        /// The configuration overload adds the filter through the other one, which is the pairing most
        /// likely to be written by accident.
        /// </summary>
        [Fact]
        public void AddValidationFilter_WithConfigurationAfterTheDefault_AddsTheFilterOnce()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                [new KeyValuePair<string, string?>("MissingValidatorBehavior", "Log")]).Build();

            // Act
            services.AddNValidation(o =>
            {
                o.AddValidationFilter();
                o.AddValidationFilter(configuration);
            });

            // Assert
            ValidationFiltersOf(services).Should().Be(1);
        }

        /// <summary>
        /// A host that added the filter to MvcOptions by hand and then also asked for it here has still
        /// only asked for it once.
        /// </summary>
        [Fact]
        public void AddValidationFilter_WhenTheHostAlreadyAddedTheFilter_AddsItOnce()
        {
            // Arrange
            var services = new ServiceCollection();
            services.Configure<MvcOptions>(o => o.Filters.Add<ValidationActionFilter>());

            // Act
            services.AddNValidation(o => o.AddValidationFilter());

            // Assert
            ValidationFiltersOf(services).Should().Be(1);
        }

        /// <summary>
        /// The configuration overload still binds what it was given.
        /// </summary>
        [Fact]
        public void AddValidationFilter_WithConfiguration_BindsTheOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                [new KeyValuePair<string, string?>("MissingValidatorBehavior", "Throw")]).Build();

            // Act
            services.AddNValidation(o => o.AddValidationFilter(configuration));

            // Assert
            var filterOptions = services.BuildServiceProvider().GetRequiredService<IOptions<ValidationFilterOptions>>();
            filterOptions.Value.MissingValidatorBehavior.Should().Be(MissingValidatorBehavior.Throw);
        }

        private static int ValidationFiltersOf(IServiceCollection services)
        {
            var mvcOptions = services.BuildServiceProvider().GetRequiredService<IOptions<MvcOptions>>().Value;

            return mvcOptions.Filters.Count(filter =>
                filter is TypeFilterAttribute typeFilter &&
                typeFilter.ImplementationType == typeof(ValidationActionFilter));
        }
    }
}
