namespace NValidation.Internals
{
    /// <summary>
    /// Which way round a comparison rule reads. One enum, so the four comparisons share a single
    /// implementation and cannot drift apart.
    /// </summary>
    internal enum ComparisonKind
    {
        GreaterThan,
        GreaterThanOrEqualTo,
        LessThan,
        LessThanOrEqualTo,
    }
}
