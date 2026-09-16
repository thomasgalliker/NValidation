namespace NValidation.Testing
{
    /// <summary>
    /// Reports every message as the key that asked for it, so a test can assert <em>which</em> message a
    /// rule reported without depending on any wording. Hand it to a validator through
    /// <see cref="TestValidator{T}(IValidationMessageProvider)"/> or
    /// <see cref="Validator{T}.Messages"/>.
    /// </summary>
    /// <remarks>
    /// Against the built-in English, several rules share a sentence and none of them names its key, so a
    /// rule wired to the wrong key still reads plausibly — which is exactly the mistake this makes
    /// visible. Assert the wording itself only where the wording is the subject of the test.
    /// </remarks>
    public sealed class ErrorCodeProvider : IValidationMessageProvider
    {
        /// <summary>
        /// The shared instance. It holds no state, so one serves every test.
        /// </summary>
        public static ErrorCodeProvider Instance { get; } = new ErrorCodeProvider();

        /// <summary>
        /// Returns <paramref name="errorCode"/> unchanged, ignoring <paramref name="arguments"/>.
        /// </summary>
        public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
        {
            return errorCode;
        }
    }
}
