using BenchmarkDotNet.Attributes;
using FluentValidation;
using NValidation.TestData;
using NValidation.TestData.Validators;
using FV = FluentValidation;

namespace NValidation.Benchmark
{
    /// <summary>
    /// NValidation against FluentValidation on the same payload, in one run, on one machine.
    /// </summary>
    /// <remarks>
    /// The two validators are held to the same rules, and <see cref="Setup"/> asserts that they report
    /// the same set of properties before anything is measured — a comparison where one library checks
    /// four things and the other five is not a comparison.
    /// <para>
    /// Three columns, not one. NValidation is asynchronous only and returns a
    /// <see cref="ValueTask{TResult}"/>, which allocates nothing when every rule completes
    /// synchronously. FluentValidation offers both, and its asynchronous form returns a
    /// <see cref="Task{TResult}"/>, which allocates even then. Measuring only against the form that
    /// flatters NValidation would be the kind of table nobody should trust, so both are here.
    /// </para>
    /// <para>
    /// Cascade behaviour is matched explicitly: NValidation defaults to reporting every property but
    /// stopping a property's chain at its first failure, so FluentValidation is configured the same way
    /// rather than left at its own defaults.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class ComparisonBenchmark
    {
        private NValidation.IValidator<Manufacturer> nValidator = null!;
        private FV.IValidator<Manufacturer> fluentValidator = null!;

        private Manufacturer valid = null!;
        private Manufacturer invalid = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.nValidator = new ManufacturerValidator();
            this.fluentValidator = new FluentManufacturerValidator();

            this.valid = Cars.Manufacturer();

            this.invalid = Cars.Manufacturer();
            this.invalid.Name = null;
            this.invalid.CountryCode = "TOO-LONG";
            this.invalid.ContactEmail = "not-an-address";

            this.RequireTheSameVerdict(this.valid);
            this.RequireTheSameVerdict(this.invalid);
        }

        [Benchmark(Baseline = true, Description = "NValidation ValueTask (valid)")]
        public ValueTask<ValidationResult> NValidation_Valid()
        {
            return this.nValidator.ValidateAsync(this.valid);
        }

        [Benchmark(Description = "FluentValidation sync (valid)")]
        public FV.Results.ValidationResult Fluent_Sync_Valid()
        {
            return this.fluentValidator.Validate(this.valid);
        }

        [Benchmark(Description = "FluentValidation async (valid)")]
        public Task<FV.Results.ValidationResult> Fluent_Async_Valid()
        {
            return this.fluentValidator.ValidateAsync(this.valid);
        }

        [Benchmark(Description = "NValidation ValueTask (invalid)")]
        public ValueTask<ValidationResult> NValidation_Invalid()
        {
            return this.nValidator.ValidateAsync(this.invalid);
        }

        [Benchmark(Description = "FluentValidation sync (invalid)")]
        public FV.Results.ValidationResult Fluent_Sync_Invalid()
        {
            return this.fluentValidator.Validate(this.invalid);
        }

        [Benchmark(Description = "FluentValidation async (invalid)")]
        public Task<FV.Results.ValidationResult> Fluent_Async_Invalid()
        {
            return this.fluentValidator.ValidateAsync(this.invalid);
        }

        /// <summary>
        /// Both libraries must blame the same properties, or the numbers below measure different work.
        /// </summary>
        /// <remarks>
        /// Property names are compared rather than messages: the wording is each library's own, and
        /// holding them to identical text would be a maintenance tax that says nothing about speed.
        /// </remarks>
        private void RequireTheSameVerdict(Manufacturer manufacturer)
        {
            var reportedByNValidation = this.nValidator.ValidateAsync(manufacturer).GetAwaiter().GetResult()
                .Errors.Select(error => error.PropertyName).Order().ToArray();

            var reportedByFluentValidation = this.fluentValidator.Validate(manufacturer)
                .Errors.Select(failure => failure.PropertyName).Order().ToArray();

            if (!reportedByNValidation.SequenceEqual(reportedByFluentValidation, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The two validators do not agree, so comparing them would be meaningless. " +
                    $"NValidation reported [{string.Join(", ", reportedByNValidation)}] and " +
                    $"FluentValidation reported [{string.Join(", ", reportedByFluentValidation)}].");
            }
        }

        /// <summary>
        /// The same rules as <see cref="ManufacturerValidator"/>, written the FluentValidation way.
        /// </summary>
        private sealed class FluentManufacturerValidator : AbstractValidator<Manufacturer>
        {
            public FluentManufacturerValidator()
            {
                // NValidation reports every property but stops each property's chain at its first
                // failure. Matched here rather than left at FluentValidation's own defaults.
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(m => m.Name)
                    .NotEmpty()
                    .MaximumLength(100);

                this.RuleFor(m => m.CountryCode)
                    .NotEmpty()
                    .Length(3);

                this.RuleFor(m => m.FoundedDate)
                    .LessThan(_ => DateTime.UtcNow);

                this.RuleFor(m => m.ContactEmail)
                    .EmailAddress()
                    .When(m => !string.IsNullOrWhiteSpace(m.ContactEmail));
            }
        }
    }
}
