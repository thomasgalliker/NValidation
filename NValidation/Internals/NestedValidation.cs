namespace NValidation.Internals
{
    internal static class NestedValidation
    {
        public static ValueTask<ValidationResult> ValidateAsync<T>(IValidator<T> validator, T instance, ValidationRun run)
        {
            if (validator is IValidationRunAware<T> aware)
            {
                return aware.ValidateAsync(instance, run);
            }

            return validator.ValidateAsync(instance, run.Inherited.AsOptions(), run.CancellationToken);
        }

        /// <summary>
        /// The same, reporting into a list the caller owns. A validator written by hand can only answer with
        /// a result, so its errors are copied across.
        /// </summary>
        public static async ValueTask ValidateIntoAsync<T>(
            IValidator<T> validator, T instance, List<ValidationError> errors, ValidationRun run)
        {
            if (validator is IValidationRunAware<T> aware)
            {
                await aware.ValidateIntoAsync(instance, errors, run);
                return;
            }

            var result = await ValidateAsync(validator, instance, run);

            errors.AddRange(result.Errors);
        }
    }
}
