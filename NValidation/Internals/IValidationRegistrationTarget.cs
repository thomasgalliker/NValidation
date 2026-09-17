namespace NValidation.Internals
{
    /// <summary>
    /// A validator the registration can hand what it configured to: the message provider and the
    /// behaviors of <c>AddNValidation</c>, as one level of the ladder.
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="Validator{T}"/>, which keeps what arrives apart from what its own
    /// constructor declared: the validator's word and the options of a call both outrank it. A validator
    /// implementing <see cref="IValidator{T}"/> by hand brings its own configuration and is left alone.
    /// </remarks>
    internal interface IValidationRegistrationTarget
    {
        NValidationOptions Options { set; }
    }
}
