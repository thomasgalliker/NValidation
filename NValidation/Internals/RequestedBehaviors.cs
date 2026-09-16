namespace NValidation.Internals
{
    /// <summary>
    /// The validation behaviors asked for when this validation was started — by the options passed to
    /// <c>ValidateAsync</c> — carried down the call so a validator composed into another, or the element
    /// chain of a <c>ForEach</c>, inherits them where it declared nothing of its own.
    /// </summary>
    /// <remarks>
    /// Both axes are <c>null</c> when the caller passed no options, which is why adding this changed no
    /// existing behaviour: every level then resolves exactly as it did before.
    /// <para>
    /// It sits <em>below</em> what a validator declared for itself, so one which said something keeps
    /// it and only one which said nothing inherits. That is the difference between handing down a
    /// default and overruling a decision, and it is why composing a validator still does not truncate
    /// what the composed one found.
    /// </para>
    /// <para>
    /// A struct passed beside the message provider rather than folded into one object with it, because
    /// a <c>ForEach</c> builds a message provider per entry and would otherwise have to build one of
    /// these per entry too.
    /// </para>
    /// </remarks>
    internal readonly record struct RequestedBehaviors(ValidationBehavior? Class, ValidationBehavior? Property);
}
