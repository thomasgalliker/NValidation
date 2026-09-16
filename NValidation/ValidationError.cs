namespace NValidation
{
    /// <summary>
    /// A single validation failure: where it is (<see cref="PropertyName"/>), which rule produced it
    /// (<see cref="ErrorCode"/>) and what to show a reader (<see cref="Message"/>).
    /// </summary>
    public sealed class ValidationError
    {
        /// <summary>
        /// Creates a failure for the property named by <paramref name="propertyName"/>, identifying no
        /// particular rule.
        /// </summary>
        public ValidationError(string propertyName, string message)
            : this(propertyName, message, errorCode: null, arguments: null)
        {
        }

        /// <summary>
        /// The same, naming the rule that produced it.
        /// </summary>
        public ValidationError(string propertyName, string message, string? errorCode)
            : this(propertyName, message, errorCode, arguments: null)
        {
        }

        /// <summary>
        /// The same, carrying the arguments the message was rendered from.
        /// </summary>
        public ValidationError(
            string propertyName,
            string message,
            string? errorCode,
            IReadOnlyDictionary<string, object?>? arguments)
        {
            ArgumentNullException.ThrowIfNull(propertyName);
            ArgumentNullException.ThrowIfNull(message);

            this.PropertyName = propertyName;
            this.Message = message;
            this.ErrorCode = errorCode;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Where the failure is: the C# property path of the validated object — <c>"Vin"</c>,
        /// <c>"Model.Manufacturer.Name"</c>, <c>"ServiceHistory[1].Workshop"</c> — so a caller can bind
        /// the message to the input it belongs to. This is what a failure is reported under.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The human-readable error message, already resolved.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Which rule produced the failure — <c>"NotEmpty"</c>, <c>"GreaterThan"</c>, or whatever
        /// <c>WithErrorCode</c> named — so a client can tell two failures of one property apart without
        /// reading the message. It is also the key the message was resolved under.
        /// </summary>
        /// <remarks>
        /// <c>null</c> for a failure built by hand through the two-argument constructor, which named no
        /// rule. Every rule this core ships reports one.
        /// </remarks>
        public string? ErrorCode { get; }

        /// <summary>
        /// What the message was rendered from — <c>{PropertyName}</c>, <c>{MinLength}</c>,
        /// <c>{CollectionIndex}</c> and whatever else the rule supplied — for a caller which logs the
        /// failure in parts rather than as a sentence.
        /// </summary>
        /// <remarks>
        /// <c>null</c> where the message did not come from a template.
        /// </remarks>
        public IReadOnlyDictionary<string, object?>? Arguments { get; }
    }
}
