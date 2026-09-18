namespace NValidation.TestData
{
    public static class Orders
    {
        /// <summary>
        /// An order every rule accepts, so a run over it measures what checking costs rather than what
        /// reporting costs.
        /// </summary>
        public static Order Order()
        {
            return new Order
            {
                OrderNumber = "ORD-2026-0001",
                CustomerFirstName = "Alex",
                CustomerLastName = "Meier",
                CustomerEmail = "alex.meier@aurora-motors.example",
                CustomerPhone = "+41445556677",
                BillingStreet = "Bahnhofstrasse 1",
                BillingCity = "Zurich",
                BillingPostalCode = "8001",
                BillingCountryCode = "CHE",
                ShippingStreet = "Seestrasse 12",
                ShippingCity = "Lucerne",
                ShippingPostalCode = "6003",
                ShippingCountryCode = "CHE",
                Currency = "CHF",
                Locale = "de-CH",
                CouponCode = "SPRING26",
                PurchaseOrderReference = "PO-88213",
                VatNumber = "CHE-123.456.789",
                TrackingNumber = "TRK-99120031",
                ReferralCode = "REF-2210",
                GiftMessage = "Congratulations on the new car.",
                DeliveryInstructions = "Leave with the concierge.",
                Subtotal = 48_500.00m,
                ShippingCost = 350.00m,
                TaxAmount = 3_757.75m,
                DiscountAmount = 500.00m,
                TotalAmount = 52_107.75m,
                ItemCount = 3,
                LoyaltyPoints = 1_250,
                WeightKg = 1_820.5,
                PlacedAt = new DateTime(2026, 3, 2, 9, 30, 0, DateTimeKind.Utc),
                RequestedDeliveryDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                ConfirmedAt = new DateTime(2026, 3, 2, 9, 31, 0, DateTimeKind.Utc),
                AcceptedTerms = true,
                IsGift = false,
            };
        }
    }
}
