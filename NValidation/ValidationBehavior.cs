namespace NValidation
{
    /// <summary>
    /// How much a validation run reports before it gives up: everything it finds, or the first thing
    /// that is wrong. Applied per axis through <see cref="ValidationBehaviors"/>, and per rule chain
    /// through <c>WithValidationBehavior</c>.
    /// </summary>
    /// <remarks>
    /// The two axes are the same question asked at two scales, which is why one type answers both:
    /// across the properties of a validator, and across the rules of one property's chain. What the
    /// value means therefore depends on which axis it is set on — see
    /// <see cref="ValidationBehaviors.Class"/> and <see cref="ValidationBehaviors.Property"/>.
    /// </remarks>
    public enum ValidationBehavior
    {
        /// <summary>
        /// Report everything: every property is validated, and every rule of a chain is run.
        /// </summary>
        All = 0,

        /// <summary>
        /// Stop as soon as something is wrong.
        /// </summary>
        StopAtFirstError,
    }
}
