namespace NValidation.Tests.TestData
{
    /// <summary>
    /// A payload whose reference properties are all declared non-nullable, which is the whole point of it:
    /// a chain no longer built for the nullable form of the property's type would not compile against
    /// these, and <c>TreatWarningsAsErrors</c> turns that into a red build. Do not relax them to their
    /// nullable forms — that is the coverage. The three types are not arbitrary either: each binds a rule
    /// through a different generic shape.
    /// </summary>
    public sealed class NonNullablePayload
    {
        public string Reference { get; set; } = string.Empty;

        public Manufacturer Manufacturer { get; set; } = new();

        public List<string> Tags { get; set; } = [];
    }
}
