namespace NValidation.TestData
{
    /// <summary>
    /// A wide, flat payload: the checkout form shape, where a request carries several dozen fields and
    /// almost all of them are fine. The rest of the test domain is deliberately deep and small
    /// (<see cref="Car"/> validates seven chains); this one is deliberately shallow and broad, because
    /// what one property chain costs is only visible when there are enough of them for the slope to
    /// show.
    /// </summary>
    public class Order
    {
        public string? OrderNumber { get; set; }

        public string? CustomerFirstName { get; set; }

        public string? CustomerLastName { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CustomerPhone { get; set; }

        public string? BillingStreet { get; set; }

        public string? BillingCity { get; set; }

        public string? BillingPostalCode { get; set; }

        public string? BillingCountryCode { get; set; }

        public string? ShippingStreet { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingPostalCode { get; set; }

        public string? ShippingCountryCode { get; set; }

        public string? Currency { get; set; }

        public string? Locale { get; set; }

        public string? CouponCode { get; set; }

        public string? PurchaseOrderReference { get; set; }

        public string? VatNumber { get; set; }

        public string? TrackingNumber { get; set; }

        public string? ReferralCode { get; set; }

        public string? GiftMessage { get; set; }

        public string? DeliveryInstructions { get; set; }

        public decimal Subtotal { get; set; }

        public decimal ShippingCost { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public int ItemCount { get; set; }

        public int LoyaltyPoints { get; set; }

        public double WeightKg { get; set; }

        public DateTime PlacedAt { get; set; }

        public DateTime RequestedDeliveryDate { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public bool AcceptedTerms { get; set; }

        public bool IsGift { get; set; }
    }
}
