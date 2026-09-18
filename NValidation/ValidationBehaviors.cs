namespace NValidation
{
    /// <summary>
    /// How much a validator reports, on two axes: whether a run goes on to the next property once one
    /// has reported (<see cref="Class"/>), and whether a property's chain goes on to its next rule once
    /// one has failed (<see cref="Property"/>). An axis left <c>null</c> inherits from the level below.
    /// </summary>
    /// <remarks>
    /// Carried by <see cref="Validator{T}.ValidationBehaviors"/>, <see cref="NValidationOptions.ValidationBehaviors"/>
    /// and the registration, so the level a setting applies to is where it is written. A value with
    /// init-only members: naming one axis, <c>new() { Class = ... }</c>, leaves the other inheriting.
    /// </remarks>
    public readonly record struct ValidationBehaviors
    {
        /// <summary>
        /// Whether a run keeps going once a property has reported. Defaults to
        /// <see cref="ValidationBehavior.All"/>, so a caller is told about every field that is wrong.
        /// </summary>
        /// <remarks>
        /// <see cref="ValidationBehavior.StopAtFirstError"/> stops the run as soon as anything has been
        /// reported, and therefore also stops the chain that produced it, overriding <see cref="Property"/>;
        /// a chain which says otherwise through <c>WithValidationBehavior</c> is the one exception. A run
        /// is stopped between rules, so a composed validator or a <c>ForEach</c> which reported several
        /// failures at once is not cut short.
        /// </remarks>
        public ValidationBehavior? Class { get; init; }

        /// <summary>
        /// Whether a property's chain keeps going once one of its rules has failed. Defaults to
        /// <see cref="ValidationBehavior.StopAtFirstError"/>, so a property reports at most one message:
        /// the rules of a chain usually run coarse to fine, and only the first failure tells the caller
        /// anything.
        /// </summary>
        public ValidationBehavior? Property { get; init; }
    }
}
