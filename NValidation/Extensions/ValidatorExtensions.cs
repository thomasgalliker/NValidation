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
        /// The same as <c>(await validator.ValidateAsync(instance, cancellationToken)).ThrowIfInvalid()</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="validator"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationException"><paramref name="instance"/> is not valid.</exception>
        public static async ValueTask ValidateAndThrowAsync<T>(
            this IValidator<T> validator,
            T instance,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(validator);

            var result = await validator.ValidateAsync(instance, cancellationToken);

            result.ThrowIfInvalid();
        }

        /// <summary>
        /// Validates <paramref name="instance"/> against options supplied for this call — the rule groups
        /// an endpoint runs, say — and throws a <see cref="ValidationException"/> when it fails.
        /// </summary>
        /// <remarks>
        /// The same as <c>(await validator.ValidateAsync(instance, options, cancellationToken)).ThrowIfInvalid()</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="validator"/> or <paramref name="options"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ValidationException"><paramref name="instance"/> is not valid.</exception>
        public static async ValueTask ValidateAndThrowAsync<T>(
            this IValidator<T> validator,
            T instance,
            NValidationOptions options,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(options);

            var result = await validator.ValidateAsync(instance, options, cancellationToken);

            result.ThrowIfInvalid();
        }
    }
}
