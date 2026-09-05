namespace NValidation
{
    /// <summary>
    /// Conveniences over <see cref="IValidator{T}"/> for the call shapes that would otherwise repeat.
    /// </summary>
    public static class ValidatorExtensions
    {
        /// <summary>
        /// Validates <paramref name="instance"/> and throws a <see cref="ValidationException"/> when it
        /// fails, for a caller which treats a failure as an exception rather than as a result to inspect.
        /// </summary>
        /// <remarks>
        /// The same as <c>(await validator.ValidateAsync(instance, cancellationToken)).ThrowIfInvalid()</c>,
        /// which is what most call sites would otherwise write.
        /// </remarks>
        public static async ValueTask ValidateAndThrowAsync<T>(
            this IValidator<T> validator,
            T instance,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(validator);

            var result = await validator.ValidateAsync(instance, cancellationToken);

            result.ThrowIfInvalid();
        }

    }
}
