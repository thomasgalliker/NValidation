namespace NValidation.Internals
{
    /// <summary>
    /// Runs a validator that another validator composed, passing on the message provider of the run
    /// where the composing validator can accept one.
    /// </summary>
    internal static class NestedValidation
    {
        public static ValueTask<ValidationResult> ValidateAsync<T>(
            IValidator<T> validator,
            T instance,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken)
        {
            return validator is IMessageProviderAware<T> aware
                ? aware.ValidateAsync(instance, messages, cancellationToken)
                : validator.ValidateAsync(instance, cancellationToken);
        }

        /// <summary>
        /// The same, reporting into a list the caller owns. A validator written by hand can only answer
        /// with a result, so its errors are copied across.
        /// </summary>
        public static async ValueTask ValidateIntoAsync<T>(
            IValidator<T> validator,
            T instance,
            List<ValidationError> errors,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken)
        {
            if (validator is IMessageProviderAware<T> aware)
            {
                await aware.ValidateIntoAsync(instance, errors, messages, cancellationToken);
                return;
            }

            var result = await validator.ValidateAsync(instance, cancellationToken);

            errors.AddRange(result.Errors);
        }
    }
}
