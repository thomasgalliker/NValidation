using BenchmarkDotNet.Attributes;
using NValidation.TestData;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What it costs a selection that leaves the default group out to reach a group a nested validator
    /// declares: the composing chain runs only the part that hands its value on, and the nested validator
    /// runs its grouped chains alone.
    /// </summary>
    /// <remarks>
    /// The three rows do different work on purpose — the default group, both groups, the nested group
    /// alone — so what they show is where the time goes, not a race between them.
    /// </remarks>
    [MemoryDiagnoser]
    public class ReachThroughBenchmark
    {
        private static readonly NValidationOptions Additive = new() { ValidationGroups = "Create" };

        private static readonly NValidationOptions ReachingThrough = new() { ValidationGroups = ValidationGroups.Only("Create") };

        private NValidation.IValidator<CarModel> validator = null!;

        private CarModel model = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.validator = new ModelValidator(new GroupedManufacturerValidator());
            this.model = Cars.CarModel();
        }

        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> DefaultGroup()
        {
            return this.validator.ValidateAsync(this.model);
        }

        [Benchmark]
        public ValueTask<ValidationResult> BothGroups()
        {
            return this.validator.ValidateAsync(this.model, Additive);
        }

        [Benchmark]
        public ValueTask<ValidationResult> NestedGroupAlone()
        {
            return this.validator.ValidateAsync(this.model, ReachingThrough);
        }

        private sealed class ModelValidator : Validator<CarModel>
        {
            public ModelValidator(NValidation.IValidator<Manufacturer> manufacturerValidator)
            {
                this.Property("Name", static m => m.Name).NotEmpty();
                this.Property("SeatCount", static m => m.SeatCount).GreaterThan(0);
                this.Property("BasePrice", static m => m.BasePrice).NotNull();
                this.Property("Manufacturer", static m => m.Manufacturer).SetValidator(manufacturerValidator);
            }
        }

        private sealed class GroupedManufacturerValidator : Validator<Manufacturer>
        {
            public GroupedManufacturerValidator()
            {
                this.Group("Create", () =>
                {
                    this.Property("Name", static m => m.Name).NotEmpty();
                    this.Property("CountryCode", static m => m.CountryCode).NotEmpty();
                    this.Property("ContactEmail", static m => m.ContactEmail).NotEmpty();
                    this.Property("Website", static m => m.Website).NotEmpty();
                });
            }
        }
    }
}
