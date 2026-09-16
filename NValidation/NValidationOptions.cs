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
        /// What each <c>AddValidator</c> and each scan asked for, resolved and applied once the delegate
        /// has run. A <c>null</c> lifetime means "whatever <see cref="ValidatorLifetime"/> ends up being".
        /// </summary>
        private readonly List<Registration> registrations = [];

        private Type? messageProvider;

        internal NValidationOptions(IServiceCollection services)
        {
            this.Services = services;
        }

        /// <summary>
        /// The collection being configured, for a registration this type has no method for — and what an
        /// integration package extends to add its own configuration here.
        /// </summary>
        internal IServiceCollection Services { get; }

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
        /// Registers a validator as a singleton where that is provably safe, whatever
        /// <see cref="ValidatorLifetime"/> says. Off by default.
        /// </summary>
        /// <remarks>
        /// A validator declares its rules in its constructor and never changes afterwards, so rebuilding
        /// them per scope is pure waste — measurably the largest cost this library imposes on a request,
        /// an order of magnitude more than validating. The reason the default is nevertheless
        /// <see cref="ServiceLifetime.Scoped"/> is that a validator may depend on something that is
        /// itself scoped, and a singleton holding one of those is a bug that surfaces under load rather
        /// than at startup.
        /// <para>
        /// This settles that case by case instead of globally: at registration, a validator is promoted
        /// only when every constructor parameter resolves to something already registered as a singleton,
        /// or to another validator which itself qualifies. One that takes a scoped dependency is left
        /// exactly where it was. The decision is made once, from the service collection, and never
        /// depends on what a request happens to do.
        /// </para>
        /// <para>
        /// A validator with more than one public constructor is never promoted: which one is used is
        /// then a choice this cannot make on the container's behalf.
        /// </para>
        /// </remarks>
        public bool PromoteSafeValidatorsToSingleton { get; set; }

        /// <summary>
        /// How much every registered validator reports, unless it says otherwise for itself: across its
        /// properties, and within one property's chain. Mutated rather than assigned —
        /// <c>o.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;</c> — so naming one
        /// axis leaves the other at its built-in default.
        /// </summary>
        /// <remarks>
        /// Reaches the validators this registration constructs, which is every validator resolved from
        /// the container. One built with <c>new</c> takes the built-in defaults instead, exactly as it
        /// takes the built-in English messages.
        /// </remarks>
        public ValidationBehaviors ValidationBehaviors { get; } = new();

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
            return this.Add(typeof(IValidator<TInstance>), typeof(TValidator), lifetime: null, isExplicit: true);
        }

        /// <inheritdoc cref="AddValidator{TInstance, TValidator}()" path="/summary"/>
        /// <inheritdoc cref="AddValidator{TValidator}(ServiceLifetime)" path="/remarks"/>
        public NValidationOptions AddValidator<TInstance, TValidator>(ServiceLifetime lifetime)
            where TValidator : class, IValidator<TInstance>
        {
            return this.Add(typeof(IValidator<TInstance>), typeof(TValidator), lifetime, isExplicit: true);
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
        /// An explicit <c>AddValidator</c> wins over whatever a scan finds for the same type, wherever
        /// in the delegate it is written.
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
            // Copied rather than shared, so a delegate which kept hold of these options cannot change
            // what already-registered validators will be handed. Captured by the factories below as a
            // value of its own, so a factory roots this small object rather than these options — and
            // through them the whole service collection.
            var validationBehaviors = new ValidationBehaviors
            {
                Class = this.ValidationBehaviors.Class,
                Property = this.ValidationBehaviors.Property,
            };

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

                var resolvedLifetime = lifetime ?? this.ValidatorLifetime;

                if (this.PromoteSafeValidatorsToSingleton &&
                    resolvedLifetime != ServiceLifetime.Singleton &&
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

            if (validator is IMessageProviderTarget messageProviderTarget)
            {
                messageProviderTarget.Messages = serviceProvider.GetRequiredService<IValidationMessageProvider>();
            }

            if (validator is IValidationBehaviorTarget validationBehaviorTarget)
            {
                validationBehaviorTarget.AmbientValidationBehaviors = validationBehaviors;
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
        private NValidationOptions AddValidatorsFromAssembly(ServiceLifetime? lifetime, params Assembly[] assemblies)
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

        private NValidationOptions Add(Type validatedType, Type validatorType, ServiceLifetime? lifetime, bool isExplicit)
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
