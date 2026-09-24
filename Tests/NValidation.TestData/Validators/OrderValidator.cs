namespace NValidation.TestData.Validators
{
    /// <summary>
    /// Thirty-four chains over a flat payload, each rule as cheap as the library offers. The point is
    /// the count, not the rules: what a validation costs per chain — reading the property, building the
    /// state a chain runs against, dispatching to it — is what this measures, and a rule that did real
    /// work would hide it.
    /// </summary>
    public sealed class OrderValidator : Validator<Order>
    {
        public OrderValidator()
        {
            this.Property(o => o.OrderNumber).NotEmpty().MaximumLength(32);
            this.Property(o => o.CustomerFirstName).NotEmpty().MaximumLength(100);
            this.Property(o => o.CustomerLastName).NotEmpty().MaximumLength(100);
            this.Property(o => o.CustomerEmail).NotEmpty().MaximumLength(256);
            this.Property(o => o.CustomerPhone).NotEmpty().MaximumLength(32);

            this.Property(o => o.BillingStreet).NotEmpty().MaximumLength(200);
            this.Property(o => o.BillingCity).NotEmpty().MaximumLength(100);
            this.Property(o => o.BillingPostalCode).NotEmpty().MaximumLength(16);
            this.Property(o => o.BillingCountryCode).NotEmpty().Length(3);

            this.Property(o => o.ShippingStreet).NotEmpty().MaximumLength(200);
            this.Property(o => o.ShippingCity).NotEmpty().MaximumLength(100);
            this.Property(o => o.ShippingPostalCode).NotEmpty().MaximumLength(16);
            this.Property(o => o.ShippingCountryCode).NotEmpty().Length(3);

            this.Property(o => o.Currency).NotEmpty().Length(3);
            this.Property(o => o.Locale).NotEmpty().MaximumLength(16);
            this.Property(o => o.CouponCode).MaximumLength(32);
            this.Property(o => o.PurchaseOrderReference).MaximumLength(64);
            this.Property(o => o.VatNumber).MaximumLength(32);
            this.Property(o => o.TrackingNumber).MaximumLength(64);
            this.Property(o => o.ReferralCode).MaximumLength(32);
            this.Property(o => o.GiftMessage).MaximumLength(500);
            this.Property(o => o.DeliveryInstructions).MaximumLength(500);

            this.Property(o => o.Subtotal).GreaterThanOrEqualTo(0m);
            this.Property(o => o.ShippingCost).GreaterThanOrEqualTo(0m);
            this.Property(o => o.TaxAmount).GreaterThanOrEqualTo(0m);
            this.Property(o => o.DiscountAmount).GreaterThanOrEqualTo(0m);
            this.Property(o => o.TotalAmount).GreaterThanOrEqualTo(0m);

            this.Property(o => o.ItemCount).GreaterThanOrEqualTo(1);
            this.Property(o => o.LoyaltyPoints).GreaterThanOrEqualTo(0);
            this.Property(o => o.WeightKg).GreaterThanOrEqualTo(0d);

            this.Property(o => o.PlacedAt).NotDefault();
            this.Property(o => o.RequestedDeliveryDate).GreaterThanOrEqualTo(o => o.PlacedAt);
            this.Property(o => o.ConfirmedAt).GreaterThanOrEqualTo(o => o.PlacedAt);

            this.Property(o => o.AcceptedTerms).EqualTo(true);
        }
    }
}
