using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

            return AddNValidationCore(services, configure);
        }

        /// <summary>
        /// Adds validation.
        /// </summary>
        public static IServiceCollection AddNValidation(this IServiceCollection services)
        {
            return AddNValidationCore(services, configure: null);
        }

        private static IServiceCollection AddNValidationCore(IServiceCollection services, Action<NValidationBuilder>? configure)
        {
            ArgumentNullException.ThrowIfNull(services);

            // The base layer, not a built-in English default written over the top of it: what this
            // resolves to is whatever NValidationOptions.Default says the first time anything asks,
            // which is the built-in provider until a host configures otherwise. Still TryAdd, so a host
            // which registered its own provider before this call keeps it, and the Replace in
            // NValidationBuilder.Apply still outranks both.
            services.TryAddSingleton<IValidationMessageProvider>(_ => NValidationOptions.DefaultInUse().MessageProvider);

            var options = new NValidationBuilder(services);

            configure?.Invoke(options);

            // After the delegate, so a setting applies to every validator however the delegate was
            // ordered — a property set at the bottom governs a validator added at the top.
            options.Apply();

            return services;
        }
    }
}
