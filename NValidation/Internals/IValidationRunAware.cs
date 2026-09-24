namespace NValidation.Internals
{
    internal interface IValidationRunAware<in T>
    {
        /// <summary>
        /// Whether every rule judges rather than awaits, so the validator can be run through the synchronous
        /// members below without an async state machine anywhere in it.
        /// </summary>
        bool IsSynchronous { get; }

        /// <summary>
        /// The groups this validator's chains are in or reach, through which a chain composing it can be
        /// selected; empty where it declares none. Reading it freezes the validator.
        /// </summary>
        string[] DeclaredGroups { get; }

        ValueTask<ValidationResult> ValidateAsync(T instance, ValidationRun run);

        /// <summary>
        /// The same, reporting into a list the caller owns rather than into a result of its own.
        /// </summary>
        ValueTask ValidateIntoAsync(T instance, List<ValidationError> errors, ValidationRun run);

        /// <summary>
        /// The synchronous form, only for a validator whose <see cref="IsSynchronous"/> is true.
        /// </summary>
        ValidationResult Validate(T instance, ValidationRun run);

        void ValidateInto(T instance, List<ValidationError> errors, ValidationRun run);
    }
}
