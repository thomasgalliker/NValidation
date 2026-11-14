using BenchmarkDotNet.Attributes;
using FluentValidation;
using NValidation.TestData;
using FV = FluentValidation;

namespace NValidation.Benchmark
{
    /// <summary>
    /// Data a caller hands the rules, against FluentValidation's root context data: one rule and one
    /// condition read a listing policy the car cannot answer for itself.
    /// </summary>
    /// <remarks>
    /// NValidation is measured twice — with options built once and reused, as an application keeps them,
    /// and with options built for every call. FluentValidation's context carries the payload itself, so it
    /// is built for every call however the application is written. <see cref="Setup"/> first holds both
    /// sides to reporting the same properties for a car the policy rejects twice over.
    /// </remarks>
    [MemoryDiagnoser]
    public class ValidationDataComparisonBenchmark
    {
        private const string PolicyKey = nameof(ListingPolicy);

        private static readonly ListingPolicy Policy = new(MaximumMileage: 200_000, RequiresServiceHistory: true);

        private static readonly NValidationOptions WithPolicy = new() { ValidationData = [Policy] };

        private NValidation.IValidator<Car> nValidator = null!;

        private FV.IValidator<Car> fluentValidator = null!;

        private Car valid = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.nValidator = new PolicyCarValidator();
            this.fluentValidator = new FluentPolicyCarValidator();

            this.valid = Cars.Car();
            this.valid.ServiceHistory = [new ServiceRecord { Workshop = "Aurora", Mileage = 10_000, Cost = 120m }];

            var invalid = Cars.Car();
            invalid.Mileage = 250_000;
            invalid.ServiceHistory = null;

            BenchmarkVerdict.RequireTheSame(
                this.nValidator.ValidateAsync(invalid, WithPolicy),
                this.fluentValidator.Validate(FluentContext(invalid)),
                "a car the policy rejects");
        }

        [Benchmark(Baseline = true, Description = "NValidation (options reused)")]
        public ValueTask<ValidationResult> NValidation_Reused()
        {
            return this.nValidator.ValidateAsync(this.valid, WithPolicy);
        }

        [Benchmark(Description = "NValidation (options per call)")]
        public ValueTask<ValidationResult> NValidation_PerCall()
        {
            return this.nValidator.ValidateAsync(this.valid, new NValidationOptions { ValidationData = [Policy] });
        }

        [Benchmark(Description = "FluentValidation sync (root context data)")]
        public FV.Results.ValidationResult Fluent()
        {
            return this.fluentValidator.Validate(FluentContext(this.valid));
        }

        private static ValidationContext<Car> FluentContext(Car car)
        {
            var context = new ValidationContext<Car>(car);
            context.RootContextData[PolicyKey] = Policy;

            return context;
        }

        private sealed class PolicyCarValidator : Validator<Car>
        {
            public PolicyCarValidator()
            {
                this.Property(c => c.Vin).NotEmpty();

                this.Property(c => c.Mileage)
                    .Must<ListingPolicy>(static (_, mileage, policy) => mileage <= policy.MaximumMileage);

                this.Property(c => c.ServiceHistory)
                    .NotEmpty()
                    .When<ListingPolicy>(static (_, policy) => policy.RequiresServiceHistory);
            }
        }

        /// <summary>
        /// The same rules, reading the policy the FluentValidation way: out of the root context data by
        /// key, and cast. A missing policy passes, as it does on the NValidation side.
        /// </summary>
        private sealed class FluentPolicyCarValidator : AbstractValidator<Car>
        {
            public FluentPolicyCarValidator()
            {
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(c => c.Vin).NotEmpty();

                this.RuleFor(c => c.Mileage)
                    .Must(static (_, mileage, context) =>
                        !context.RootContextData.TryGetValue(PolicyKey, out var policy) ||
                        mileage <= ((ListingPolicy)policy).MaximumMileage);

                this.RuleFor(c => c.ServiceHistory)
                    .NotEmpty()
                    .When(static (_, context) =>
                        context.RootContextData.TryGetValue(PolicyKey, out var policy) &&
                        ((ListingPolicy)policy).RequiresServiceHistory);
            }
        }
    }
}
