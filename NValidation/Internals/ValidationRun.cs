namespace NValidation.Internals
{
    internal readonly record struct ValidationRun(
        InheritedSettings Inherited,
        ElementScope? Scope,
        CancellationToken CancellationToken);
}
