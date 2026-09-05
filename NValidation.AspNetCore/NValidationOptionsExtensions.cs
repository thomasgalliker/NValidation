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
    public static class NValidationOptionsExtensions
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
        public static NValidationOptions AddValidationFilter(
            this NValidationOptions options,
            Action<ValidationFilterOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(options);

            // Configure runs every registered delegate, so calling this twice — directly, or once through
            // the IConfiguration overload — would put the filter in the pipeline twice and validate every
            // payload twice. The check also covers a host that added the filter to MvcOptions by hand.
            options.Services.Configure<MvcOptions>(mvcOptions =>
            {
                if (!mvcOptions.Filters.Any(IsValidationFilter))
                {
                    mvcOptions.Filters.Add<ValidationActionFilter>();
                }
            });

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
        public static NValidationOptions AddValidationFilter(
            this NValidationOptions options,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(configuration);

            options.Services.Configure<ValidationFilterOptions>(configuration);

            return options.AddValidationFilter();
        }

        private static bool IsValidationFilter(IFilterMetadata filter)
        {
            return filter is TypeFilterAttribute typeFilter &&
                   typeFilter.ImplementationType == typeof(ValidationActionFilter);
        }
    }
}
