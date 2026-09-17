namespace NValidation.Internals
{
    /// <summary>
    /// A validator which can be handed the <see cref="ValidationRun"/> it is taking part in, so what the
    /// call was given — its options, its token, the collection entry it is inside — reaches a validator
    /// composed through <c>SetValidator</c> or <c>ForEach</c>.
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="Validator{T}"/>. A validator written by hand against
    /// <see cref="IValidator{T}"/> is called through that interface instead, which is why this is a
    /// capability to test for rather than a requirement.
    /// </remarks>
    internal interface IValidationRunAware<in T>
    {
        /// <summary>
        /// Whether every rule judges rather than awaits, so the validator can be run through the
        /// synchronous members below without an async state machine anywhere in it.
        /// </summary>
        bool IsSynchronous { get; }

        ValueTask<ValidationResult> ValidateAsync(T instance, ValidationRun run);

        /// <summary>
        /// The same, reporting into a list the caller owns rather than into a result of its own.
        /// </summary>
        ValueTask ValidateIntoAsync(T instance, List<ValidationError> errors, ValidationRun run);

        /// <summary>
        /// The synchronous form, only for a validator whose <see cref="IsSynchronous"/> is <c>true</c>.
        /// </summary>
        ValidationResult Validate(T instance, ValidationRun run);

        /// <inheritdoc cref="Validate"/>
        void ValidateInto(T instance, List<ValidationError> errors, ValidationRun run);
    }
}
