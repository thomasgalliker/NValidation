using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NValidation.Internals
{
    /// <summary>
    /// Compiles property accessors once and reuses them: validators are resolved per request, so
    /// compiling the same handful of expressions on every construction would be pure waste.
    /// </summary>
    internal static class PropertyAccessor
    {
        /// <remarks>
        /// The property type is part of the key, not just the owner and the name:
        /// <see cref="PropertyPath"/> strips conversions, so <c>x => x.Age</c> and
        /// <c>x => (object)x.Age</c> produce the same name while needing different delegates.
        /// </remarks>
        public static Func<T, TProperty> For<T, TProperty>(string propertyName, Expression<Func<T, TProperty>> expression)
        {
            // The static lambda plus its state argument, rather than a capturing one: a closure over
            // `expression` would be allocated on every call, including the cache hits this exists for.
            return Accessors<T, TProperty>.ByPropertyName.GetOrAdd(
                propertyName,
                static (_, toCompile) => toCompile.Compile(),
                expression);
        }

        /// <summary>
        /// The accessors of one owner-and-property-type pair, keyed by the property name alone.
        /// </summary>
        /// <remarks>
        /// A dictionary per closed generic rather than one keyed by <see cref="Type"/>. A static of a
        /// generic instantiation lives in that instantiation's loader allocator, so a validator declared
        /// for a type from a collectible <c>AssemblyLoadContext</c> dies with the context; a
        /// <see cref="Type"/> used as a key would root its assembly for the life of the process, and a
        /// plugin host that loads and unloads repeatedly would never reclaim any of it.
        /// <para>
        /// It also states the "property type is part of the key" rule in the type system rather than in
        /// a tuple, and spares every cache hit the tuple's hashing and a cast back from
        /// <see cref="Delegate"/>.
        /// </para>
        /// </remarks>
        private static class Accessors<T, TProperty>
        {
            public static readonly ConcurrentDictionary<string, Func<T, TProperty>> ByPropertyName =
                new(StringComparer.Ordinal);
        }
    }
}
