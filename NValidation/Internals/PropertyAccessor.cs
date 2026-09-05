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
        private static readonly ConcurrentDictionary<(Type Owner, Type Property, string Path), Delegate> Accessors = new();

        /// <remarks>
        /// The property type is part of the key, not just the owner and the path:
        /// <see cref="PropertyPath"/> strips conversions, so <c>x => x.Age</c> and
        /// <c>x => (object)x.Age</c> produce the same path while needing different delegates.
        /// </remarks>
        public static Func<T, TProperty> For<T, TProperty>(string path, Expression<Func<T, TProperty>> expression)
        {
            // The static lambda plus its state argument, rather than a capturing one: a closure over
            // `expression` would be allocated on every call, including the cache hits this exists for.
            return (Func<T, TProperty>)Accessors.GetOrAdd(
                (typeof(T), typeof(TProperty), path),
                static (_, toCompile) => toCompile.Compile(),
                expression);
        }
    }
}
