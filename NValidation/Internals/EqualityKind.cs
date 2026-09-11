namespace NValidation.Internals
{
    /// <summary>
    /// Which way round an equality rule reads. One enum, so the two forms share a single implementation
    /// and cannot drift apart.
    /// </summary>
    internal enum EqualityKind
    {
        EqualTo,
        NotEqualTo,
    }
}
