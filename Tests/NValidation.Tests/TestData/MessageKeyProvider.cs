namespace NValidation.Tests.TestData
{
    /// <summary>
    /// Reports every message as the key that asked for it, so a test can assert <em>which</em> message a
    /// rule reported without depending on any wording.
    /// </summary>
    /// <remarks>
    /// The rule tests otherwise only see the built-in English, where several rules share a sentence and
    /// none of them names its key. A rule wired to the wrong key still reads plausibly in English, which
    /// is exactly the mistake this makes visible.
    /// </remarks>
    internal sealed class MessageKeyProvider : IValidationMessageProvider
    {
        public static MessageKeyProvider Instance { get; } = new MessageKeyProvider();

        public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
        {
            return messageKey;
        }
    }
}
