using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// Configures validation: which validators are registered, how long they live, and where their
    /// messages come from.
    /// </summary>
    /// <remarks>
    /// Handed to the delegate passed to
    /// <see cref="ServiceCollectionExtensions.AddNValidation(IServiceCollection, Action{NValidationOptions})"/>,
    /// so everything this library needs is configured in one place. The settings are properties and the
    /// registrations are methods; nothing reaches the service collection until the delegate has run, so
    /// a setting applies to every validator however the delegate is ordered.
    /// </remarks>
    public sealed class NValidationOptions
    {
        /// <summary>
        /// Constructor selection is reflection, and the answer is the same every time; a validator
        /// resolved per scope would otherwise pay for it on every request.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, ObjectFactory> Factories = new();

        /// <summary>
        /// What each <c>AddValidator</c> asked for, applied once the delegate has run. A <c>null</c>
        /// lifetime means "whatever <see cref="ValidatorLifetime"/> ends up being".
        /// </summary>
        private readonly List<(Type ValidatedType, Type ValidatorType, ServiceLifetime? Lifetime)> registrations = [];

        private Type? messageProvider;

        internal NValidationOptions(IServiceCollection services)
        {
            this.Services = services;
        }

        /// <summary>
        /// The collection being configured, for a registration this type has no method for — and what an
        /// integration package extends to add its own configuration here.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// The lifetime validators are registered with unless one is named on the call itself.
        /// <see cref="ServiceLifetime.Scoped"/> by default, because a validator may take a dependency
        /// that is itself scoped — the database an async uniqueness rule asks — and a longer-lived
        /// validator would capture it.
        /// </summary>
        /// <remarks>
        /// A validator built on <see cref="Validator{T}"/> declares its rules in its constructor and
        /// never changes afterwards, so where its dependencies allow it,
        /// <see cref="ServiceLifetime.Singleton"/> pays for that construction once for the process
        /// rather than once per scope. Choose it when the validators being registered are safe to
        /// share: no scoped dependencies, no mutable state. A single validator that cannot follow it
        /// still names its own lifetime on its own call.
        /// </remarks>
        public ServiceLifetime ValidatorLifetime { get; set; } = ServiceLifetime.Scoped;

        /// <summary>
        /// The <see cref="IValidationMessageProvider"/> rules take their message texts from — typically
        /// the application's resources, served in the language of the current request. Left unset, the
        /// built-in English is used.
        /// </summary>
        /// <remarks>
        /// Built by the container, so a provider may take whatever it depends on through its
        /// constructor, and registered as a singleton: a provider is a lookup asked for text, it has to
        /// be thread-safe anyway because validators run concurrently, and being longer-lived than every
        /// validator is what lets validators of any lifetime be handed it. Resolve the language while
        /// the message is produced — a <see cref="Func{TResult}"/> over a resource — rather than in the
        /// constructor.
        /// <para>
        /// A provider that genuinely cannot be shared is registered through <see cref="Services"/>
        /// instead, at the cost of forcing every validator that uses it to be scoped as well.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// The type does not implement <see cref="IValidationMessageProvider"/>, or cannot be
        /// constructed.
        /// </exception>
        public Type? MessageProvider
        {
            get => this.messageProvider;

            set
            {
                if (value != null && !typeof(IValidationMessageProvider).IsAssignableFrom(value))
                {
                    throw new ArgumentException(
                        $"Type '{value}' does not implement {nameof(IValidationMessageProvider)}.",
                        nameof(this.MessageProvider));
                }

                if (value != null && (value.IsAbstract || value.IsInterface))
                {
                    throw new ArgumentException(
                        $"Type '{value}' cannot be constructed, so it cannot serve as the message provider.",
                        nameof(this.MessageProvider));
                }

                this.messageProvider = value;
            }
        }

        /// <summary>
        /// Registers a validator, for every <see cref="IValidator{T}"/> it implements.
        /// </summary>
        /// <remarks>
        /// A validator deriving from <see cref="Validator{T}"/> is handed the configured message
        /// provider, so its own constructor only declares rules and whatever it genuinely depends on
        /// (e.g. the validator of a nested object). One implementing <see cref="IValidator{T}"/> by hand
        /// brings its own messages and is left alone.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <typeparamref name="TValidator"/> does not implement <see cref="IValidator{T}"/>.
        /// </exception>
        public NValidationOptions AddValidator<TValidator>()
            where TValidator : class
        {
            return this.AddValidator(typeof(TValidator), lifetime: null);
        }

        /// <inheritdoc cref="AddValidator{TValidator}()" path="/summary"/>
        /// <remarks>
        /// For the one validator whose dependencies do not allow <see cref="ValidatorLifetime"/>.
        /// </remarks>
        public NValidationOptions AddValidator<TValidator>(ServiceLifetime lifetime)
            where TValidator : class
        {
            return this.AddValidator(typeof(TValidator), (ServiceLifetime?)lifetime);
        }

        /// <summary>
        /// The same, naming the validated type as well — so the compiler checks that
        /// <typeparamref name="TValidator"/> really does validate <typeparamref name="TInstance"/>.
        /// </summary>
        public NValidationOptions AddValidator<TInstance, TValidator>()
            where TValidator : class, IValidator<TInstance>
        {
            return this.Add(typeof(IValidator<TInstance>), typeof(TValidator), lifetime: null);
        }

        /// <inheritdoc cref="AddValidator{TInstance, TValidator}()" path="/summary"/>
        /// <inheritdoc cref="AddValidator{TValidator}(ServiceLifetime)" path="/remarks"/>
        public NValidationOptions AddValidator<TInstance, TValidator>(ServiceLifetime lifetime)
            where TValidator : class, IValidator<TInstance>
        {
            return this.Add(typeof(IValidator<TInstance>), typeof(TValidator), lifetime);
        }

        /// <summary>
        /// The form a scan uses, where the validator's type is only known at runtime.
        /// </summary>
        /// <inheritdoc cref="AddValidator{TValidator}()" path="/exception"/>
        public NValidationOptions AddValidator(Type validatorType)
        {
            return this.AddValidator(validatorType, lifetime: null);
        }

        /// <inheritdoc cref="AddValidator(Type)" path="/summary"/>
        /// <inheritdoc cref="AddValidator{TValidator}(ServiceLifetime)" path="/remarks"/>
        /// <inheritdoc cref="AddValidator{TValidator}()" path="/exception"/>
        public NValidationOptions AddValidator(Type validatorType, ServiceLifetime lifetime)
        {
            return this.AddValidator(validatorType, (ServiceLifetime?)lifetime);
        }

        /// <summary>
        /// Registers every validator in the given assemblies — anything that implements
        /// <see cref="IValidator{T}"/> and can be constructed.
        /// </summary>
        /// <remarks>
        /// Registrations use <c>TryAdd</c>, so a validator registered explicitly beforehand wins over
        /// whatever a scan finds for the same type.
        /// </remarks>
        public NValidationOptions AddValidatorsFromAssembly(params Assembly[] assemblies)
        {
            return this.AddValidatorsFromAssembly(lifetime: null, assemblies);
        }

        /// <inheritdoc cref="AddValidatorsFromAssembly(Assembly[])" path="/summary"/>
        /// <inheritdoc cref="AddValidatorsFromAssembly(Assembly[])" path="/remarks"/>
        public NValidationOptions AddValidatorsFromAssembly(ServiceLifetime lifetime, params Assembly[] assemblies)
        {
            return this.AddValidatorsFromAssembly((ServiceLifetime?)lifetime, assemblies);
        }

        /// <summary>
        /// Writes everything the delegate asked for into the service collection. Deferred to here so
        /// that <see cref="ValidatorLifetime"/> and <see cref="MessageProvider"/> apply wherever in the
        /// delegate they were set.
        /// </summary>
        internal void Apply()
        {
            if (this.messageProvider != null)
            {
                this.Services.Replace(new ServiceDescriptor(
                    typeof(IValidationMessageProvider), this.messageProvider, ServiceLifetime.Singleton));
            }

            foreach (var (validatedType, validatorType, lifetime) in this.registrations)
            {
                // TryAdd, in the order they were asked for: an explicit registration wins over whatever
                // a later scan finds for the same type.
                this.Services.TryAdd(new ServiceDescriptor(
                    validatedType,
                    serviceProvider => Create(serviceProvider, validatorType),
                    lifetime ?? this.ValidatorLifetime));
            }
        }

        private static object Create(IServiceProvider serviceProvider, Type validatorType)
        {
            var factory = Factories.GetOrAdd(
                validatorType, static type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes));

            var validator = factory(serviceProvider, arguments: null);

            if (validator is IMessageProviderTarget target)
            {
                target.Messages = serviceProvider.GetRequiredService<IValidationMessageProvider>();
            }

            return validator;
        }

        /// <remarks>
        /// The type is inspected now rather than at <see cref="Apply"/>, so a type that validates
        /// nothing is reported from the call that named it.
        /// </remarks>
        private NValidationOptions AddValidator(Type validatorType, ServiceLifetime? lifetime)
        {
            ArgumentNullException.ThrowIfNull(validatorType);

            var validatedTypes = ValidatorTypeInfo.GetValidatedTypes(validatorType).ToArray();

            if (validatedTypes.Length == 0)
            {
                throw new ArgumentException(
                    $"Type '{validatorType}' does not implement {typeof(IValidator<>)}.", nameof(validatorType));
            }

            foreach (var validatedType in validatedTypes)
            {
                this.Add(validatedType, validatorType, lifetime);
            }

            return this;
        }

        /// <remarks>
        /// Two validators for the same payload are rejected rather than resolved by whichever
        /// <see cref="Assembly.GetTypes"/> happened to return first: that order is not documented,
        /// which would make the choice arbitrary and the behaviour reproducible only by luck.
        /// </remarks>
        private NValidationOptions AddValidatorsFromAssembly(ServiceLifetime? lifetime, params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            var found = new Dictionary<Type, Type>();
            var discovered = new List<(Type ValidatedType, Type ValidatorType)>();

            foreach (var assembly in assemblies)
            {
                foreach (var validatorType in ValidatorTypeInfo.GetValidatorTypes(assembly))
                {
                    foreach (var validatedType in ValidatorTypeInfo.GetValidatedTypes(validatorType))
                    {
                        if (found.TryGetValue(validatedType, out var already))
                        {
                            if (already == validatorType)
                            {
                                continue;
                            }

                            throw new InvalidOperationException(
                                $"'{already}' and '{validatorType}' both validate '{validatedType}', so a scan " +
                                "cannot choose between them. Register the one you want with AddValidator " +
                                "before scanning — an explicit registration wins — or keep only one of them " +
                                "in the assemblies being scanned.");
                        }

                        found.Add(validatedType, validatorType);
                        discovered.Add((validatedType, validatorType));
                    }
                }
            }

            // In discovery order, so the whole scan is rejected before any of it is registered.
            foreach (var (validatedType, validatorType) in discovered)
            {
                this.Add(validatedType, validatorType, lifetime);
            }

            return this;
        }

        private NValidationOptions Add(Type validatedType, Type validatorType, ServiceLifetime? lifetime)
        {
            this.registrations.Add((validatedType, validatorType, lifetime));

            return this;
        }
    }
}
