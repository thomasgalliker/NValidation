namespace NValidation.TestData
{
    /// <summary>
    /// One entry of a car's service history. A collection of these is what gives the element rules a
    /// nested type to validate, rather than only a collection of scalars.
    /// </summary>
    public sealed class ServiceRecord
    {
        public string? Workshop { get; set; }

        public int Mileage { get; set; }

        public decimal Cost { get; set; }
    }
}
