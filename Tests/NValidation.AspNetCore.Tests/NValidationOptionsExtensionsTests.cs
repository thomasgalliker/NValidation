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
        /// The order the other way round, which is the one a host actually writes: AddNValidation
        /// configures MVC first, and AddControllers — registered after it — runs its own delegate
        /// afterwards. Checking inside a Configure delegate cannot see that one, so the check happens
        /// after every delegate has run.
        /// </summary>
        [Fact]
        public void AddValidationFilter_WhenTheHostAddsTheFilterAfterwards_AddsItOnce()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o.AddValidationFilter());

            // Act
            services.Configure<MvcOptions>(o => o.Filters.Add<ValidationActionFilter>());

            // Assert
            ValidationFiltersOf(services).Should().Be(1);
        }

        /// <summary>
        /// The filter can also reach MvcOptions as an instance or through the container, and two of it
        /// validate every payload twice however it got there.
        /// </summary>
        [Fact]
        public void AddValidationFilter_WhenTheHostAddsTheFilterAsAService_AddsItOnce()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o => o.AddValidationFilter());

            // Act
            services.Configure<MvcOptions>(o => o.Filters.Add(new ServiceFilterAttribute(typeof(ValidationActionFilter))));

            // Assert
            AllValidationFiltersOf(services).Should().Be(1);
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
            return FiltersOf(services).Count(filter =>
                filter is TypeFilterAttribute typeFilter &&
                typeFilter.ImplementationType == typeof(ValidationActionFilter));
        }

        /// <summary>
        /// Every shape the filter can be registered in, so a duplicate cannot hide behind a different one.
        /// </summary>
        private static int AllValidationFiltersOf(IServiceCollection services)
        {
            return FiltersOf(services).Count(filter => filter switch
            {
                ValidationActionFilter => true,
                TypeFilterAttribute typeFilter => typeFilter.ImplementationType == typeof(ValidationActionFilter),
                ServiceFilterAttribute serviceFilter => serviceFilter.ServiceType == typeof(ValidationActionFilter),
                _ => false,
            });
        }

        private static IEnumerable<IFilterMetadata> FiltersOf(IServiceCollection services)
        {
            return services.BuildServiceProvider().GetRequiredService<IOptions<MvcOptions>>().Value.Filters;
        }
    }
}
