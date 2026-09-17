using NValidation.Internals;

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

        /// <summary>
        /// Validates <paramref name="instance"/> against options supplied for this call — the untyped
        /// form of <see cref="IValidator{T}.ValidateAsync(T, NValidationOptions, CancellationToken)"/>,
        /// for the pipeline which resolved its validator by <see cref="Type"/> and therefore holds this
        /// interface rather than the typed one.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="instance"/> or <paramref name="options"/> is <c>null</c>.
        /// </exception>
        /// <inheritdoc cref="ValidateAsync(object, CancellationToken)" path="/exception[@cref='T:System.InvalidCastException']"/>
        ValueTask<ValidationResult> ValidateAsync(
            object instance, NValidationOptions options, CancellationToken cancellationToken = default);
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

        /// <summary>
        /// Validates <paramref name="instance"/> against options supplied for this call rather than the
        /// ones the process was configured with — a request whose messages are in its own language, say.
        /// </summary>
        /// <remarks>
        /// The options are the layer under what a validator declared for itself, so one which named a
        /// setting keeps it and one which named nothing takes what this call asked for. They reach a
        /// nested validator and the element chain of a <c>ForEach</c> as well, which nothing configured
        /// on a registration can. A validator written by hand against this interface brings its own
        /// configuration and is left alone: the default implementation ignores
        /// <paramref name="options"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        ValueTask<ValidationResult> ValidateAsync(T instance, NValidationOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);

            return this.ValidateAsync(instance, cancellationToken);
        }

        /// <inheritdoc />
        ValueTask<ValidationResult> IValidator.ValidateAsync(object instance, CancellationToken cancellationToken)
        {
            // Through the typed interface rather than through this, so the two overloads cannot resolve
            // to each other when T is object.
            return ((IValidator<T>)this).ValidateAsync(Payload.Cast<T>(instance, this.GetType()), cancellationToken);
        }

        /// <inheritdoc />
        ValueTask<ValidationResult> IValidator.ValidateAsync(
            object instance, NValidationOptions options, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(options);

            return ((IValidator<T>)this).ValidateAsync(Payload.Cast<T>(instance, this.GetType()), options, cancellationToken);
        }
    }
}
