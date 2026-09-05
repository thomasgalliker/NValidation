namespace NValidation.TestData
{
    /// <summary>
    /// Valid instances of the test domain. A test starts from one of these and breaks exactly the one
    /// property it is about, so a failure names the rule under test rather than an unrelated one.
    /// </summary>
    public static class Cars
    {
        public static Manufacturer Manufacturer()
        {
            return new Manufacturer
            {
                Id = 1,
                Name = "Aurora Motors",
                CountryCode = "CHE",
                FoundedDate = new DateTime(1962, 4, 17, 0, 0, 0, DateTimeKind.Utc),
                ContactEmail = "info@aurora-motors.example",
                Website = "https://aurora-motors.example",
            };
        }

        public static CarModel CarModel()
        {
            return new CarModel
            {
                Id = 10,
                Name = "Aurora Comet",
                Manufacturer = Manufacturer(),
                EngineType = EngineType.Petrol,
                SeatCount = 5,
                BasePrice = 34_900m,
                FuelConsumption = 6.4d,
                TopSpeed = 213.5f,
                UnitsProduced = 4_120_000L,
            };
        }

        public static CarModel ElectricCarModel()
        {
            var carModel = CarModel();
            carModel.Name = "Aurora Comet E";
            carModel.EngineType = EngineType.Electric;
            carModel.FuelConsumption = 0d;
            carModel.BatteryCapacityKwh = 58m;

            return carModel;
        }

        public static Car Car()
        {
            return new Car
            {
                Vin = "WAUZZZ8V5KA123456",
                Model = CarModel(),
                Condition = CarCondition.Used,
                Equipment = CarEquipment.AirConditioning | CarEquipment.Navigation,
                Mileage = 42_000,
                PurchasePrice = 18_500m,
                TradeInValue = 15_000m,
                IsListedForSale = true,
                FirstRegistration = new DateTime(2019, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                SoldDate = new DateTime(2023, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                RegisteredAt = new DateTimeOffset(2019, 6, 1, 9, 0, 0, TimeSpan.FromHours(2)),
                NextServiceAt = new DateTimeOffset(2027, 3, 1, 9, 0, 0, TimeSpan.FromHours(1)),
                ServiceInterval = TimeSpan.FromDays(365),
                WarrantyEndsOn = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                WarrantyMileageLimit = 100_000,
                FeatureIds = [1, 2, 3],
                PreviousOwnerIds = [Guid.Parse("11111111-1111-1111-1111-111111111111")],
            };
        }
    }
}
