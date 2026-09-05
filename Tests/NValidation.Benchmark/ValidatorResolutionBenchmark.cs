using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NValidation.TestData;
using NValidation.TestData.Validators;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What resolving a validator costs, which is what a scoped registration pays on every request.
    /// </summary>
    /// <remarks>
    /// A validator declares its rules in its constructor: a property rule and its closures per rule, and
    /// a lookup into the compiled-accessor cache per property. Scoped means all of that happens per
    /// scope; singleton means once for the process. The difference is what
    /// <c>ValidatorLifetime = ServiceLifetime.Singleton</c> buys a host whose validators have nothing
    /// scoped to capture.
    /// </remarks>
    [MemoryDiagnoser]
    public class ValidatorResolutionBenchmark
    {
        private ServiceProvider scopedProvider = null!;
        private ServiceProvider singletonProvider = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.scopedProvider = BuildProvider(ServiceLifetime.Scoped);
            this.singletonProvider = BuildProvider(ServiceLifetime.Singleton);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            this.scopedProvider.Dispose();
            this.singletonProvider.Dispose();
        }

        /// <summary>
        /// A scope, and the validator graph built inside it — the shape of one request.
        /// </summary>
        [Benchmark(Baseline = true)]
        public IValidator<Car> Scoped()
        {
            using var scope = this.scopedProvider.CreateScope();

            return scope.ServiceProvider.GetRequiredService<IValidator<Car>>();
        }

        /// <inheritdoc cref="Scoped"/>
        [Benchmark]
        public IValidator<Car> Singleton()
        {
            using var scope = this.singletonProvider.CreateScope();

            return scope.ServiceProvider.GetRequiredService<IValidator<Car>>();
        }

        private static ServiceProvider BuildProvider(ServiceLifetime lifetime)
        {
            var services = new ServiceCollection();

            services.AddNValidation(o =>
            {
                o.ValidatorLifetime = lifetime;
                o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
            });

            return services.BuildServiceProvider();
        }
    }
}
