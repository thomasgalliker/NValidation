namespace NValidation
{
    /// <summary>
    /// A single validation failure: a <see cref="Code"/> that identifies what failed and a
    /// human-readable <see cref="Message"/>.
    /// </summary>
    public sealed class ValidationError
    {
        /// <summary>
        /// Creates a failure for the property named by <paramref name="code"/>.
        /// </summary>
        public ValidationError(string code, string message)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentNullException.ThrowIfNull(message);

            this.Code = code;
            this.Message = message;
        }

        /// <summary>
        /// Identifies the failed rule. This is the C# property path of the validated object
        /// (e.g. <c>"Vin"</c>, <c>"Model.Manufacturer.Name"</c>) so a caller can bind the message to the
        /// corresponding property.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// The human-readable error message.
        /// </summary>
        public string Message { get; }
    }
}
