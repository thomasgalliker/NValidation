namespace NValidation.Internals
{
    /// <summary>
    /// What one pass hands to the validators it composes: the settings they inherit, the collection
    /// entry currently under judgement, and the token the call was given.
    /// </summary>
    /// <param name="Inherited">
    /// At the root, what the call was given; below it, what the composer resolved — so a nested
    /// validator, or the element chain of a <c>ForEach</c>, answers like its composer unless it declared
    /// otherwise for itself.
    /// </param>
    /// <param name="Scope">The <c>ForEach</c> entry the run is inside, or <c>null</c> at the top level.</param>
    /// <param name="CancellationToken">The token every rule of the run is handed.</param>
    /// <remarks>
    /// A <c>readonly record struct</c>, so passing it allocates nothing; a pass substitutes what it
    /// resolved, and a <c>ForEach</c> its entry, with <c>with</c>.
    /// </remarks>
    internal readonly record struct ValidationRun(
        InheritedSettings Inherited,
        ElementScope? Scope,
        CancellationToken CancellationToken);
}
