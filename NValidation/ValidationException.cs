namespace NValidation
{
    /// <summary>
    /// Thrown when validation fails. Carries the failures grouped by <see cref="ValidationError.Code"/>, so
    /// a host can render them without re-inspecting the <see cref="ValidationResult"/> they came from.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Creates an exception carrying no per-property failures, for a caller which has only a
        /// message. <see cref="Errors"/> is then empty.
        /// </summary>
        public ValidationException(string message)
            : base(RequireMessage(message))
        {
            this.Errors = EmptyErrors;
        }

        /// <inheritdoc cref="ValidationException(string)"/>
        public ValidationException(string message, Exception innerException)
            : base(RequireMessage(message), innerException)
        {
            this.Errors = EmptyErrors;
        }

        /// <summary>
        /// Carries the failures of <paramref name="validationResult"/>, grouped by code.
        /// </summary>
        public ValidationException(ValidationResult validationResult)
            : this(RequireValidationResult(validationResult).ToErrorsDictionary())
        {
        }

        /// <summary>
        /// Carries failures a caller grouped itself.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="errors"/> is empty. A validation failure always reports at least one error,
        /// which is what lets a host treat "has errors" as the whole signal that validation failed.
        /// </exception>
        public ValidationException(IReadOnlyDictionary<string, string[]> errors)
            : base(BuildMessage(errors))
        {
            this.Errors = errors.ToDictionary(
                error => error.Key,
                error => (IReadOnlyList<string>)error.Value.ToArray(),
                StringComparer.Ordinal);
        }

        /// <summary>
        /// The validation failures, keyed by <see cref="ValidationError.Code"/>, each mapping to one or
        /// more messages. Empty when the exception was created from a message alone.
        /// </summary>
        /// <remarks>
        /// Read-only all the way down: the messages of an exception already in flight cannot be
        /// rewritten by whatever handles it.
        /// </remarks>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyErrors { get; } =
            new Dictionary<string, IReadOnlyList<string>>(0, StringComparer.Ordinal);

        private static ValidationResult RequireValidationResult(ValidationResult validationResult)
        {
            ArgumentNullException.ThrowIfNull(validationResult);

            return validationResult;
        }

        private static string RequireMessage(string message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return message;
        }

        /// <remarks>
        /// Also where <paramref name="errors"/> is checked, because this runs before the base
        /// constructor: an exception whose message was built from an empty dictionary would otherwise
        /// exist for as long as it took to throw the one complaining about it.
        /// </remarks>
        private static string BuildMessage(IReadOnlyDictionary<string, string[]> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            if (errors.Count == 0)
            {
                throw new ArgumentException("A validation failure must carry at least one error.", nameof(errors));
            }

            return string.Join(" ", errors.Values.SelectMany(messages => messages));
        }
    }
}
