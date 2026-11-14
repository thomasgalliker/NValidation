using BenchmarkDotNet.Attributes;
using NValidation.TestData;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What one property chain costs, isolated: the same rule over the same payload, declared a varying
    /// number of times, so the slope across <see cref="Chains"/> is the per-chain term and the value at
    /// zero is everything a validation costs before any chain runs.
    /// </summary>
    /// <remarks>
    /// Chains are declared without expressions, so what is measured is validating rather than whatever
    /// building the chains happened to cost.
    /// </remarks>
    [MemoryDiagnoser]
    public class ChainCostBenchmark
    {
        private readonly Dictionary<int, NValidation.IValidator<Manufacturer>> byChainCount = [];

        private Manufacturer manufacturer = null!;

        [Params(0, 1, 2, 4, 8, 16)]
        public int Chains { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();

            foreach (var count in new[] { 0, 1, 2, 4, 8, 16 })
            {
                this.byChainCount[count] = new NChainValidator(count);
            }
        }

        [Benchmark]
        public ValueTask<ValidationResult> Validate()
        {
            return this.byChainCount[this.Chains].ValidateAsync(this.manufacturer);
        }

        private sealed class NChainValidator : Validator<Manufacturer>
        {
            public NChainValidator(int chains)
            {
                for (var i = 0; i < chains; i++)
                {
                    this.Property($"Name{i}", static m => m.Name).NotEmpty();
                }
            }
        }
    }

    /// <summary>
    /// What one rule costs on a value it accepts. Both validators are one chain over one property, so
    /// the difference between them is the rule and nothing else.
    /// </summary>
    [MemoryDiagnoser]
    public class RuleCostBenchmark
    {
        private NValidation.IValidator<Manufacturer> cheapRule = null!;
        private NValidation.IValidator<Manufacturer> emailRule = null!;
        private NValidation.IValidator<Manufacturer> patternRule = null!;

        private Manufacturer manufacturer = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();

            this.cheapRule = new OneRuleValidator(builder => builder.NotEmpty());
            this.emailRule = new OneRuleValidator(builder => builder.EmailAddress());
            this.patternRule = new OneRuleValidator(builder => builder.MaximumLength(256));
        }

        /// <summary>
        /// The control: a rule which allocates nothing and reads one field.
        /// </summary>
        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> NotEmpty()
        {
            return this.cheapRule.ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> MaximumLength()
        {
            return this.patternRule.ValidateAsync(this.manufacturer);
        }

        /// <summary>
        /// The rule under suspicion: its difference from <see cref="NotEmpty"/> is what accepting a
        /// valid address costs.
        /// </summary>
        [Benchmark]
        public ValueTask<ValidationResult> EmailAddress()
        {
            return this.emailRule.ValidateAsync(this.manufacturer);
        }

        private sealed class OneRuleValidator : Validator<Manufacturer>
        {
            public OneRuleValidator(Action<PropertyRuleBuilder<Manufacturer, string?>> declare)
            {
                declare(this.Property("ContactEmail", static m => m.ContactEmail));
            }
        }
    }
}
