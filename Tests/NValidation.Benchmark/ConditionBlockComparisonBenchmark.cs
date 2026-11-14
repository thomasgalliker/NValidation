using BenchmarkDotNet.Attributes;
using FluentValidation;
using NValidation.TestData;
using FV = FluentValidation;

namespace NValidation.Benchmark
{
    /// <summary>
    /// A condition block against FluentValidation's: three chains that only a used car answers for, and
    /// two that every car does, over a used car and a new one.
    /// </summary>
    /// <remarks>
    /// NValidation asks the block's condition once per chain in it, and allocates nothing to remember the
    /// answer; the rows show what that costs where the condition holds and where it does not.
    /// <see cref="Setup"/> first holds both sides to reporting the same properties for a used car missing
    /// everything the block asks for.
    /// </remarks>
    [MemoryDiagnoser]
    public class ConditionBlockComparisonBenchmark
    {
        private NValidation.IValidator<Car> nValidator = null!;

        private FV.IValidator<Car> fluentValidator = null!;

        private Car used = null!;

        private Car brandNew = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.nValidator = new UsedCarValidator();
            this.fluentValidator = new FluentUsedCarValidator();

            this.used = Cars.Car();
            this.used.Condition = CarCondition.Used;

            this.brandNew = Cars.Car();
            this.brandNew.Condition = CarCondition.New;

            var incomplete = Cars.Car();
            incomplete.Condition = CarCondition.Used;
            incomplete.IntakeCondition = null;
            incomplete.ServiceIntervalKm = null;
            incomplete.PreviousRegistrationPlate = null;

            BenchmarkVerdict.RequireTheSame(this.nValidator, this.fluentValidator, incomplete);
        }

        [Benchmark(Baseline = true, Description = "NValidation (condition holds)")]
        public ValueTask<ValidationResult> NValidation_Holds()
        {
            return this.nValidator.ValidateAsync(this.used);
        }

        [Benchmark(Description = "FluentValidation sync (condition holds)")]
        public FV.Results.ValidationResult Fluent_Holds()
        {
            return this.fluentValidator.Validate(this.used);
        }

        [Benchmark(Description = "NValidation (condition fails)")]
        public ValueTask<ValidationResult> NValidation_Fails()
        {
            return this.nValidator.ValidateAsync(this.brandNew);
        }

        [Benchmark(Description = "FluentValidation sync (condition fails)")]
        public FV.Results.ValidationResult Fluent_Fails()
        {
            return this.fluentValidator.Validate(this.brandNew);
        }

        private sealed class UsedCarValidator : Validator<Car>
        {
            public UsedCarValidator()
            {
                this.Property(c => c.Vin).NotEmpty();
                this.Property(c => c.RegistrationPlate).NotEmpty();

                this.When(static c => c.Condition == CarCondition.Used, () =>
                {
                    this.Property(c => c.IntakeCondition).NotNull();
                    this.Property(c => c.ServiceIntervalKm).NotNull();
                    this.Property(c => c.PreviousRegistrationPlate).NotEmpty();
                });
            }
        }

        /// <summary>
        /// The same rules and the same block, written the FluentValidation way.
        /// </summary>
        private sealed class FluentUsedCarValidator : AbstractValidator<Car>
        {
            public FluentUsedCarValidator()
            {
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(c => c.Vin).NotEmpty();
                this.RuleFor(c => c.RegistrationPlate).NotEmpty();

                this.When(static c => c.Condition == CarCondition.Used, () =>
                {
                    this.RuleFor(c => c.IntakeCondition).NotNull();
                    this.RuleFor(c => c.ServiceIntervalKm).NotNull();
                    this.RuleFor(c => c.PreviousRegistrationPlate).NotEmpty();
                });
            }
        }
    }
}
