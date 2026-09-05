namespace NValidation.TestData
{
    /// <summary>
    /// The root of the test domain. Reached from <see cref="CarModel"/>, so a rule chain declared on
    /// <see cref="Car"/> can prove that nested error codes are prefixed the whole way down
    /// (<c>Model.Manufacturer.Name</c>).
    /// </summary>
    public class Manufacturer
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        /// <summary>
        /// ISO 3166-1 alpha-3, i.e. a value of an exact length rather than a maximum one.
        /// </summary>
        public string? CountryCode { get; set; }

        public DateTime? FoundedDate { get; set; }

        public string? ContactEmail { get; set; }

        /// <summary>
        /// Free-form enough that only a pattern describes it, which is what <c>Matches</c> is for.
        /// </summary>
        public string? Website { get; set; }
    }
}
