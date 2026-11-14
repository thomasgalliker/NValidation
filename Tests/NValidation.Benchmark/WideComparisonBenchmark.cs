using BenchmarkDotNet.Attributes;
using FluentValidation;
using NValidation.TestData;
using NValidation.TestData.Validators;
using FV = FluentValidation;

namespace NValidation.Benchmark
{
    /// <summary>
    /// The same head-to-head as <see cref="ComparisonBenchmark"/>, on a payload thirty-four chains wide
    /// instead of four.
    /// </summary>
    /// <remarks>
    /// Four chains is a floor, not a request. What a validation costs per chain is the term that scales
    /// with a real payload, and a four-chain measurement barely contains it — so a difference that looks
    /// small on <see cref="Manufacturer"/> is measured here where it has room to show.
    /// </remarks>
    [MemoryDiagnoser]
    public class WideComparisonBenchmark
    {
        private NValidation.IValidator<Order> nValidator = null!;
        private FV.IValidator<Order> fluentValidator = null!;

        private Order valid = null!;
        private Order invalid = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.nValidator = new OrderValidator();
            this.fluentValidator = new FluentOrderValidator();

            this.valid = Orders.Order();

            this.invalid = Orders.Order();
            this.invalid.OrderNumber = null;
            this.invalid.BillingCountryCode = "TOO-LONG";
            this.invalid.ItemCount = 0;
            this.invalid.TotalAmount = -1m;

            BenchmarkVerdict.RequireTheSame(this.nValidator, this.fluentValidator, this.valid);
            BenchmarkVerdict.RequireTheSame(this.nValidator, this.fluentValidator, this.invalid);
        }

        [Benchmark(Baseline = true, Description = "NValidation wide (valid)")]
        public ValueTask<ValidationResult> NValidation_Valid()
        {
            return this.nValidator.ValidateAsync(this.valid);
        }

        [Benchmark(Description = "FluentValidation sync wide (valid)")]
        public FV.Results.ValidationResult Fluent_Sync_Valid()
        {
            return this.fluentValidator.Validate(this.valid);
        }

        [Benchmark(Description = "NValidation wide (invalid)")]
        public ValueTask<ValidationResult> NValidation_Invalid()
        {
            return this.nValidator.ValidateAsync(this.invalid);
        }

        [Benchmark(Description = "FluentValidation sync wide (invalid)")]
        public FV.Results.ValidationResult Fluent_Sync_Invalid()
        {
            return this.fluentValidator.Validate(this.invalid);
        }

        /// <summary>
        /// The same rules as <see cref="OrderValidator"/>, written the FluentValidation way.
        /// </summary>
        private sealed class FluentOrderValidator : AbstractValidator<Order>
        {
            public FluentOrderValidator()
            {
                this.ClassLevelCascadeMode = CascadeMode.Continue;
                this.RuleLevelCascadeMode = CascadeMode.Stop;

                this.RuleFor(o => o.OrderNumber).NotEmpty().MaximumLength(32);
                this.RuleFor(o => o.CustomerFirstName).NotEmpty().MaximumLength(100);
                this.RuleFor(o => o.CustomerLastName).NotEmpty().MaximumLength(100);
                this.RuleFor(o => o.CustomerEmail).NotEmpty().MaximumLength(256);
                this.RuleFor(o => o.CustomerPhone).NotEmpty().MaximumLength(32);

                this.RuleFor(o => o.BillingStreet).NotEmpty().MaximumLength(200);
                this.RuleFor(o => o.BillingCity).NotEmpty().MaximumLength(100);
                this.RuleFor(o => o.BillingPostalCode).NotEmpty().MaximumLength(16);
                this.RuleFor(o => o.BillingCountryCode).NotEmpty().Length(3);

                this.RuleFor(o => o.ShippingStreet).NotEmpty().MaximumLength(200);
                this.RuleFor(o => o.ShippingCity).NotEmpty().MaximumLength(100);
                this.RuleFor(o => o.ShippingPostalCode).NotEmpty().MaximumLength(16);
                this.RuleFor(o => o.ShippingCountryCode).NotEmpty().Length(3);

                this.RuleFor(o => o.Currency).NotEmpty().Length(3);
                this.RuleFor(o => o.Locale).NotEmpty().MaximumLength(16);
                this.RuleFor(o => o.CouponCode).MaximumLength(32);
                this.RuleFor(o => o.PurchaseOrderReference).MaximumLength(64);
                this.RuleFor(o => o.VatNumber).MaximumLength(32);
                this.RuleFor(o => o.TrackingNumber).MaximumLength(64);
                this.RuleFor(o => o.ReferralCode).MaximumLength(32);
                this.RuleFor(o => o.GiftMessage).MaximumLength(500);
                this.RuleFor(o => o.DeliveryInstructions).MaximumLength(500);

                this.RuleFor(o => o.Subtotal).GreaterThanOrEqualTo(0m);
                this.RuleFor(o => o.ShippingCost).GreaterThanOrEqualTo(0m);
                this.RuleFor(o => o.TaxAmount).GreaterThanOrEqualTo(0m);
                this.RuleFor(o => o.DiscountAmount).GreaterThanOrEqualTo(0m);
                this.RuleFor(o => o.TotalAmount).GreaterThanOrEqualTo(0m);

                this.RuleFor(o => o.ItemCount).GreaterThanOrEqualTo(1);
                this.RuleFor(o => o.LoyaltyPoints).GreaterThanOrEqualTo(0);
                this.RuleFor(o => o.WeightKg).GreaterThanOrEqualTo(0d);

                this.RuleFor(o => o.PlacedAt).NotEqual(default(DateTime));
                this.RuleFor(o => o.RequestedDeliveryDate).GreaterThanOrEqualTo(o => o.PlacedAt);
                this.RuleFor(o => o.ConfirmedAt).GreaterThanOrEqualTo(o => o.PlacedAt);

                this.RuleFor(o => o.AcceptedTerms).Equal(true);
            }
        }
    }
}
