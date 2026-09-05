namespace NValidation.Internals
{
    /// <summary>
    /// Hands a rule the position of the element it is judging, so a message can name the row it is
    /// about.
    /// </summary>
    /// <remarks>
    /// Wraps the host's provider for the duration of one element rather than assigning it, because a
    /// validator is shared and may be running against more than one object at a time.
    /// </remarks>
    internal sealed class IndexedMessageProvider : IValidationMessageProvider
    {
        private readonly IValidationMessageProvider inner;

        private readonly int index;

        public IndexedMessageProvider(IValidationMessageProvider inner, int index)
        {
            this.inner = inner;
            this.index = index;
        }

        public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
        {
            var withIndex = new Dictionary<string, object?>(arguments.Count + 1, StringComparer.Ordinal);

            foreach (var argument in arguments)
            {
                withIndex[argument.Key] = argument.Value;
            }

            withIndex[ValidationMessagePlaceholders.CollectionIndex] = this.index;

            return this.inner.GetMessage(messageKey, withIndex);
        }
    }
}
