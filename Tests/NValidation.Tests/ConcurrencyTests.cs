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
        /// A validator composed into another keeps what its passes resolve to while its composer hands it the
        /// same, and every run here hands it data of its own: each run's entries must still be judged against
        /// that run's data, whichever run's resolution happens to be kept.
        /// </summary>
        [Fact]
        public async Task ForEach_OnASharedElementValidator_JudgesEachRunsEntriesAgainstItsOwnData()
        {
            // Arrange
            var recordValidator = new TestValidator<ServiceRecord>();
            recordValidator.Property(r => r.Mileage).Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage);

            var validator = new TestValidator<Car>();
            validator.Property(c => c.ServiceHistory).ForEach(recordValidator);

            // Act
            var failures = await RunConcurrently(
                validator,
                worker =>
                {
                    // Every entry of an even worker is at its limit and passes; every entry of an odd worker is
                    // one above it.
                    var car = Cars.Car();
                    car.ServiceHistory = Enumerable.Range(0, 3)
                        .Select(_ => new ServiceRecord { Workshop = "Garage", Mileage = (worker * 10) + (worker % 2) })
                        .ToList();

                    return (car, Expected: worker % 2 == 0 ? "0" : "3");
                },
                (result, expected) =>
                {
                    var count = result.Errors.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    return count == expected ? null : $"expected {expected} failures, saw {count}";
                },
                worker => new NValidationOptions { ValidationData = [new ListingPolicy(worker * 10, false)] });

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
            Func<ValidationResult, string, string?> verify,
            Func<int, NValidationOptions?>? options = null)
        {
            var failures = new ConcurrentBag<string>();

            await Parallel.ForEachAsync(
                Enumerable.Range(0, Workers),
                async (worker, cancellationToken) =>
                {
                    for (var iteration = 0; iteration < Iterations; iteration++)
                    {
                        var (payload, expected) = arrange(worker);

                        var result = options?.Invoke(worker) is { } workerOptions
                            ? await validator.ValidateAsync(payload, workerOptions, cancellationToken)
                            : await validator.ValidateAsync(payload, cancellationToken);

                        if (verify(result, expected) is { } failure)
                        {
                            failures.Add($"worker {worker}: {failure}");
                        }
                    }
                });

            return [.. failures];
        }

        /// <summary>
        /// The selection belongs to the run rather than to the validator, so two runs of one validator
        /// asking for different groups must not see each other's answer.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_OnASharedValidator_KeepsEachRunsGroupSelectionToItself()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).Must(vin => false).WithMessage((car, _) => $"rejected {car.Vin}");
            validator.Property(c => c.RegistrationPlate)
                .Must(plate => false)
                .WithMessage((car, _) => $"plate rejected {car.Vin}")
                .WithGroup("Create");

            // Act
            var failures = await RunConcurrently(
                validator,
                worker =>
                {
                    var car = Cars.Car();
                    car.Vin = $"VIN-{worker}";

                    // An even worker selects the group and hears about both properties; an odd one
                    // selects nothing and hears about the ungrouped property alone.
                    return (car, Expected: $"{(SelectsTheGroup(worker) ? 2 : 1)} rejected VIN-{worker}");
                },
                (result, expected) =>
                {
                    var expectedCount = int.Parse(expected[..expected.IndexOf(' ', StringComparison.Ordinal)]);
                    var expectedVin = expected[(expected.LastIndexOf(' ') + 1)..];

                    if (result.Errors.Count != expectedCount)
                    {
                        return $"expected {expectedCount} failures for {expectedVin}, saw {result.Errors.Count}";
                    }

                    foreach (var error in result.Errors)
                    {
                        if (!error.Message.EndsWith(expectedVin, StringComparison.Ordinal))
                        {
                            return $"expected every message to name {expectedVin}, saw '{error.Message}'";
                        }
                    }

                    return null;
                },
                worker => SelectsTheGroup(worker) ? new NValidationOptions { ValidationGroups = "Create" } : null);

            // Assert
            failures.Should().BeEmpty();
        }

        private static bool SelectsTheGroup(int worker)
        {
            return worker % 2 == 0;
        }

        /// <summary>
        /// The same for a selection that leaves the default group out: an even worker hears about the
        /// grouped property alone, an odd one about the ungrouped property alone, and neither may be told
        /// what the other asked for.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_OnASharedValidator_KeepsEachRunsExclusiveSelectionToItself()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).Must(vin => false).WithMessage((car, _) => $"vin rejected {car.Vin}");
            validator.Property(c => c.RegistrationPlate)
                .Must(plate => false)
                .WithMessage((car, _) => $"plate rejected {car.Vin}")
                .WithGroup("Create");

            var onlyCreate = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Create") };

            // Act
            var failures = await RunConcurrently(
                validator,
                worker =>
                {
                    var car = Cars.Car();
                    car.Vin = $"VIN-{worker}";

                    return (car, Expected: $"{(SelectsTheGroup(worker) ? "plate" : "vin")} rejected VIN-{worker}");
                },
                (result, expected) =>
                {
                    var message = result.Errors.Single().Message;

                    return message == expected ? null : $"expected \"{expected}\" but got \"{message}\"";
                },
                worker => SelectsTheGroup(worker) ? onlyCreate : null);

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// Data belongs to the call that handed it over: each worker's limit sits just below or at its
        /// own mileage, so a worker judged against anybody else's limit would get the wrong answer.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_OnASharedValidator_KeepsEachRunsDataToItself()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Mileage).Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage);

            // Act
            var failures = await RunConcurrently(
                validator,
                worker =>
                {
                    // An even worker's mileage is at its limit and passes; an odd worker's is one above it.
                    var car = Cars.Car();
                    car.Mileage = (worker * 10) + (worker % 2);

                    return (car, Expected: worker % 2 == 0 ? "0" : "1");
                },
                (result, expected) =>
                {
                    var count = result.Errors.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    return count == expected ? null : $"expected {expected} failures, saw {count}";
                },
                worker => new NValidationOptions { ValidationData = [new ListingPolicy(worker * 10, false)] });

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// One data instance kept for every call pairs itself with whichever selection the call made, and
        /// keeps only the last pairing; runs alternating between two selections must still each get their
        /// own.
        /// </summary>
        [Fact]
        public async Task ValidateAsync_WithOneDataInstanceSharedBetweenSelections_GivesEachRunItsOwnSelection()
        {
            // Arrange
            var validator = new TestValidator<Car>();
            validator.Property(c => c.Vin).Must(vin => false).WithMessage("vin");
            validator.Property(c => c.Mileage)
                .Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage)
                .WithMessage("mileage")
                .WithGroup("Listing");

            ValidationData data = [new ListingPolicy(100, false)];
            var listingAlone = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing"), ValidationData = data };
            var plain = new NValidationOptions { ValidationData = data };

            // Act
            var failures = await RunConcurrently(
                validator,
                worker =>
                {
                    var car = Cars.Car();
                    car.Mileage = 200;

                    return (car, Expected: SelectsTheGroup(worker) ? "mileage" : "vin");
                },
                (result, expected) =>
                {
                    var message = result.Errors.Single().Message;

                    return message == expected ? null : $"expected \"{expected}\" but got \"{message}\"";
                },
                worker => SelectsTheGroup(worker) ? listingAlone : plain);

            // Assert
            failures.Should().BeEmpty();
        }

        /// <summary>
        /// The first validations of a shared validator freeze it while others are already reading. What
        /// they read has to come from one freeze: rules without the gates of their groups would run a
        /// grouped chain nobody selected.
        /// </summary>
        [Fact]
        public void ValidateAsync_OnAValidatorFreezingUnderConcurrentRuns_StillGatesItsGroupedChains()
        {
            // Arrange
            var counts = new ConcurrentBag<int>();

            // Act
            for (var round = 0; round < 20; round++)
            {
                var validator = new TestValidator<Car>();
                validator.Property(c => c.Vin).NotEmpty();
                validator.Property(c => c.RegistrationPlate).NotEmpty().WithGroup("Create");

                RaceOnDedicatedThreads(Environment.ProcessorCount, () =>
                    counts.Add(validator.ValidateAsync(new Car()).GetAwaiter().GetResult().Errors.Count));
            }

            // Assert
            counts.Should().OnlyContain(count => count == 1);
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
