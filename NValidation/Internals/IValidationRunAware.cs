namespace NValidation.Internals
{
    /// <summary>
    /// A validator which can be handed the state of the run it is taking part in — the message
    /// provider, and the behaviors the run was asked for — rather than resolving both through what it
    /// carries itself.
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="Validator{T}"/>. It is what lets a validator composed into another —
    /// merged by <c>SetValidator</c>, or run per entry by <c>ForEach</c> — report in the same language,
    /// and with the same placeholders, as the validator that composed it. Without it a nested
    /// validator answers from its own provider, so a per-call provider never reaches it and
    /// <see cref="ValidationMessagePlaceholders.CollectionIndex"/> has nothing to substitute.
    /// <para>
    /// The behaviors are the same <see cref="ValidationBehaviors"/> every other level carries, and
    /// <c>null</c> where the caller asked for nothing — so a composed validator resolves exactly as it
    /// did before anything was passed. They belong to options which using froze, so nothing can change
    /// them while the run reads them.
    /// </para>
    /// <para>
    /// A validator written by hand against <see cref="IValidator{T}"/> brings its own messages and is
    /// left alone, which is why this is a capability to test for rather than a requirement.
    /// </para>
    /// </remarks>
    internal interface IValidationRunAware<in T>
    {
        ValueTask<ValidationResult> ValidateAsync(T instance, IValidationMessageProvider messages, ValidationBehaviors? requested, CancellationToken cancellationToken);

        /// <summary>
        /// The same, reporting into a list the caller owns rather than into a result of its own.
        /// </summary>
        ValueTask ValidateIntoAsync(T instance, List<ValidationError> errors, IValidationMessageProvider messages, ValidationBehaviors? requested, CancellationToken cancellationToken);
    }
}
