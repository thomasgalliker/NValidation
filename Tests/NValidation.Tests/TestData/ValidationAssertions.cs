namespace NValidation.Tests.TestData
{
    /// <summary>
    /// Runs a validator against the <see cref="MessageKeyProvider"/> and asserts what it reported —
    /// the property it blamed and the message it chose, not merely that it failed.
    /// </summary>
    internal static class ValidationAssertions
    {
        /// <summary>
        /// Validates with messages resolved to their keys.
        /// </summary>
        public static ValueTask<ValidationResult> ValidateForKeysAsync<T>(this Validator<T> validator, T instance)
        {
            validator.Messages = MessageKeyProvider.Instance;

            return validator.ValidateAsync(instance);
        }

        /// <summary>
        /// Asserts that the only failure is <paramref name="messageKey"/>, reported under
        /// <paramref name="code"/>.
        /// </summary>
        public static void ShouldReport(this ValidationResult result, string code, string messageKey)
        {
            result.Errors.Should().ContainSingle(
                $"the rule should report exactly one failure, under {code}, as {messageKey}");

            var error = result.Errors[0];

            error.Code.Should().Be(code, "the failure names the property a caller binds to");
            error.Message.Should().Be(messageKey, "the rule reports its own message, not another rule's");
        }
    }
}
