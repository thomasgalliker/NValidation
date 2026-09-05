namespace NValidation.Internals
{
    /// <summary>
    /// A validator which can be handed the message provider of the run it is taking part in, rather
    /// than resolving messages through the one it carries itself.
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="Validator{T}"/>. It is what lets a validator composed into another —
    /// merged by <c>SetValidator</c>, or run per entry by <c>ForEach</c> — report in the same language,
    /// and with the same placeholders, as the validator that composed it. Without it a nested
    /// validator answers from its own provider, so a per-call provider never reaches it and
    /// <see cref="ValidationMessagePlaceholders.CollectionIndex"/> has nothing to substitute.
    /// <para>
    /// A validator written by hand against <see cref="IValidator{T}"/> brings its own messages and is
    /// left alone, which is why this is a capability to test for rather than a requirement.
    /// </para>
    /// </remarks>
    internal interface IMessageProviderAware<in T>
    {
        ValueTask<ValidationResult> ValidateAsync(
            T instance,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken);

        /// <summary>
        /// The same, reporting into a list the caller owns rather than into a result of its own.
        /// </summary>
        ValueTask ValidateIntoAsync(
            T instance,
            List<ValidationError> errors,
            IValidationMessageProvider messages,
            CancellationToken cancellationToken);
    }
}
