namespace NValidation.TestData
{
    public class CarModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public Manufacturer? Manufacturer { get; set; }

        public EngineType EngineType { get; set; }

        public int SeatCount { get; set; }

        /// <summary>
        /// Optional in the contract, so the nullable-value-type overloads of the rules have a subject.
        /// </summary>
        public decimal? BasePrice { get; set; }

        /// <summary>
        /// Litres per 100 km. Carries <see cref="double.NaN"/> when the figure was never measured, which
        /// is what makes it the subject for <c>NotNaN</c>.
        /// </summary>
        public double FuelConsumption { get; set; }

        /// <summary>
        /// km/h, and optional — the single-precision counterpart of <see cref="FuelConsumption"/>.
        /// </summary>
        public float? TopSpeed { get; set; }

        /// <summary>
        /// Units built over the model's lifetime: large enough that it is a <see cref="long"/>, which is
        /// what proves the comparison rules are not written per numeric type.
        /// </summary>
        public long UnitsProduced { get; set; }

        /// <summary>
        /// Only meaningful for <see cref="EngineType.Electric"/>, which is what <c>When</c>/<c>Unless</c>
        /// are demonstrated on.
        /// </summary>
        public decimal? BatteryCapacityKwh { get; set; }
    }
}
