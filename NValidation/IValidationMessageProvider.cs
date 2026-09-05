namespace NValidation
{
    /// <summary>
    /// Resolves the message of a failed rule. This is the seam that keeps the validation core free of
    /// the application's resources: the core knows only the keys in <see cref="ValidationMessageKeys"/>,
    /// while the host decides where the texts come from and in which language.
    /// </summary>
    public interface IValidationMessageProvider
    {
        /// <summary>
        /// Returns the message for <paramref name="messageKey"/>, with the named placeholders of
        /// <paramref name="arguments"/> substituted. A rule always supplies every argument it has —
        /// including <see cref="ValidationMessagePlaceholders.PropertyName"/> — and the message decides
        /// which of them it mentions, so a translation is free to leave any of them out.
        /// </summary>
        string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments);
    }
}
