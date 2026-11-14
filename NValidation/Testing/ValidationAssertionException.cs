namespace NValidation.Testing
{
    /// <summary>
    /// Thrown when a validation result is not what a test expected. Carrying a plain
    /// <see cref="Exception"/> is what makes this usable from any test framework: a runner reports
    /// whatever a test throws.
    /// </summary>
    /// <remarks>
    /// Deliberately not derived from <see cref="ValidationException"/>. A test which asserts that a
    /// validator throws — <c>Throws&lt;ValidationException&gt;</c> — would otherwise swallow the failure
    /// of the assertion inside it and pass.
    /// </remarks>
    public sealed class ValidationAssertionException : Exception
    {
        /// <summary>
        /// Creates a failure carrying <paramref name="message"/>, which the test runner shows as-is.
        /// </summary>
        public ValidationAssertionException(string message)
            : base(message)
        {
        }
    }
}
