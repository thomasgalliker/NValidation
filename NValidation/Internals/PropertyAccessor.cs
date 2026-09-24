using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NValidation.Internals
{
    internal static class PropertyAccessor
    {
        /// <summary>
        /// The property type is part of the key: <c>PropertyPath</c> strips conversions, so <c>x =&gt; x.Age</c>
        /// and <c>x =&gt; (object)x.Age</c> produce the same name while needing different delegates.
        /// </summary>
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
        /// Held per closed generic rather than under a <see cref="Type"/> key, so a validator declared for a
        /// type from a collectible <c>AssemblyLoadContext</c> does not root that context.
        /// </summary>
        private static class Accessors<T, TProperty>
        {
            public static readonly ConcurrentDictionary<string, Func<T, TProperty>> ByPropertyName =
                new(StringComparer.Ordinal);
        }
    }
}
