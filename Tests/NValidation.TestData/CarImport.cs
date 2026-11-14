namespace NValidation.TestData
{
    /// <summary>
    /// Deliberately has no validator, so it is the subject for a host deciding what an unvalidated
    /// payload means.
    /// </summary>
    public sealed class CarImport
    {
        public string? SourceSystem { get; set; }

        public string? Payload { get; set; }
    }
}
