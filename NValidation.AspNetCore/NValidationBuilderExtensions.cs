using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NValidation.AspNetCore
{
    /// <summary>
    /// The ASP.NET Core parts of the configuration, so a web host configures this library in the one
    /// place the rest of it is configured.
    /// </summary>
    public static class NValidationBuilderExtensions
    {
        /// <summary>
        /// Adds <see cref="ValidationActionFilter"/> to MVC, so every controller action validates its
        /// body- and form-bound payloads before it runs.
        /// </summary>
        /// <example>
        /// <code>
        /// services.AddNValidation(o =>
        /// {
        ///     o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
        ///     o.AddValidationFilter(f => f.MissingValidatorBehavior = MissingValidatorBehavior.Throw);
        /// });
        /// </code>
        /// </example>
        /// <remarks>
        /// The equivalent of adding the filter to <see cref="MvcOptions.Filters"/> by hand. Minimal-API
        /// endpoints are unaffected — there is no action for a filter to run in front of — and validate
        /// by calling their validator.
        /// </remarks>
        public static NValidationBuilder AddValidationFilter(
            this NValidationBuilder options,
            Action<ValidationFilterOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            return AddValidationFilterCore(options, configure);
        }

        /// <summary>
        /// The same, leaving <see cref="ValidationFilterOptions"/> at its defaults.
        /// </summary>
        public static NValidationBuilder AddValidationFilter(this NValidationBuilder options)
        {
            return AddValidationFilterCore(options, configure: null);
        }

        private static NValidationBuilder AddValidationFilterCore(
            NValidationBuilder options,
            Action<ValidationFilterOptions>? configure)
        {
            ArgumentNullException.ThrowIfNull(options);

            options.Services.Configure<MvcOptions>(mvcOptions => mvcOptions.Filters.Add<ValidationActionFilter>());

            // Calling this twice — directly, or once through the IConfiguration overload — would put the
            // filter in the pipeline twice, and validating every payload twice reports every failure
            // twice. Deduplicated after every Configure delegate has run rather than inside one of them:
            // the delegates run in the order they were registered, so a host which adds the filter to
            // MvcOptions itself is only visible from here if it happened to do so first.
            options.Services.PostConfigure<MvcOptions>(RemoveDuplicateValidationFilters);

            if (configure != null)
            {
                options.Services.Configure(configure);
            }

            return options;
        }

        /// <summary>
        /// The same, binding <see cref="ValidationFilterOptions"/> from configuration so the behaviour
        /// can be changed without a rebuild.
        /// </summary>
        public static NValidationBuilder AddValidationFilter(
            this NValidationBuilder options,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(configuration);

            options.Services.Configure<ValidationFilterOptions>(configuration);

            return options.AddValidationFilter();
        }

        /// <summary>
        /// Keeps the first <see cref="ValidationActionFilter"/> in the pipeline and drops the rest.
        /// </summary>
        private static void RemoveDuplicateValidationFilters(MvcOptions mvcOptions)
        {
            var first = -1;

            for (var i = 0; i < mvcOptions.Filters.Count; i++)
            {
                if (IsValidationFilter(mvcOptions.Filters[i]))
                {
                    first = i;
                    break;
                }
            }

            if (first < 0)
            {
                return;
            }

            // Backwards, so removing an entry cannot move one that has not been looked at yet.
            for (var i = mvcOptions.Filters.Count - 1; i > first; i--)
            {
                if (IsValidationFilter(mvcOptions.Filters[i]))
                {
                    mvcOptions.Filters.RemoveAt(i);
                }
            }
        }

        /// <remarks>
        /// Covers the three shapes a host can register the filter in: by type
        /// (<c>Filters.Add&lt;ValidationActionFilter&gt;()</c>), through the container
        /// (<c>Filters.Add(new ServiceFilterAttribute(...))</c>), or as an instance.
        /// </remarks>
        private static bool IsValidationFilter(IFilterMetadata filter)
        {
            return filter switch
            {
                ValidationActionFilter => true,
                TypeFilterAttribute typeFilter => typeFilter.ImplementationType == typeof(ValidationActionFilter),
                ServiceFilterAttribute serviceFilter => serviceFilter.ServiceType == typeof(ValidationActionFilter),
                _ => false,
            };
        }
    }
}
