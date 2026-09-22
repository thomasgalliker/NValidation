using BenchmarkDotNet.Attributes;
using NValidation.TestData;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What rule groups cost: the chains in no group are the baseline the gate must not move, and the
    /// grouped validator is measured both where its group was selected and where it was not, so the
    /// price of a chain a run skips is visible on its own.
    /// </summary>
    /// <remarks>
    /// Chains are declared without expressions, so what is measured is validating rather than whatever
    /// building the chains happened to cost.
    /// </remarks>
    [MemoryDiagnoser]
    public class GroupBenchmark
    {
        private readonly Dictionary<int, NValidation.IValidator<Manufacturer>> ungrouped = [];

        private readonly Dictionary<int, NValidation.IValidator<Manufacturer>> grouped = [];

        private static readonly NValidationOptions Selected = new() { ValidationGroups = "Create" };

        private static readonly NValidationOptions Everything = new() { ValidationGroups = ValidationGroups.All };

        private Manufacturer manufacturer = null!;

        [Params(4, 16)]
        public int Chains { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();

            foreach (var count in new[] { 4, 16 })
            {
                this.ungrouped[count] = new NChainValidator(count, group: null);
                this.grouped[count] = new NChainValidator(count, group: "Create");
            }
        }

        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> Ungrouped()
        {
            return this.ungrouped[this.Chains].ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> GroupedAndSelected()
        {
            return this.grouped[this.Chains].ValidateAsync(this.manufacturer, Selected);
        }

        [Benchmark]
        public ValueTask<ValidationResult> GroupedAndSkipped()
        {
            return this.grouped[this.Chains].ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> GroupedUnderAll()
        {
            return this.grouped[this.Chains].ValidateAsync(this.manufacturer, Everything);
        }

        private sealed class NChainValidator : Validator<Manufacturer>
        {
            public NChainValidator(int chains, string? group)
            {
                for (var i = 0; i < chains; i++)
                {
                    var chain = this.Property($"Name{i}", static m => m.Name).NotEmpty();

                    if (group != null)
                    {
                        chain.WithGroup(group);
                    }
                }
            }
        }
    }
}
