using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FluentValidation;
using NValidation.TestData;
using FV = FluentValidation;

namespace NValidation.Benchmark
{
    /// <summary>
    /// Rule groups against FluentValidation's rule sets on the same validator: two chains every
    /// validation runs and two in a group, selected four ways — nothing, the group on top of the rest, the
    /// group alone, and everything.
    /// </summary>
    /// <remarks>
    /// Each way of selecting is a category of its own, with NValidation as its baseline, so a ratio
    /// compares like with like. <see cref="Setup"/> first holds both sides of every category to reporting
    /// the same properties for a payload that breaks one chain of each kind. What is measured is the valid
    /// payload, the common case. The options NValidation is handed are built once, as an application
    /// would keep them; FluentValidation is called the way its selection is written.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class GroupSelectionComparisonBenchmark
    {
        private static readonly NValidationOptions Additive = new() { ValidationGroups = "Create" };

        private static readonly NValidationOptions Exclusive = new() { ValidationGroups = ValidationGroups.Only("Create") };

        private static readonly NValidationOptions Everything = new() { ValidationGroups = ValidationGroups.All };

        private NValidation.IValidator<Manufacturer> nValidator = null!;

        private FV.IValidator<Manufacturer> fluentValidator = null!;

        private Manufacturer valid = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.nValidator = new GroupedManufacturerValidator();
            this.fluentValidator = new FluentGroupedManufacturerValidator();
            this.valid = Cars.Manufacturer();

            var invalid = Cars.Manufacturer();
            invalid.Name = null;
            invalid.CountryCode = null;

            BenchmarkVerdict.RequireTheSame(
                this.nValidator.ValidateAsync(invalid), this.fluentValidator.Validate(invalid), "the default group");
            BenchmarkVerdict.RequireTheSame(
                this.nValidator.ValidateAsync(invalid, Additive),
                this.fluentValidator.Validate(invalid, o => o.IncludeRuleSets("Create").IncludeRulesNotInRuleSet()),
                "a group on top of the rest");
            BenchmarkVerdict.RequireTheSame(
                this.nValidator.ValidateAsync(invalid, Exclusive),
                this.fluentValidator.Validate(invalid, o => o.IncludeRuleSets("Create")),
                "a group alone");
            BenchmarkVerdict.RequireTheSame(
                this.nValidator.ValidateAsync(invalid, Everything),
                this.fluentValidator.Validate(invalid, o => o.IncludeAllRuleSets()),
                "every group");
        }

        [BenchmarkCategory("Default")]
        [Benchmark(Baseline = true, Description = "NValidation (default group)")]
        public ValueTask<ValidationResult> NValidation_Default()
        {
            return this.nValidator.ValidateAsync(this.valid);
        }

        [BenchmarkCategory("Default")]
        [Benchmark(Description = "FluentValidation sync (rules in no set)")]
        public FV.Results.ValidationResult Fluent_Default()
        {
            return this.fluentValidator.Validate(this.valid);
        }

        [BenchmarkCategory("Additive")]
        [Benchmark(Baseline = true, Description = "NValidation (Create on top)")]
        public ValueTask<ValidationResult> NValidation_Additive()
        {
            return this.nValidator.ValidateAsync(this.valid, Additive);
        }

        [BenchmarkCategory("Additive")]
        [Benchmark(Description = "FluentValidation sync (Create and rules in no set)")]
        public FV.Results.ValidationResult Fluent_Additive()
        {
            return this.fluentValidator.Validate(this.valid, o => o.IncludeRuleSets("Create").IncludeRulesNotInRuleSet());
        }

        [BenchmarkCategory("Exclusive")]
        [Benchmark(Baseline = true, Description = "NValidation (Only Create)")]
        public ValueTask<ValidationResult> NValidation_Exclusive()
        {
            return this.nValidator.ValidateAsync(this.valid, Exclusive);
        }

        [BenchmarkCategory("Exclusive")]
        [Benchmark(Description = "FluentValidation sync (Create alone)")]
        public FV.Results.ValidationResult Fluent_Exclusive()
        {
            return this.fluentValidator.Validate(this.valid, o => o.IncludeRuleSets("Create"));
        }

        [BenchmarkCategory("All")]
        [Benchmark(Baseline = true, Description = "NValidation (All)")]
        public ValueTask<ValidationResult> NValidation_All()
        {
            return this.nValidator.ValidateAsync(this.valid, Everything);
        }

        [BenchmarkCategory("All")]
        [Benchmark(Description = "FluentValidation sync (all sets)")]
        public FV.Results.ValidationResult Fluent_All()
        {
            return this.fluentValidator.Validate(this.valid, o => o.IncludeAllRuleSets());
        }

        private sealed class GroupedManufacturerValidator : Validator<Manufacturer>
        {
            public GroupedManufacturerValidator()
            {
                this.Property(m => m.Name).NotEmpty().MaximumLength(100);
                this.Property(m => m.ContactEmail).NotEmpty();

                this.Group("Create", () =>
                {
                    this.Property(m => m.CountryCode).NotEmpty().Length(3);
                    this.Property(m => m.Website).NotEmpty();
                });
            }
        }

        /// <summary>
        /// The same rules and the same group, written the FluentValidation way.
        /// </summary>
        private sealed class FluentGroupedManufacturerValidator : AbstractValidator<Manufacturer>
        {
            public FluentGroupedManufacturerValidator()
            {
                // NValidation reports every property but stops each property's chain at its first
                // failure. Matched here rather than left at FluentValidation's own defaults.
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(m => m.Name).NotEmpty().MaximumLength(100);
                this.RuleFor(m => m.ContactEmail).NotEmpty();

                this.RuleSet("Create", () =>
                {
                    this.RuleFor(m => m.CountryCode).NotEmpty().Length(3);
                    this.RuleFor(m => m.Website).NotEmpty();
                });
            }
        }
    }
}
