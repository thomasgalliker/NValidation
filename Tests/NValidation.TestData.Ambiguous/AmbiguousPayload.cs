namespace NValidation.TestData.Ambiguous
{
    /// <summary>
    /// A payload with two validators, which is the one thing an assembly scan cannot decide for itself.
    /// </summary>
    public sealed class AmbiguousPayload
    {
        public string? Name { get; set; }
    }
}
