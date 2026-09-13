namespace NValidation.Tests.TestData
{
    /// <summary>
    /// One expected validation failure, as passed to
    /// <see cref="ValidationAssertions.ShouldReport(ValidationResult, IEnumerable{ExpectedError})"/>.
    /// </summary>
    /// <param name="Code">The property path the failure is reported under. Matched exactly.</param>
    /// <param name="Message">
    /// The expected message, matched with wildcards: <c>"*greater than*"</c> pins a fragment, and the
    /// default <c>"*"</c> accepts any message — for a test about which properties report rather than
    /// about the wording.
    /// </param>
    internal sealed record ExpectedError(string Code, string Message = "*");
}
