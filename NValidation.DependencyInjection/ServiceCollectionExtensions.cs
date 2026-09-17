using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NValidation
{
    /// <summary>
    /// Registers validation with the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds validation. Everything there is to configure — which validators are registered, and
        /// where their messages come from — is configured through <paramref name="configure"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// services.AddNValidation(o =>
        /// {
        ///     o.MessageProvider = typeof(ResourceValidationMessageProvider);
        ///     o.ValidatorLifetime = ServiceLifetime.Singleton;
        ///     o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
        /// });
        /// </code>
        /// Or naming each validator, which stays greppable:
        /// <code>
        /// services.AddNValidation(o =>
        /// {
        ///     o.MessageProvider = typeof(ResourceValidationMessageProvider);
        ///     o.AddValidator&lt;CarModelValidator&gt;()
        ///      .AddValidator&lt;CarValidator&gt;();
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddNValidation(this IServiceCollection services, Action<NValidationBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            return AddNValidationCore(services, configuration: null, configure);
        }

        /// <summary>
        /// Adds validation.
        /// </summary>
        public static IServiceCollection AddNValidation(this IServiceCollection services)
        {
            return AddNValidationCore(services, configuration: null, configure: null);
        }

        /// <summary>
        /// Adds validation, taking the settings which have a value in <paramref name="configuration"/>
        /// from there — so how much a validator reports, and how long one lives, can be changed without
        /// a rebuild.
        /// </summary>
        /// <example>
        /// <code>
        /// services.AddNValidation(builder.Configuration.GetSection("NValidation"));
        /// </code>
        /// against
        /// <code>
        /// {
        ///   "NValidation": {
        ///     "ValidatorLifetime": "Singleton",
        ///     "PromoteSafeValidatorsToSingleton": true,
        ///     "ValidationBehaviors": { "Class": "All", "Property": "StopAtFirstError" }
        ///   }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// <paramref name="configuration"/> is the section itself, not the root it was taken from, which
        /// is how <c>AddValidationFilter</c> takes one too. A key which is absent leaves that setting
        /// alone; a key whose value is not one of the permitted ones is refused here rather than being
        /// ignored, so a typo in appsettings.json fails at startup.
        /// <para>
        /// Which validators are registered stays in code: a scan names an assembly, and naming one in
        /// configuration would turn a typo into a payload that is silently never validated. The message
        /// provider stays in code for the same reason — a type name in a settings file is a refactor
        /// waiting to break.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// A key in <paramref name="configuration"/> carries a value the setting does not permit.
        /// </exception>
        public static IServiceCollection AddNValidation(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            return AddNValidationCore(services, configuration, configure: null);
        }

        /// <summary>
        /// The same, and then <paramref name="configure"/> — which runs afterwards, so what it names
        /// outranks what configuration said and what only code can express is still expressible.
        /// </summary>
        /// <inheritdoc cref="AddNValidation(IServiceCollection, IConfiguration)" path="/remarks"/>
        /// <inheritdoc cref="AddNValidation(IServiceCollection, IConfiguration)" path="/exception"/>
        public static IServiceCollection AddNValidation(
            this IServiceCollection services, IConfiguration configuration, Action<NValidationBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(configure);

            return AddNValidationCore(services, configuration, configure);
        }

        private static IServiceCollection AddNValidationCore(
            IServiceCollection services, IConfiguration? configuration, Action<NValidationBuilder>? configure)
        {
            ArgumentNullException.ThrowIfNull(services);

            // No provider is registered on the host's behalf: a validator the container builds and was
            // handed none falls through to the options passed to the call, then NValidationOptions.Default,
            // read while validating — so a default assigned after the container was built still reaches it.
            var options = new NValidationBuilder(services);

            if (configuration != null)
            {
                Bind(options, configuration);
            }

            configure?.Invoke(options);

            // After the delegate, so a setting applies to every validator however the delegate was
            // ordered — a property set at the bottom governs a validator added at the top.
            options.Apply();

            return services;
        }

        /// <summary>
        /// Takes the settings which configuration has a value for, leaving the rest alone.
        /// </summary>
        /// <remarks>
        /// Read key by key rather than through <c>IConfiguration.Bind</c>, which is annotated
        /// <c>RequiresUnreferencedCode</c> and would surface as a trimming warning in the build of every
        /// consumer publishing ahead of time. It also lets a bad value name itself.
        /// </remarks>
        private static void Bind(NValidationBuilder builder, IConfiguration configuration)
        {
            if (ParseEnum<ServiceLifetime>(configuration, "ValidatorLifetime") is { } validatorLifetime)
            {
                builder.ValidatorLifetime = validatorLifetime;
            }

            if (ParseBoolean(configuration, "PromoteSafeValidatorsToSingleton") is { } promote)
            {
                builder.PromoteSafeValidatorsToSingleton = promote;
            }

            if (ParseEnum<ValidationBehavior>(configuration, "ValidationBehaviors:Class") is { } classBehavior)
            {
                builder.ValidationBehaviors = builder.ValidationBehaviors with { Class = classBehavior };
            }

            if (ParseEnum<ValidationBehavior>(configuration, "ValidationBehaviors:Property") is { } propertyBehavior)
            {
                builder.ValidationBehaviors = builder.ValidationBehaviors with { Property = propertyBehavior };
            }
        }

        private static TValue? ParseEnum<TValue>(IConfiguration configuration, string key)
            where TValue : struct, Enum
        {
            var value = configuration[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!Enum.TryParse<TValue>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                throw new InvalidOperationException(
                    $"'{value}' is not a valid value for '{key}'. It has to be one of: " +
                    $"{string.Join(", ", Enum.GetNames<TValue>())}.");
            }

            return parsed;
        }

        private static bool? ParseBoolean(IConfiguration configuration, string key)
        {
            var value = configuration[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!bool.TryParse(value, out var parsed))
            {
                throw new InvalidOperationException(
                    $"'{value}' is not a valid value for '{key}'. It has to be true or false.");
            }

            return parsed;
        }
    }
}
