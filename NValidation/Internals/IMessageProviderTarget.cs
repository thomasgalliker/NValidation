namespace NValidation.Internals
{
    /// <summary>
    /// A validator the registration can hand the configured message provider to.
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="Validator{T}"/>. It exists so the registration does not have to reflect
    /// over a property, and so a validator which implements <see cref="IValidator{T}"/> by hand — and
    /// therefore brings its own messages — is left alone.
    /// </remarks>
    internal interface IMessageProviderTarget
    {
        IValidationMessageProvider Messages { set; }
    }
}
