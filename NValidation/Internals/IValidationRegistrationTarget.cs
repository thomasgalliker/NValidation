namespace NValidation.Internals
{
    /// <summary>
    /// A validator the registration can hand its configured options to. <see cref="Validator{T}"/> keeps
    /// them apart from what its own constructor declared, so the validator's word still wins.
    /// </summary>
    internal interface IValidationRegistrationTarget
    {
        NValidationOptions Options { set; }
    }
}
