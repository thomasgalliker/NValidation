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
    /// <see cref="ServiceCollectionExtensions.AddNValidation(IServiceCollection, Action{NValidationBuilder})"/>,
    /// so everything this library needs is configured in one place. The settings are properties and the
    /// registrations are methods; nothing reaches the service collection until the delegate has run, so
    /// a setting applies to every validator however the delegate is ordered.
    /// </remarks>
    public sealed class NValidationBuilder
    {
        private readonly List<Registration> registrations = [];
        private Type? messageProvider;
        private ServiceLifetime? validatorLifetime;
        private bool? promoteSafeValidatorsToSingleton;

        internal NValidationBuilder(IServiceCollection services)
        {
            this.Services = services;
        }

        internal IServiceCollection Services { get; }

        /// <summary>
        /// The lifetime a validator is registered with unless one is named on its own call.
        /// <see cref="ServiceLifetime.Scoped"/> unless set, because a validator may depend on something
        /// that is itself scoped and a longer-lived validator would capture it.
        /// </summary>
        /// <remarks>
        /// A lifetime set here is registered as written, unless promotion was asked for by name. Left
        /// unset, a validator which provably depends on nothing scoped is promoted to a singleton — see
        /// <see cref="PromoteSafeValidatorsToSingleton"/>.
        /// </remarks>
        public ServiceLifetime ValidatorLifetime
        {
            get => this.validatorLifetime ?? ServiceLifetime.Scoped;
            set => this.validatorLifetime = value;
        }

        /// <summary>
        /// Registers a validator as a singleton where that is provably safe. On by default.
        /// </summary>
        /// <remarks>
        /// A validator declares its rules in its constructor and never changes afterwards, so rebuilding
        /// them per scope is pure waste: building a validator graph costs over twenty times what using it
        /// does. A validator is promoted only when every constructor parameter resolves to something
        /// already registered as a singleton, or to another validator which itself qualifies; one with a
        /// scoped dependency, or with more than one public constructor, is left where it was. The
        /// decision is made once, from the service collection.
        /// <para>
        /// A lifetime the host named — on a registration call, or through <see cref="ValidatorLifetime"/>
        /// — is an instruction, and the default promotion leaves it alone. Setting this to <c>true</c>
        /// yourself asks for promotion regardless, which lifts a named lifetime too.
        /// </para>
        /// </remarks>
        public bool PromoteSafeValidatorsToSingleton
        {
            get => this.promoteSafeValidatorsToSingleton ?? true;
            set => this.promoteSafeValidatorsToSingleton = value;
        }

        /// <summary>
        /// How much every validator the container builds reports, unless it says otherwise for itself:
        /// <c>o.ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError };</c>. An axis
        /// left <c>null</c> falls through to the options passed to the call, then
        /// <see cref="NValidationOptions.Default"/>.
        /// </summary>
        /// <remarks>
        /// Handed to the validators this registration constructs, and only those: it reaches neither a
        /// validator built with <c>new</c> nor one a validator composed for itself. What every validator
        /// reads is <see cref="NValidationOptions.Default"/>.
        /// </remarks>
        public ValidationBehaviors ValidationBehaviors { get; set; }

        /// <summary>
        /// The <see cref="IValidationMessageProvider"/> the validators the container builds take their
        /// message texts from — typically the application's resources, served in the language of the
        /// current request. Left unset, nothing is registered and those validators fall through to the
        /// options passed to the call, then <see cref="NValidationOptions.Default"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="Type"/> rather than an instance, because the container builds it and a provider
        /// may therefore take what it depends on through its constructor; a provider a host already
        /// holds goes on <see cref="NValidationOptions.Default"/> instead. Registered as a singleton: a
        /// provider has to be thread-safe anyway, and being longer-lived than every validator is what
        /// lets validators of any lifetime be handed it. Resolve the language while the message is
        /// produced rather than in the constructor.
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
        public NValidationBuilder AddValidator<TValidator>()
            where TValidator : class
        {
            return this.AddValidator(typeof(TValidator), lifetime: null);
        }

        /// <inheritdoc cref="AddValidator{TValidator}()" path="/summary"/>
        /// <remarks>
        /// For the one validator whose dependencies do not allow <see cref="ValidatorLifetime"/>.
        /// </remarks>
        public NValidationBuilder AddValidator<TValidator>(ServiceLifetime lifetime)
            where TValidator : class
        {
            return this.AddValidator(typeof(TValidator), (ServiceLifetime?)lifetime);
        }

        /// <summary>
        /// The same, naming the validated type as well — so the compiler checks that
        /// <typeparamref name="TValidator"/> really does validate <typeparamref name="TInstance"/>.
        /// </summary>
        public NValidationBuilder AddValidator<TInstance, TValidator>()
            where TValidator : class, IValidator<TInstance>
        {
            return this.Add(typeof(IValidator<TInstance>), typeof(TValidator), lifetime: null, isExplicit: true);
        }

        /// <inheritdoc cref="AddValidator{TInstance, TValidator}()" path="/summary"/>
        /// <inheritdoc cref="AddValidator{TValidator}(ServiceLifetime)" path="/remarks"/>
        public NValidationBuilder AddValidator<TInstance, TValidator>(ServiceLifetime lifetime)
            where TValidator : class, IValidator<TInstance>
        {
            return this.Add(typeof(IValidator<TInstance>), typeof(TValidator), lifetime, isExplicit: true);
        }

        /// <summary>
        /// The form a scan uses, where the validator's type is only known at runtime.
        /// </summary>
        /// <inheritdoc cref="AddValidator{TValidator}()" path="/exception"/>
        public NValidationBuilder AddValidator(Type validatorType)
        {
            return this.AddValidator(validatorType, lifetime: null);
        }

        /// <inheritdoc cref="AddValidator(Type)" path="/summary"/>
        /// <inheritdoc cref="AddValidator{TValidator}(ServiceLifetime)" path="/remarks"/>
        /// <inheritdoc cref="AddValidator{TValidator}()" path="/exception"/>
        public NValidationBuilder AddValidator(Type validatorType, ServiceLifetime lifetime)
        {
            return this.AddValidator(validatorType, (ServiceLifetime?)lifetime);
        }

        /// <summary>
        /// Registers every validator in the given assemblies — anything that implements
        /// <see cref="IValidator{T}"/> and can be constructed.
        /// </summary>
        /// <remarks>
        /// An explicit <c>AddValidator</c> wins over whatever a scan finds for the same type, wherever
        /// in the delegate it is written.
        /// </remarks>
        public NValidationBuilder AddValidatorsFromAssembly(params Assembly[] assemblies)
        {
            return this.AddValidatorsFromAssembly(lifetime: null, assemblies);
        }

        /// <inheritdoc cref="AddValidatorsFromAssembly(Assembly[])" path="/summary"/>
        /// <inheritdoc cref="AddValidatorsFromAssembly(Assembly[])" path="/remarks"/>
        public NValidationBuilder AddValidatorsFromAssembly(ServiceLifetime lifetime, params Assembly[] assemblies)
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
            // A value, captured by the factories below as it is now: a factory roots this small struct
            // rather than the builder and, through it, the whole service collection.
            var validationBehaviors = this.ValidationBehaviors;

            if (this.messageProvider != null)
            {
                this.Services.Replace(new ServiceDescriptor(
                    typeof(IValidationMessageProvider), this.messageProvider, ServiceLifetime.Singleton));
            }

            // Resolved in full before anything is written, so a contradiction rejects the whole
            // configuration rather than leaving half of it registered.
            // Constructor selection is reflection and the answer never changes, so it is done once
            // here and captured. Held by the descriptors rather than in a process-static keyed by
            // Type: a static would root the validator's assembly for the life of the process, which a
            // host loading plugins into a collectible AssemblyLoadContext cannot afford. One entry per
            // validator type, so a validator serving two payloads is still only inspected once.
            var factories = new Dictionary<Type, ObjectFactory>();

            var registrations = this.Resolve().ToArray();

            // Which validator serves which payload, so a validator depending on another can be reasoned
            // about before either of them reaches the container.
            var validatorsByService = registrations.ToDictionary(
                registration => registration.ValidatedType,
                registration => registration.ValidatorType);

            foreach (var (validatedType, validatorType, lifetime) in registrations)
            {
                if (!factories.TryGetValue(validatorType, out var factory))
                {
                    factory = ActivatorUtilities.CreateFactory(validatorType, Type.EmptyTypes);
                    factories.Add(validatorType, factory);
                }

                // A lifetime someone wrote down — on the call, or through ValidatorLifetime — is an
                // instruction: the default promotion leaves it alone, and only promotion asked for by
                // name lifts it.
                var namedLifetime = lifetime ?? this.validatorLifetime;
                var resolvedLifetime = namedLifetime ?? ServiceLifetime.Scoped;
                var askedForByName = this.promoteSafeValidatorsToSingleton == true;

                if (this.PromoteSafeValidatorsToSingleton &&
                    resolvedLifetime != ServiceLifetime.Singleton &&
                    (askedForByName || namedLifetime == null) &&
                    this.CanBeShared(validatorType, validatorsByService, []))
                {
                    resolvedLifetime = ServiceLifetime.Singleton;
                }

                this.Services.TryAdd(new ServiceDescriptor(
                    validatedType,
                    serviceProvider => Create(serviceProvider, factory, validationBehaviors),
                    resolvedLifetime));
            }
        }

        /// <summary>
        /// One validator per payload, chosen without regard to the order the delegate happened to be
        /// written in.
        /// </summary>
        /// <remarks>
        /// A payload named explicitly is settled by that, whether the scan that also found it ran before
        /// or after — a host which scans an assembly and then names its own replacement has said
        /// something unambiguous either way, and deciding it by line number would be order dependence in
        /// the one place it hurts most.
        /// <para>
        /// What is left is a genuine contradiction — two different validators asked for with equal
        /// standing — and that is refused rather than settled by whichever was seen first.
        /// </para>
        /// </remarks>
        private IEnumerable<(Type ValidatedType, Type ValidatorType, ServiceLifetime? Lifetime)> Resolve()
        {
            foreach (var group in this.registrations.GroupBy(registration => registration.ValidatedType))
            {
                var explicitly = group.Where(registration => registration.Explicit).ToArray();
                var candidates = explicitly.Length > 0 ? explicitly : [.. group];

                var chosen = candidates[0];

                foreach (var candidate in candidates)
                {
                    if (candidate.ValidatorType != chosen.ValidatorType)
                    {
                        throw new InvalidOperationException(RefusalOf(chosen, candidate, group.Key));
                    }
                }

                yield return (group.Key, chosen.ValidatorType, chosen.Lifetime);
            }
        }

        /// <remarks>
        /// The remedy differs by how the two were asked for: a scan can be settled by naming the one to
        /// keep, while two explicit registrations have already named both.
        /// </remarks>
        private static string RefusalOf(Registration first, Registration second, Type validatedType)
        {
            var subject = $"'{first.ValidatorType}' and '{second.ValidatorType}' both validate " +
                $"'{Validated(validatedType)}'";

            return first.Explicit
                ? $"{subject}, and both were registered explicitly. Name only one of them."
                : $"{subject}, so a scan cannot choose between them. Register the one you want with " +
                  "AddValidator — an explicit registration wins wherever it is written — or keep only " +
                  "one of them in the assemblies being scanned.";
        }

        /// <summary>
        /// Whether one instance of <paramref name="validatorType"/> can serve every request.
        /// </summary>
        /// <remarks>
        /// Conservative by construction: anything it cannot prove is shareable, it treats as not. A
        /// validator with several constructors, one depending on a service registered elsewhere at a
        /// shorter lifetime, or one whose dependencies form a cycle, all stay where the configuration
        /// put them.
        /// </remarks>
        private bool CanBeShared(
            Type validatorType,
            IReadOnlyDictionary<Type, Type> validatorsByService,
            HashSet<Type> underConsideration)
        {
            if (!underConsideration.Add(validatorType))
            {
                // A cycle. Nothing can be concluded, so nothing is.
                return false;
            }

            var constructors = validatorType.GetConstructors();

            if (constructors.Length != 1)
            {
                return false;
            }

            foreach (var parameter in constructors[0].GetParameters())
            {
                if (!this.IsShareable(parameter.ParameterType, validatorsByService, underConsideration))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether one dependency is safe for a shared validator to hold.
        /// </summary>
        private bool IsShareable(
            Type serviceType,
            IReadOnlyDictionary<Type, Type> validatorsByService,
            HashSet<Type> underConsideration)
        {
            // Another validator this same registration is about to add, whose own lifetime is therefore
            // still being decided: it is safe exactly when it is itself promotable.
            if (validatorsByService.TryGetValue(serviceType, out var dependency))
            {
                return this.CanBeShared(dependency, validatorsByService, underConsideration);
            }

            // Anything else has to be a singleton already. The last descriptor wins, as it does when the
            // container resolves it.
            for (var i = this.Services.Count - 1; i >= 0; i--)
            {
                if (this.Services[i].ServiceType == serviceType)
                {
                    return this.Services[i].Lifetime == ServiceLifetime.Singleton;
                }
            }

            return false;
        }

        private static object Create(IServiceProvider serviceProvider, ObjectFactory factory, ValidationBehaviors validationBehaviors)
        {
            var validator = factory(serviceProvider, arguments: null);

            // The level beneath what the validator's constructor declared: a validator which named a
            // setting for itself keeps it. A provider is handed over only where the host registered one.
            if (validator is IValidationRegistrationTarget registrationTarget)
            {
                registrationTarget.Options = new NValidationOptions
                {
                    MessageProvider = serviceProvider.GetService<IValidationMessageProvider>(),
                    ValidationBehaviors = validationBehaviors,
                };
            }

            return validator;
        }

        /// <remarks>
        /// The type is inspected now rather than at <see cref="Apply"/>, so a type that validates
        /// nothing is reported from the call that named it.
        /// </remarks>
        private NValidationBuilder AddValidator(Type validatorType, ServiceLifetime? lifetime)
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
                this.Add(validatedType, validatorType, lifetime, isExplicit: true);
            }

            return this;
        }

        /// <remarks>
        /// The scan only collects what it found. Which validator serves a payload — and whether the
        /// answer is a contradiction at all — is settled in <see cref="Resolve"/> once the whole
        /// delegate has run, because a payload this scan found may be named explicitly on a line the
        /// scan has not reached yet.
        /// </remarks>
        private NValidationBuilder AddValidatorsFromAssembly(ServiceLifetime? lifetime, params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            foreach (var assembly in assemblies)
            {
                foreach (var validatorType in ValidatorTypeInfo.GetValidatorTypes(assembly))
                {
                    foreach (var validatedType in ValidatorTypeInfo.GetValidatedTypes(validatorType))
                    {
                        this.Add(validatedType, validatorType, lifetime, isExplicit: false);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// The payload behind a service type, so a message about <c>IValidator&lt;Car&gt;</c> talks about
        /// the <c>Car</c> the reader wrote a validator for.
        /// </summary>
        private static Type Validated(Type validatorServiceType)
        {
            return validatorServiceType.GetGenericArguments()[0];
        }

        private NValidationBuilder Add(Type validatedType, Type validatorType, ServiceLifetime? lifetime, bool isExplicit)
        {
            this.registrations.Add(new Registration(validatedType, validatorType, lifetime, isExplicit));

            return this;
        }

        /// <summary>
        /// One validator asked for, and whether it was named or merely found.
        /// </summary>
        private readonly record struct Registration(
            Type ValidatedType,
            Type ValidatorType,
            ServiceLifetime? Lifetime,
            bool Explicit);
    }
}
