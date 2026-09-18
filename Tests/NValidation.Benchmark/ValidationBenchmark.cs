using BenchmarkDotNet.Attributes;
using NValidation.TestData;
using NValidation.TestData.Validators;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What one validation costs, from the floor up to a realistic payload.
    /// </summary>
    /// <remarks>
    /// Every benchmark <b>returns</b> its task rather than awaiting it, so BenchmarkDotNet awaits it and
    /// the measurement is the library's cost rather than a wrapper state machine belonging to the
    /// benchmark. Awaiting here once doubled the apparent overhead.
    /// </remarks>
    [MemoryDiagnoser]
    public class ValidationBenchmark
    {
        private IValidator<Car> carValidator = null!;
        private IValidator<Car> asyncRuleValidator = null!;
        private IValidator<Manufacturer> singleRuleValidator = null!;
        private Car validCar = null!;
        private Car invalidCar = null!;
        private Manufacturer manufacturer = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.carValidator = new CarValidator(
                new CarModelValidator(new ManufacturerValidator()), new ServiceRecordValidator());

            this.singleRuleValidator = new ManufacturerValidator();

            this.asyncRuleValidator = new AwaitingCarValidator();

            this.validCar = Cars.Car();

            this.invalidCar = Cars.Car();
            this.invalidCar.Vin = "TOO-SHORT";
            this.invalidCar.FeatureIds = [1, 1];
            this.invalidCar.Model!.Name = "";

            this.manufacturer = Cars.Manufacturer();
        }

        /// <summary>
        /// The floor: four rules over one flat object, nothing to report.
        /// </summary>
        [Benchmark]
        public ValueTask<ValidationResult> SingleObject()
        {
            return this.singleRuleValidator.ValidateAsync(this.manufacturer);
        }

        /// <summary>
        /// A rule which genuinely awaits. Every other benchmark here completes synchronously, so this is
        /// the only one that exercises the state machine the async-only design actually rests on.
        /// </summary>
        [Benchmark]
        public ValueTask<ValidationResult> WithAnAwaitingRule()
        {
            return this.asyncRuleValidator.ValidateAsync(this.validCar);
        }

        /// <summary>
        /// A realistic payload: plain rules, a rule of its own, a nested validator, a comparison against
        /// a sibling property and a collection.
        /// </summary>
        [Benchmark]
        public ValueTask<ValidationResult> WholePayload()
        {
            return this.carValidator.ValidateAsync(this.validCar);
        }

        /// <summary>
        /// The failing path, which is where the errors, the message lookups and their placeholder
        /// dictionaries are actually allocated.
        /// </summary>
        [Benchmark]
        public ValueTask<ValidationResult> WholePayloadWithFailures()
        {
            return this.carValidator.ValidateAsync(this.invalidCar);
        }

        /// <summary>
        /// One rule which yields, so the measurement includes a suspension rather than a chain that
        /// happens to finish synchronously.
        /// </summary>
        private sealed class AwaitingCarValidator : Validator<Car>
        {
            public AwaitingCarValidator()
            {
                this.Property(c => c.Vin).MustAsync(async (vin, cancellationToken) =>
                {
                    await Task.Yield();

                    return vin != null;
                });
            }
        }
    }
}
