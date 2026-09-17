namespace NValidation
{
    /// <summary>
    /// The outcome of validating an object: a successful result carries no errors; a failed result
    /// carries one or more <see cref="ValidationError"/>. Validators return this (they never throw for
    /// validation failures); a caller that would rather handle the failure as an exception calls
    /// <see cref="ThrowIfInvalid"/>.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// A shared, successful result (no errors).
        /// </summary>
        public static ValidationResult Success { get; } = new ValidationResult([]);

        private ValidationResult(IReadOnlyList<ValidationError> errors)
        {
            this.Errors = errors;
        }

        /// <summary>
        /// <c>true</c> when there are no validation errors.
        /// </summary>
        public bool Succeeded => this.Errors.Count == 0;

        /// <summary>
        /// The validation errors; empty when <see cref="Succeeded"/> is <c>true</c>.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; }

        /// <summary>
        /// Creates a failed result from the given errors, or <see cref="Success"/> when none are supplied.
        /// </summary>
        public static ValidationResult FromValidationErrors(params ValidationError[] errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            return errors.Length == 0 ? Success : new ValidationResult(errors.ToArray());
        }

        /// <summary>
        /// Creates a failed result from the given errors, or <see cref="Success"/> when none are supplied.
        /// </summary>
        public static ValidationResult FromValidationErrors(IEnumerable<ValidationError> validationErrors)
        {
            ArgumentNullException.ThrowIfNull(validationErrors);

            var errors = validationErrors.ToArray();
            return errors.Length == 0 ? Success : new ValidationResult(errors);
        }

        /// <summary>
        /// A failed result over a list the caller is finished with: it is taken, not copied.
        /// </summary>
        internal static ValidationResult FromValidationErrorsInternal(List<ValidationError> errors)
        {
            return errors.Count == 0 ? Success : new ValidationResult(errors);
        }

        /// <summary>
        /// Throws a <see cref="ValidationException"/> carrying the field-grouped errors when this result
        /// is not successful; otherwise returns. This is the bridge from a pure validation result to a
        /// host's exception-handling pipeline.
        /// </summary>
        public void ThrowIfInvalid()
        {
            if (!this.Succeeded)
            {
                throw new ValidationException(this);
            }
        }

        /// <summary>
        /// Groups <see cref="Errors"/> by <see cref="ValidationError.PropertyName"/> into a
        /// <c>{ propertyName: [messages] }</c> shape, so each property carries every message it collected. Shared
        /// by <see cref="ValidationException"/> and by callers that report a validation failure directly
        /// instead of throwing.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> ToErrorsDictionary()
        {
            return this.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Message).ToArray(),
                    StringComparer.Ordinal);
        }
    }
}
