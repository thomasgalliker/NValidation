using BenchmarkDotNet.Attributes;
using FluentValidation;
using NValidation.TestData;
using FV = FluentValidation;

namespace NValidation.Benchmark
{
    /// <summary>
    /// A validation limited to two of five properties, against FluentValidation's selection of properties
    /// — what a check of the fields one request changed costs — with the full validation beside it.
    /// </summary>
    /// <remarks>
    /// The selection NValidation is handed is built once, as an application keeps it; FluentValidation is
    /// called the way its selection is written. <see cref="Setup"/> first holds both sides to reporting the
    /// same properties for a car that breaks every rule.
    /// </remarks>
    [MemoryDiagnoser]
    public class PartialValidationComparisonBenchmark
    {
        private static readonly NValidationOptions TwoProperties = new() { ValidationProperties = ["Vin", "Mileage"] };

        private NValidation.IValidator<Car> nValidator = null!;

        private FV.IValidator<Car> fluentValidator = null!;

        private Car valid = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.nValidator = new FiveChainCarValidator();
            this.fluentValidator = new FluentFiveChainCarValidator();
            this.valid = Cars.Car();

            var invalid = new Car { Mileage = -1, PurchasePrice = -1m };

            BenchmarkVerdict.RequireTheSame(
                this.nValidator.ValidateAsync(invalid, TwoProperties),
                this.fluentValidator.Validate(invalid, o => o.IncludeProperties("Vin", "Mileage")),
                "two selected properties");
        }

        [Benchmark(Baseline = true, Description = "NValidation (two properties)")]
        public ValueTask<ValidationResult> NValidation_Partial()
        {
            return this.nValidator.ValidateAsync(this.valid, TwoProperties);
        }

        [Benchmark(Description = "FluentValidation sync (two properties)")]
        public FV.Results.ValidationResult Fluent_Partial()
        {
            return this.fluentValidator.Validate(this.valid, o => o.IncludeProperties("Vin", "Mileage"));
        }

        [Benchmark(Description = "NValidation (every property)")]
        public ValueTask<ValidationResult> NValidation_Full()
        {
            return this.nValidator.ValidateAsync(this.valid);
        }

        [Benchmark(Description = "FluentValidation sync (every property)")]
        public FV.Results.ValidationResult Fluent_Full()
        {
            return this.fluentValidator.Validate(this.valid);
        }

        private sealed class FiveChainCarValidator : Validator<Car>
        {
            public FiveChainCarValidator()
            {
                this.Property(c => c.Vin).NotEmpty();
                this.Property(c => c.RegistrationPlate).NotEmpty();
                this.Property(c => c.Mileage).GreaterThanOrEqualTo(0);
                this.Property(c => c.PurchasePrice).GreaterThan(0m);
                this.Property(c => c.Model).NotNull();
            }
        }

        /// <summary>
        /// The same rules, written the FluentValidation way.
        /// </summary>
        private sealed class FluentFiveChainCarValidator : AbstractValidator<Car>
        {
            public FluentFiveChainCarValidator()
            {
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(c => c.Vin).NotEmpty();
                this.RuleFor(c => c.RegistrationPlate).NotEmpty();
                this.RuleFor(c => c.Mileage).GreaterThanOrEqualTo(0);
                this.RuleFor(c => c.PurchasePrice).GreaterThan(0m);
                this.RuleFor(c => c.Model).NotNull();
            }
        }
    }
}
