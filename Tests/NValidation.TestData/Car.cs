namespace NValidation.TestData
{
    public class Car
    {
        /// <summary>
        /// Vehicle identification number: exactly 17 characters, which no shipped rule covers on its
        /// own — so it is the subject for <c>Must</c> and for <c>Matches</c>.
        /// </summary>
        public string? Vin { get; set; }

        public CarModel? Model { get; set; }

        public CarCondition Condition { get; set; }

        /// <summary>
        /// Which way the selected gear drives the car. Negative for reverse.
        /// </summary>
        public GearDirection GearDirection { get; set; }

        public CarEquipment Equipment { get; set; }

        public int Mileage { get; set; }

        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// Absent until the car has been appraised, so the nullable-value rules have a subject.
        /// </summary>
        public decimal? TradeInValue { get; set; }

        public bool IsListedForSale { get; set; }

        public DateTime FirstRegistration { get; set; }

        /// <summary>
        /// Compared against <see cref="FirstRegistration"/>: a car cannot be sold before it was first
        /// registered.
        /// </summary>
        public DateTime? SoldDate { get; set; }

        /// <summary>
        /// Carries its own offset, so the date rules have a subject with no time-zone ambiguity.
        /// </summary>
        public DateTimeOffset RegisteredAt { get; set; }

        public DateTimeOffset? NextServiceAt { get; set; }

        /// <summary>
        /// The other end of a both-nullable comparison against <see cref="SoldDate"/>.
        /// </summary>
        public DateTime? WarrantyEndsOn { get; set; }

        /// <summary>
        /// The other end of a comparison where neither side is nullable: the mileage at which the
        /// warranty lapses.
        /// </summary>
        public int WarrantyMileageLimit { get; set; }

        public TimeSpan ServiceInterval { get; set; }

        public ICollection<int>? FeatureIds { get; set; }

        /// <summary>
        /// Declared as a concrete list rather than an interface, so the collection rules are proven to
        /// bind whatever the property's declared type is.
        /// </summary>
        public List<Guid>? PreviousOwnerIds { get; set; }

        /// <summary>
        /// Declared as a bare sequence rather than a list, because a service history is often
        /// read straight off a query. What is behind it may only be walked once.
        /// </summary>
        public IEnumerable<int>? ServiceMileages { get; set; }

        /// <summary>
        /// The service history proper. A collection of a nested type, which is what the
        /// per-element rules are declared against.
        /// </summary>
        public List<ServiceRecord>? ServiceHistory { get; set; }
    }
}
