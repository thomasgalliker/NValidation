using BenchmarkDotNet.Attributes;
using NValidation.TestData;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What handing the rules data costs: carrying it where no rule reads it, building the options for
    /// every call, and chains whose condition reads it — with the data handed over and without.
    /// </summary>
    /// <remarks>
    /// The data rides in the reference the group selection already takes, so options built once and
    /// reused should cost exactly what a call without data costs, frame included.
    /// </remarks>
    [MemoryDiagnoser]
    public class ValidationDataBenchmark
    {
        private static readonly ListingPolicy Policy = new(MaximumMileage: 200_000, RequiresServiceHistory: true);

        private static readonly NValidationOptions WithData = new() { ValidationData = [Policy] };

        private readonly NValidation.IValidator<Manufacturer> plain = new PlainValidator();

        private readonly NValidation.IValidator<Manufacturer> readingData = new DataReadingValidator();

        private Manufacturer manufacturer = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();
        }

        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> NoData()
        {
            return this.plain.ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> DataReused()
        {
            return this.plain.ValidateAsync(this.manufacturer, WithData);
        }

        [Benchmark]
        public ValueTask<ValidationResult> DataFresh()
        {
            return this.plain.ValidateAsync(this.manufacturer, new NValidationOptions { ValidationData = [Policy] });
        }

        [Benchmark]
        public ValueTask<ValidationResult> WhenAbsent()
        {
            return this.readingData.ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> WhenPresent()
        {
            return this.readingData.ValidateAsync(this.manufacturer, WithData);
        }

        private sealed class PlainValidator : Validator<Manufacturer>
        {
            public PlainValidator()
            {
                for (var i = 0; i < 4; i++)
                {
                    this.Property($"Name{i}", static m => m.Name).NotEmpty();
                }
            }
        }

        private sealed class DataReadingValidator : Validator<Manufacturer>
        {
            public DataReadingValidator()
            {
                for (var i = 0; i < 4; i++)
                {
                    this.Property($"Name{i}", static m => m.Name)
                        .NotEmpty()
                        .When<ListingPolicy>(static (_, policy) => policy.RequiresServiceHistory);
                }
            }
        }
    }
}
