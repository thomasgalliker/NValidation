using BenchmarkDotNet.Attributes;
using FluentValidation;
using NValidation.TestData;
using NValidation.TestData.Validators;
using FV = FluentValidation;

namespace NValidation.Benchmark
{
    /// <summary>
    /// The head-to-head on the realistic payload rather than the flat one: a nested validator two levels
    /// deep, a comparison against a sibling property, a capped collection and a collection whose entries
    /// have a validator of their own.
    /// </summary>
    /// <remarks>
    /// <see cref="ComparisonBenchmark"/> measures four chains over one flat object, which is the floor.
    /// Nesting and collections are where a library's per-element and per-level overhead appears, and a
    /// conclusion drawn only from the flat case would not know whether the gap it found holds here.
    /// </remarks>
    [MemoryDiagnoser]
    public class PayloadComparisonBenchmark
    {
        private NValidation.IValidator<Car> nValidator = null!;
        private FV.IValidator<Car> fluentValidator = null!;

        private Car valid = null!;
        private Car invalid = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.nValidator = new CarValidator(
                new CarModelValidator(new ManufacturerValidator()), new ServiceRecordValidator());

            this.fluentValidator = new FluentCarValidator();

            this.valid = Cars.Car();

            this.invalid = Cars.Car();
            this.invalid.Vin = "TOO-SHORT";
            this.invalid.FeatureIds = [1, 1];
            this.invalid.Model!.Name = "";

            BenchmarkVerdict.RequireTheSame(this.nValidator, this.fluentValidator, this.valid);
            BenchmarkVerdict.RequireTheSame(this.nValidator, this.fluentValidator, this.invalid);
        }

        [Benchmark(Baseline = true, Description = "NValidation payload (valid)")]
        public ValueTask<ValidationResult> NValidation_Valid()
        {
            return this.nValidator.ValidateAsync(this.valid);
        }

        [Benchmark(Description = "FluentValidation sync payload (valid)")]
        public FV.Results.ValidationResult Fluent_Sync_Valid()
        {
            return this.fluentValidator.Validate(this.valid);
        }

        [Benchmark(Description = "NValidation payload (invalid)")]
        public ValueTask<ValidationResult> NValidation_Invalid()
        {
            return this.nValidator.ValidateAsync(this.invalid);
        }

        [Benchmark(Description = "FluentValidation sync payload (invalid)")]
        public FV.Results.ValidationResult Fluent_Sync_Invalid()
        {
            return this.fluentValidator.Validate(this.invalid);
        }

        private sealed class FluentCarValidator : AbstractValidator<Car>
        {
            public FluentCarValidator()
            {
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(c => c.Vin)
                    .NotEmpty()
                    .Must(vin => vin == null || vin.Trim().Length == CarValidator.VinLength);

                this.RuleFor(c => c.Model)
                    .NotNull()
                    .SetValidator(new FluentCarModelValidator()!);

                this.RuleFor(c => c.Mileage).GreaterThanOrEqualTo(0);

                this.RuleFor(c => c.FirstRegistration).NotEqual(default(DateTime));

                this.RuleFor(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);

                this.RuleFor(c => c.FeatureIds)
                    .Must(ids => ids == null || ids.Distinct().Count() == ids.Count);

                this.RuleFor(c => c.ServiceHistory)
                    .Must(history => history == null || history.Count <= CarValidator.MaximumServiceRecords)
                    .Must((c, history) => history == null || history.All(record => record.Mileage <= c.Mileage));

                this.RuleForEach(c => c.ServiceHistory).SetValidator(new FluentServiceRecordValidator());
            }
        }

        private sealed class FluentCarModelValidator : AbstractValidator<CarModel>
        {
            public FluentCarModelValidator()
            {
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(m => m.Name).NotEmpty().MaximumLength(100);

                this.RuleFor(m => m.Manufacturer)
                    .NotNull()
                    .SetValidator(new FluentManufacturerValidator()!);

                this.RuleFor(m => m.EngineType).IsInEnum();

                this.RuleFor(m => m.SeatCount).InclusiveBetween(1, 9);

                this.RuleFor(m => m.BasePrice).NotNull().GreaterThan(0m);

                this.RuleFor(m => m.FuelConsumption).Must(value => !double.IsNaN(value));

                this.RuleFor(m => m.BatteryCapacityKwh)
                    .NotNull()
                    .GreaterThan(0m)
                    .When(m => m.EngineType == EngineType.Electric);
            }
        }

        private sealed class FluentManufacturerValidator : AbstractValidator<Manufacturer>
        {
            public FluentManufacturerValidator()
            {
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(m => m.Name).NotEmpty().MaximumLength(100);
                this.RuleFor(m => m.CountryCode).NotEmpty().Length(3);
                this.RuleFor(m => m.FoundedDate).LessThan(_ => DateTime.UtcNow);
                this.RuleFor(m => m.ContactEmail)
                    .EmailAddress()
                    .When(m => !string.IsNullOrWhiteSpace(m.ContactEmail));
            }
        }

        private sealed class FluentServiceRecordValidator : AbstractValidator<ServiceRecord>
        {
            public FluentServiceRecordValidator()
            {
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(r => r.Workshop).NotEmpty().MaximumLength(100);
                this.RuleFor(r => r.Mileage).GreaterThanOrEqualTo(0);
                this.RuleFor(r => r.Cost).GreaterThan(0m);
            }
        }
    }
}
