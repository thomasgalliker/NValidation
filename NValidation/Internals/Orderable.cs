namespace NValidation.Internals
{
    /// <summary>
    /// Whether values of <typeparamref name="TValue"/> can be put in order. Asked once when the rule is
    /// declared, because <c>Comparer</c> throws on every comparison for a type that implements none.
    /// </summary>
    internal static class Orderable<TValue>
        where TValue : struct
    {
        public static readonly bool IsSupported =
            typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) || typeof(TValue).IsEnum;
    }
}
