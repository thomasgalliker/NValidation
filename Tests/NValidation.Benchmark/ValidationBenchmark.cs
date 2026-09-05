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
        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> SingleObject()
        {
            return this.singleRuleValidator.ValidateAsync(this.manufacturer);
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
    }
}
