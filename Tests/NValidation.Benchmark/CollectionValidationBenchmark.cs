using BenchmarkDotNet.Attributes;
using NValidation.TestData;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What each entry of a collection costs, which is the cost that multiplies.
    /// </summary>
    /// <remarks>
    /// An entry validated by its own validator currently pays for a fresh error list, a
    /// <see cref="ValidationResult"/>, a message provider that knows the entry's index and one rule
    /// context per rule. A payload with a few hundred rows pays all of that a few hundred times, which
    /// is what makes this the cost worth measuring before optimising anything else.
    /// </remarks>
    [MemoryDiagnoser]
    public class CollectionValidationBenchmark
    {
        private readonly ServiceHistoryValidator validator = new();

        private Car car = null!;

        [Params(0, 10, 100, 1000)]
        public int ServiceRecords { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            this.car = Cars.Car();
            this.car.ServiceHistory = Enumerable
                .Range(0, this.ServiceRecords)
                .Select(index => new ServiceRecord
                {
                    Workshop = "Aurora Service",
                    Mileage = index * 1_000,
                    Cost = 120m,
                })
                .ToList();
        }

        [Benchmark]
        public ValueTask<ValidationResult> Validate()
        {
            return this.validator.ValidateAsync(this.car);
        }
    }
}
