using System.Collections.Concurrent;

namespace NValidation.Tests
{
    /// <summary>
    /// A validator may be registered as a singleton and validate many requests at once, so everything a
    /// run touches has to belong to that run. These tests hold one validator and hammer it: what they
    /// catch is a crossed wire — one request receiving another request's message, code or row index —
    /// which no single-threaded test can see.
    /// </summary>
    /// <remarks>
    /// Each worker validates its <em>own</em> payload carrying its own identity, so a leak shows up as
    /// worker <c>i</c> being told about worker <c>j</c> rather than as a vague count mismatch.
    /// </remarks>
    [Trait(Traits.Category, Traits.UnitTests)]
    [Collection(Collections.Concurrency)]
    public class ConcurrencyTests
    {
        private static readonly int Workers = Environment.ProcessorCount * 4;

        private const int Iterations = 200;

        /// <summary>
        /// The overrides are the sharpest edge: <c>WithMessage</c> and <c>WithErrorCode</c> are declared
        /// once and applied per rule, so a run which kept them anywhere shared would hand them to
        /// whichever run happened to be next.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_OnASharedValidator_KeepsEachRunsOverridesToItself()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .WithDisplayName("Vehicle ID")
                .Must(vin => false)
                .WithMessage((car, _) => $"rejected {car.Vin}")
                .WithErrorCode("VinRejected");

            // Act
            var failures = await RunConcurrently(validator, worker =>
            {
                var car = Cars.Car();
                car.Vin = $"VIN-{worker}";
                return (car, Expected: $"rejected VIN-{worker}");
            },
            (result, expected) =>
            {
                var error = result.Errors.Single();

                return error.Message == expected && error.ErrorCode == "VinRejected"
                    ? null
                    : $"expected \"{expected}\" but got \"{error.Message}\" / {error.ErrorCode}";
            });

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// The narrower leak the test above could mask: an override reaching the <em>next</em> rule of
        /// the same chain rather than the next run.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithAlternatingPayloads_AppliesEachRulesOwnOverride()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin)
                .WithValidationBehavior(ValidationBehavior.All)
                .Must(vin => vin != "TRIPS-FIRST")
                .WithErrorCode("First")
                .Must(vin => vin != "TRIPS-SECOND")
                .WithErrorCode("Second");

            // Act
            var failures = await RunConcurrently(validator, worker =>
            {
                var trips = worker % 2 == 0 ? "TRIPS-FIRST" : "TRIPS-SECOND";
                var car = Cars.Car();
                car.Vin = trips;
                return (car, Expected: worker % 2 == 0 ? "First" : "Second");
            },
            (result, expected) =>
            {
                var code = result.Errors.Single().ErrorCode;

                return code == expected ? null : $"expected {expected} but got {code}";
            });

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// The display names are resolved once, on the first run, and kept. Two first runs racing must
        /// both see a complete map rather than one seeing a half-built one.
        /// </summary>
        [Fact]
        public void ValidateAsync_OnAColdValidator_ResolvesDisplayNamesConsistently()
        {
            // Arrange
            var failures = new ConcurrentBag<string>();

            // Act
            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                // A validator per iteration, because the cache this races is warm after the first run.
                var validator = new TestValidator<Car>();
                validator.Property(c => c.Vin).WithDisplayName("Vehicle ID").NotEmpty();

                RaceOnDedicatedThreads(Environment.ProcessorCount, () =>
                {
                    var message = validator.ValidateAsync(new Car()).GetAwaiter().GetResult().Errors.Single().Message;

                    if (message != "Vehicle ID is required.")
                    {
                        failures.Add(message);
                    }
                });
            }

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// The element path holds the most per-run state: an index, a reused error list and a provider
        /// built per entry. Each worker walks a collection of its own length with its own bad row.
        /// </summary>
        [Fact]
        public async Task ForEach_OnASharedValidator_ReportsEachRowUnderItsOwnIndex()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record.Property(r => r.Workshop).NotEmpty());

            // Act
            var failures = await RunConcurrently(validator, worker =>
            {
                var rows = (worker % 5) + 1;
                var bad = rows - 1;

                var car = Cars.Car();
                car.ServiceHistory = Enumerable.Range(0, rows)
                    .Select(row => new ServiceRecord { Workshop = row == bad ? null : $"Garage {row}" })
                    .ToList();

                return (car, Expected: $"ServiceHistory[{bad}].Workshop");
            },
            (result, expected) =>
            {
                var propertyName = result.Errors.Single().PropertyName;

                return propertyName == expected ? null : $"expected {expected} but got {propertyName}";
            });

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// The same for an identity of the element's own, which is resolved lazily against the entry the
        /// provider was built for.
        /// </summary>
        [Fact]
        public async Task ForEach_WithAnIndexer_NamesEachRowByItsOwnIdentity()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory)
                .ForEach(record => record
                    .WithIndexer((r, _) => r.Workshop ?? "?")
                    .Property(r => r.Mileage).GreaterThan(0));

            // Act
            var failures = await RunConcurrently(validator, worker =>
            {
                var car = Cars.Car();
                car.ServiceHistory = [new ServiceRecord { Workshop = $"W{worker}", Mileage = 0 }];

                return (car, Expected: $"ServiceHistory[W{worker}].Mileage");
            },
            (result, expected) =>
            {
                var propertyName = result.Errors.Single().PropertyName;

                return propertyName == expected ? null : $"expected {expected} but got {propertyName}";
            });

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// The blunt sweep: whatever else is true, concurrent validation must not throw. This is what
        /// catches a rule list being enumerated while something else writes to it.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_UnderConcurrency_NeverThrows()
        {
            // Arrange
            var validator = new CarValidator(
                new CarModelValidator(new ManufacturerValidator()), new ServiceRecordValidator());

            var thrown = new ConcurrentBag<Exception>();

            // Act
            await Parallel.ForEachAsync(
                Enumerable.Range(0, Workers),
                async (worker, cancellationToken) =>
                {
                    for (var iteration = 0; iteration < Iterations; iteration++)
                    {
                        try
                        {
                            await validator.ValidateAsync(Cars.Car(), cancellationToken);
                            await validator.ValidateAsync(new Car(), cancellationToken);
                        }
                        catch (Exception exception)
                        {
                            thrown.Add(exception);
                        }
                    }
                });

            // Assert
            thrown.Should().BeEmpty();
        }

        /// <summary>
        /// The shape the registration actually produces for a singleton lifetime: one instance, resolved
        /// once, shared by every request.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_OnASingletonResolvedFromTheContainer_IsSafeToShare()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNValidation(o =>
            {
                o.ValidatorLifetime = ServiceLifetime.Singleton;
                o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
            });

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IValidator<Car>>();

            // Act
            var failures = await RunConcurrently(
                validator,
                worker => (new Car(), Expected: "Vin"),
                (result, expected) => result.Errors.Any(error => error.PropertyName == expected)
                    ? null
                    : "the VIN was not reported");

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// The compiled-accessor caches are keyed by property name inside a holder per closed generic,
        /// so this races a key that no other test can have warmed — which is why the payload type is
        /// declared for this test alone.
        /// </summary>
        [Fact]
        public void PropertyAccessor_ForOneColdExpression_CompilesOneUsableDelegate()
        {
            // Arrange
            var results = new ConcurrentBag<string?>();

            // Act
            RaceOnDedicatedThreads(Environment.ProcessorCount, () =>
            {
                var validator = new TestValidator<ColdPayload>();
                validator.Property(p => p.Name).NotEmpty();

                results.Add(validator.ValidateAsync(new ColdPayload()).GetAwaiter().GetResult()
                    .Errors.Single().PropertyName);
            });

            // Assert
            results.Should().OnlyContain(propertyName => propertyName == "Name");
        }

        /// <summary>
        /// Runs <paramref name="arrange"/> once per worker per iteration and checks the result with
        /// <paramref name="verify"/>, which returns <c>null</c> when the result is what that worker
        /// asked for and a description of the difference otherwise.
        /// </summary>
        private static async Task<IReadOnlyList<string>> RunConcurrently<T>(
            IValidator<T> validator,
            Func<int, (T Payload, string Expected)> arrange,
            Func<ValidationResult, string, string?> verify)
        {
            var failures = new ConcurrentBag<string>();

            await Parallel.ForEachAsync(
                Enumerable.Range(0, Workers),
                async (worker, cancellationToken) =>
                {
                    for (var iteration = 0; iteration < Iterations; iteration++)
                    {
                        var (payload, expected) = arrange(worker);

                        var result = await validator.ValidateAsync(payload, cancellationToken);

                        if (verify(result, expected) is { } failure)
                        {
                            failures.Add($"worker {worker}: {failure}");
                        }
                    }
                });

            return [.. failures];
        }

        /// <summary>
        /// Releases <paramref name="count"/> real threads at once. A <see cref="Barrier"/> inside a
        /// thread-pool work item would block more pool threads than there are cores and stall until the
        /// pool injected more, roughly one per second — so the interleaving this needs has to come from
        /// threads the pool does not own.
        /// </summary>
        private static void RaceOnDedicatedThreads(int count, Action body)
        {
            using var gate = new Barrier(count);

            var threads = new Thread[count];

            for (var i = 0; i < count; i++)
            {
                threads[i] = new Thread(() =>
                {
                    gate.SignalAndWait();
                    body();
                });

                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        /// <summary>
        /// Declared for the cold-cache test alone: the accessor cache lives for the process, so any
        /// other test touching this type would warm the very key the test is trying to race.
        /// </summary>
        private sealed class ColdPayload
        {
            public string? Name { get; set; }
        }
    }
}
