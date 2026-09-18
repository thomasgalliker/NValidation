using BenchmarkDotNet.Attributes;
using NValidation.TestData;
using NValidation.TestData.Validators;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What a validation costs when the rules have nothing to do: every chain of a wide validator
    /// short-circuits because the value it guards is absent. The difference from the same validator over
    /// a populated payload is what the rules themselves cost; what is left is the framework.
    /// </summary>
    [MemoryDiagnoser]
    public class FloorBenchmark
    {
        private NValidation.IValidator<Order> validator = null!;
        private Order populated = null!;
        private Order absent = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.validator = new OrderValidator();
            this.populated = Orders.Order();

            // Every optional text chain passes on a missing value, so the chains run and stop at once.
            this.absent = Orders.Order();
            this.absent.CouponCode = null;
            this.absent.PurchaseOrderReference = null;
            this.absent.VatNumber = null;
            this.absent.TrackingNumber = null;
            this.absent.ReferralCode = null;
            this.absent.GiftMessage = null;
            this.absent.DeliveryInstructions = null;
            this.absent.ConfirmedAt = null;
        }

        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> Populated()
        {
            return this.validator.ValidateAsync(this.populated);
        }

        [Benchmark]
        public ValueTask<ValidationResult> MostlyAbsent()
        {
            return this.validator.ValidateAsync(this.absent);
        }
    }

    /// <summary>
    /// What a <c>When</c> costs. Conditions fold into one another as they are declared, so a chain with
    /// three of them asks three nested delegates before it reads the property at all.
    /// </summary>
    [MemoryDiagnoser]
    public class ConditionBenchmark
    {
        private readonly Dictionary<int, NValidation.IValidator<Manufacturer>> byConditionCount = [];

        private Manufacturer manufacturer = null!;

        [Params(0, 1, 3)]
        public int Conditions { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();

            foreach (var count in new[] { 0, 1, 3 })
            {
                this.byConditionCount[count] = new ConditionalValidator(count);
            }
        }

        [Benchmark]
        public ValueTask<ValidationResult> Validate()
        {
            return this.byConditionCount[this.Conditions].ValidateAsync(this.manufacturer);
        }

        private sealed class ConditionalValidator : Validator<Manufacturer>
        {
            public ConditionalValidator(int conditions)
            {
                var builder = this.Property("Name", static m => m.Name).NotEmpty();

                for (var i = 0; i < conditions; i++)
                {
                    builder = builder.When(static m => m.Id >= 0);
                }
            }
        }
    }
}
