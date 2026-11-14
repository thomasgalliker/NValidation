namespace NValidation.TestData
{
    /// <summary>
    /// The payload of the legacy valuation endpoint. It has a validator like everything else, but the
    /// endpoint is excluded from the automatic filter and runs that validator itself, because it answers
    /// in its own error shape.
    /// </summary>
    public sealed class CarValuation
    {
        public string? Vin { get; set; }

        public int Mileage { get; set; }
    }
}
