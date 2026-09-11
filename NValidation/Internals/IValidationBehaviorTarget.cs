namespace NValidation.Internals
{
    /// <summary>
    /// A validator the registration can hand the configured <see cref="ValidationBehaviors"/> to.
    /// </summary>
    /// <remarks>
    /// The sibling of <see cref="IMessageProviderTarget"/>, and there for the same reasons: the
    /// registration does not have to reflect over a property, and a validator which implements
    /// <see cref="IValidator{T}"/> by hand is left alone.
    /// <para>
    /// What is handed over is kept apart from what the validator's own constructor declared, so the
    /// two can be told apart while validating and the validator's word wins whichever ran first.
    /// </para>
    /// </remarks>
    internal interface IValidationBehaviorTarget
    {
        ValidationBehaviors AmbientValidationBehaviors { set; }
    }
}
