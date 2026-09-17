namespace NValidation.Internals
{
    internal interface IValidationRunAware<in T>
    {
        // Whether every rule judges rather than awaits, so the validator can be run through the synchronous
        // members below without an async state machine anywhere in it.
        bool IsSynchronous { get; }

        ValueTask<ValidationResult> ValidateAsync(T instance, ValidationRun run);

        // The same, reporting into a list the caller owns rather than into a result of its own.
        ValueTask ValidateIntoAsync(T instance, List<ValidationError> errors, ValidationRun run);

        // The synchronous form, only for a validator whose IsSynchronous is true.
        ValidationResult Validate(T instance, ValidationRun run);

        void ValidateInto(T instance, List<ValidationError> errors, ValidationRun run);
    }
}
