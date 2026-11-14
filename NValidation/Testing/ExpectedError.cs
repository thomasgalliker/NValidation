namespace NValidation.Testing
{
    /// <summary>
    /// One expected validation failure, as passed to
    /// <see cref="ValidationAssertions.ShouldReport(ValidationResult, IEnumerable{ExpectedError})"/>.
    /// </summary>
    /// <remarks>
    /// The message is matched <b>exactly</b>, the way an assertion library's <c>Be</c> does, so an
    /// expected message means what it says. <see cref="Matching"/> and <see cref="Any"/> are the two
    /// looser forms, and each says so at the call site — which is the point: a pattern that was the
    /// default would quietly weaken every assertion that happened to contain a <c>?</c>.
    /// </remarks>
    public sealed record ExpectedError
    {
        private ExpectedError(string propertyName, string? message, ExpectedMessage match)
        {
            ArgumentNullException.ThrowIfNull(propertyName);

            this.PropertyName = propertyName;
            this.Message = message;
            this.Match = match;
        }

        /// <summary>
        /// A failure under <paramref name="propertyName"/> whose message is exactly
        /// <paramref name="message"/>, compared ordinally.
        /// </summary>
        /// <exception cref="ArgumentNullException">Either argument is <c>null</c>.</exception>
        public ExpectedError(string propertyName, string message)
            : this(propertyName, RequireMessage(message), ExpectedMessage.Exact)
        {
        }

        /// <summary>
        /// A failure under <paramref name="propertyName"/> whose message matches
        /// <paramref name="pattern"/>: <c>*</c> stands for any run of characters and <c>?</c> for
        /// exactly one, with <c>\*</c>, <c>\?</c> and <c>\\</c> for those characters themselves.
        /// </summary>
        /// <exception cref="ArgumentNullException">Either argument is <c>null</c>.</exception>
        public static ExpectedError Matching(string propertyName, string pattern)
        {
            return new ExpectedError(propertyName, RequireMessage(pattern), ExpectedMessage.Pattern);
        }

        /// <summary>
        /// A failure under <paramref name="propertyName"/> whatever its wording — for a test about
        /// which properties report rather than about the text.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <c>null</c>.</exception>
        public static ExpectedError Any(string propertyName)
        {
            return new ExpectedError(propertyName, message: null, ExpectedMessage.Any);
        }

        /// <summary>
        /// The property path the failure is reported under. Matched exactly.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The expected message, or <c>null</c> where any message is accepted.
        /// </summary>
        public string? Message { get; }

        internal ExpectedMessage Match { get; }

        private static string RequireMessage(string message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return message;
        }
    }

    internal enum ExpectedMessage
    {
        Exact,
        Pattern,
        Any,
    }
}
