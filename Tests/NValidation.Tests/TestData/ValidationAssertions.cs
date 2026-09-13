namespace NValidation.Tests.TestData
{
    /// <summary>
    /// Asserts what a validator reported — the properties it blamed and the messages it chose, not
    /// merely that it failed.
    /// </summary>
    internal static class ValidationAssertions
    {
        /// <summary>
        /// Validates with messages resolved to their keys.
        /// </summary>
        /// <remarks>
        /// Rebinds the validator's provider for good rather than for the one call, so a test which wants
        /// both the keyed wording and the built-in English one has to take the English run first.
        /// </remarks>
        public static ValueTask<ValidationResult> ValidateForKeysAsync<T>(this Validator<T> validator, T instance)
        {
            validator.Messages = MessageKeyProvider.Instance;

            return validator.ValidateAsync(instance);
        }

        /// <summary>
        /// Asserts that the only failure is <paramref name="message"/>, reported under
        /// <paramref name="code"/>. The message is matched with wildcards.
        /// </summary>
        public static void ShouldReport(this ValidationResult result, string code, string message)
        {
            result.ShouldReport([new ExpectedError(code, message)]);
        }

        /// <summary>
        /// Asserts that the result reports exactly <paramref name="expected"/> — no more and no fewer,
        /// in any order, so a repeated entry asks for a repeated failure. Each code is matched exactly
        /// and each message with wildcards.
        /// </summary>
        public static void ShouldReport(this ValidationResult result, IEnumerable<ExpectedError> expected)
        {
            result.Errors.Should().BeEquivalentTo(expected, options => options
                .Using<string>(context => context.Subject.Should().Match(context.Expectation))
                .When(info => info.Path.EndsWith("Message", StringComparison.Ordinal)));
        }
    }
}
