namespace NValidation.Internals
{
    /// <summary>
    /// Whether values of <typeparamref name="TValue"/> can be put in order at all.
    /// </summary>
    /// <remarks>
    /// The ordering rules compare through <see cref="Comparer{T}.Default"/>, which for a type that
    /// implements no comparison falls back to a comparer that throws <see cref="ArgumentException"/>
    /// on every call — that is, on every request, turning a bad payload into a server error. Asking
    /// this once at declaration time instead turns it into a mistake the validator's constructor
    /// reports, which is where the rule was written.
    /// <para>
    /// An enum qualifies even though <see cref="System.Enum"/> implements no
    /// <c>IComparable&lt;TEnum&gt;</c>: the runtime resolves <see cref="Comparer{T}.Default"/> to a
    /// specialised enum comparer, so enum comparison is both correct and allocation-free. The
    /// non-generic <see cref="IComparable"/> is deliberately <em>not</em> accepted on its own — a type
    /// may implement it against something other than itself, which would pass this check and still
    /// throw on every comparison.
    /// </para>
    /// </remarks>
    internal static class Orderable<TValue>
        where TValue : struct
    {
        /// <summary>
        /// Worked out once per <typeparamref name="TValue"/> by the static constructor, not per rule
        /// and not per comparison.
        /// </summary>
        public static readonly bool IsSupported =
            typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) || typeof(TValue).IsEnum;
    }
}
