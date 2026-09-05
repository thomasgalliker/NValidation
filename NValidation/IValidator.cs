namespace NValidation
{
    /// <summary>
    /// Validates an instance whose type is only known at runtime. Implemented for every
    /// <see cref="IValidator{T}"/>, so a caller holding a validator it resolved by <see cref="Type"/> —
    /// a request pipeline dispatching on a parameter's declared type, say — can invoke it without
    /// reflecting over the closed generic.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="IValidator{T}"/> wherever the type is known at compile time: it is the typed
    /// contract, and it cannot be handed an instance of the wrong type.
    /// </remarks>
    public interface IValidator
    {
        /// <summary>
        /// Validates <paramref name="instance"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidCastException">
        /// <paramref name="instance"/> is not of the type this validator validates.
        /// </exception>
        ValueTask<ValidationResult> ValidateAsync(object instance, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Validates instances of <typeparamref name="T"/>. Returns a <see cref="ValidationResult"/> (success
    /// or failures) and never throws for validation failures, so validators stay pure and unit-testable.
    /// Callers decide how to react to a failed result — by inspecting
    /// <see cref="ValidationResult.Errors"/>, or by calling <see cref="ValidationResult.ThrowIfInvalid"/>
    /// to turn it into a <see cref="ValidationException"/>.
    /// </summary>
    public interface IValidator<in T> : IValidator
    {
        /// <summary>
        /// Validates <paramref name="instance"/>. A null <paramref name="instance"/> throws
        /// <see cref="ArgumentNullException"/> instead of returning a failure.
        /// </summary>
        ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        ValueTask<ValidationResult> IValidator.ValidateAsync(object instance, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instance);

            // Through the typed interface rather than through this, so the two overloads cannot resolve
            // to each other when T is object.
            return ((IValidator<T>)this).ValidateAsync((T)instance, cancellationToken);
        }
    }
}
