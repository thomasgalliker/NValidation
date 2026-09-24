namespace NValidation.TestData.Validators
{
    /// <summary>
    /// What a whole car has to satisfy, and the one validator the sample API exposes. Deliberately not a
    /// list of one-rule properties: it declares a plain rule, a rule of its own, a nested validator, a
    /// comparison against a sibling property, and a collection whose entries have a validator of their
    /// own; a chain which runs only in the <see cref="CreateGroup"/> group; and the chains a listing
    /// check runs on its own with <c>ValidationGroups.Only(ListingGroup)</c> — one of which every other
    /// validation runs too, two of which read the <see cref="ListingPolicy"/> the caller hands over, and two
    /// of which only a used car answers for — so a scenario test can exercise all of those at once, the way
    /// a real payload does.
    /// </summary>
    public sealed class CarValidator : Validator<Car>
    {
        public const int VinLength = 17;

        public const int MaximumServiceRecords = 20;

        /// <summary>
        /// The group of the chains which only apply when a car is taken in, not when one already on the
        /// books is corrected.
        /// </summary>
        public const string CreateGroup = "Create";

        /// <summary>
        /// The group of what a car needs before it is offered for sale. A listing check selects it alone,
        /// so it hears about exactly that and not about the rest of the car.
        /// </summary>
        public const string ListingGroup = "Listing";

        public CarValidator(IValidator<CarModel> carModelValidator, IValidator<ServiceRecord> serviceRecordValidator)
        {
            // A null VIN is already reported by NotEmpty, and the chain stops there, so the predicate
            // only has to describe the shape of a VIN which is present.
            this.Property(c => c.Vin)
                .NotEmpty()
                .Must(vin => vin == null || vin.Trim().Length == VinLength)
                .WithMessage("The VIN must be exactly 17 characters long.");

            this.Property(c => c.Model)
                .NotNull()
                .SetValidator(carModelValidator);

            this.Property(c => c.Mileage)
                .GreaterThanOrEqualTo(0);

            // A price is checked in every validation, and a listing check asks for it too: the chain stays in
            // the default group and joins the Listing group besides.
            this.Property(c => c.PurchasePrice)
                .GreaterThan(0m)
                .WithGroup(ValidationGroups.DefaultGroup, ListingGroup);

            // Appraised when the car is taken in: a trade-in cannot be worth more than the car is bought
            // for. A later correction carries prices settled after the fact, so this chain runs only where
            // the Create group is selected.
            this.Property(c => c.TradeInValue)
                .LessThanOrEqualTo(c => c.PurchasePrice)
                .WithGroup(CreateGroup);

            this.Property(c => c.FirstRegistration)
                .WithDisplayName("Registration date")
                .NotDefault();

            this.Property(c => c.SoldDate)
                .GreaterThanOrEqualTo(c => c.FirstRegistration);

            this.Property(c => c.FeatureIds)
                .NoDuplicates();

            // The history is capped, checked against the car it belongs to, and then each entry is
            // judged by the service record's own validator. A car without a history has nothing to
            // answer for, which is why every rule here passes a missing collection.
            this.Property(c => c.ServiceHistory)
                .MaximumCount(MaximumServiceRecords)
                .Must((c, history) => history == null || history.All(record => record.Mileage <= c.Mileage))
                .WithMessage("A service cannot be recorded at a higher mileage than the car has reached.")
                .ForEach(serviceRecordValidator);

            // What a car needs before it is offered for sale, and nothing a plain validation asks about.
            this.Group(ListingGroup, () =>
            {
                this.Property(c => c.RegistrationPlate)
                    .NotEmpty();

                // How far a listed car may have been driven, and whether its history has to be on record,
                // is up to the market it is offered in, which only the caller knows: it hands a
                // ListingPolicy over, and a check without one leaves both chains alone.
                this.Property(c => c.Mileage)
                    .Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage)
                    .WithMessage("The mileage is above what this market lists.");

                this.Property(c => c.ServiceHistory)
                    .NotEmpty()
                    .When<ListingPolicy>((_, policy) => policy.RequiresServiceHistory);

                // A used car is listed with the condition it arrived in and the interval it is serviced at.
                // Asked about the car itself, so it is a condition rather than a group.
                this.When(c => c.Condition == CarCondition.Used, () =>
                {
                    this.Property(c => c.IntakeCondition)
                        .NotNull();

                    this.Property(c => c.ServiceIntervalKm)
                        .NotNull();
                });
            });
        }
    }
}
