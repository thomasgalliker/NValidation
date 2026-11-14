using System.Collections.Concurrent;

namespace NValidation.DependencyInjection.Tests
{
    /// <summary>
    /// A validator the container shares across every request has to be safe to share. The core suite
    /// hammers validators it constructed itself; this hammers the shape the registration actually
    /// produces, which is the one a host runs.
    /// </summary>
    /// <remarks>
    /// Its own collection because it saturates the thread pool, exactly as the core's concurrency tests
    /// do: a test trying to maximise interleaving should not be competing with the rest of the suite for
    /// the threads it needs.
    /// </remarks>
    [Trait(Traits.Category, Traits.UnitTests)]
    [Collection(Collections.Concurrency)]
    public class ContainerConcurrencyTests
    {
        private static readonly int Workers = Environment.ProcessorCount * 4;

        private const int Iterations = 200;

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

            var validator = services.BuildServiceProvider().GetRequiredService<IValidator<Car>>();

            // Each worker validates its own payload: a leak shows up as one worker being told about
            // another's, rather than as a vague count mismatch.
            var failures = new ConcurrentBag<string>();

            // Act
            await Parallel.ForEachAsync(
                Enumerable.Range(0, Workers),
                async (worker, cancellationToken) =>
                {
                    for (var iteration = 0; iteration < Iterations; iteration++)
                    {
                        var result = await validator.ValidateAsync(new Car(), cancellationToken);

                        if (!result.Errors.Any(error => error.PropertyName == "Vin"))
                        {
                            failures.Add($"worker {worker}: the VIN was not reported");
                        }
                    }
                });

            // Assert
            failures.Should().BeEmpty();
        }
    }
}
