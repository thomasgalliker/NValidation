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
        /// <remarks>
        /// The name is deliberately not <c>AddValidation</c>: ASP.NET Core ships one of its own with the
        /// same shape, and two would make every call ambiguous for a web host.
        /// <para>
        /// The defaults are registered with <c>TryAdd</c>, so a host which registered its own
        /// implementation first keeps it.
        /// </para>
        /// </remarks>
        public static IServiceCollection AddNValidation(this IServiceCollection services, Action<NValidationOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<IValidationMessageProvider, DefaultValidationMessageProvider>();

            var options = new NValidationOptions(services);

            configure?.Invoke(options);

            // After the delegate, so a setting applies to every validator however the delegate was
            // ordered — a property set at the bottom governs a validator added at the top.
            options.Apply();

            return services;
        }
    }
}
