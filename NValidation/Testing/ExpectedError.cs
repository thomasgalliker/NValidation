namespace NValidation.Testing
{
    /// <summary>
    /// One expected validation failure, as passed to
    /// <see cref="ValidationAssertions.ShouldReport(ValidationResult, IEnumerable{ExpectedError})"/>.
    /// </summary>
    /// <param name="Code">The property path the failure is reported under. Matched exactly.</param>
    /// <param name="Message">
    /// The expected message. <c>*</c> stands for any run of characters and <c>?</c> for exactly one, so
    /// <c>"*greater than*"</c> pins a fragment and the default <c>"*"</c> accepts any message — for a
    /// test about which properties report rather than about the wording. Write <c>\*</c> or <c>\?</c> for
    /// those characters themselves.
    /// </param>
    public sealed record ExpectedError(string Code, string Message = "*");
}
