using System.Globalization;

namespace NValidation
{
    /// <summary>
    /// Type names for the messages a developer reads.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// The type's name the way C# spells it — <c>PropertyRuleBuilder&lt;Car, String&gt;</c> rather than
        /// the reflection form <c>PropertyRuleBuilder`2</c> — formatting the type arguments the same way.
        /// </summary>
        /// <remarks>
        /// <c>nameof</c> cannot say this: it resolves to the bare identifier, so
        /// <c>nameof(PropertyRuleBuilder&lt;T, TProperty&gt;)</c> reads "PropertyRuleBuilder" and reports
        /// nothing about what was being built.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        internal static string GetFormattedName(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return Format(type, qualified: false);
        }

        /// <summary>
        /// The same name qualified by its namespace, for a message which names a type the reader has to go
        /// and find: two validators called <c>CarValidator</c> are told apart by where they live, and
        /// nothing else.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        internal static string GetFormattedFullName(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return Format(type, qualified: true);
        }

        private static string Format(Type type, bool qualified)
        {
            // A type parameter stands for whatever is substituted for it, so T is the whole of its name.
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (type.IsArray)
            {
                var commas = new string(',', type.GetArrayRank() - 1);

                return $"{Format(type.GetElementType()!, qualified)}[{commas}]";
            }

            var name = type.Name;
            var arguments = Array.Empty<Type>();

            var arity = name.IndexOf('`');
            if (arity >= 0)
            {
                // GetGenericArguments carries a declaring type's arguments ahead of the type's own, and
                // reports an open definition's parameters where GenericTypeArguments reports none. The
                // type's own are the trailing ones, as many as the arity in its name — a type nested in a
                // generic type has no arity of its own and owns none of them.
                var count = int.Parse(name[(arity + 1)..], CultureInfo.InvariantCulture);

                arguments = type.GetGenericArguments()[^count..];
                name = name[..arity];
            }

            var qualifier = string.Empty;
            if (qualified)
            {
                if (type.IsNested)
                {
                    qualifier = $"{Format(type.DeclaringType!, qualified: true)}.";
                }
                else if (!string.IsNullOrEmpty(type.Namespace))
                {
                    qualifier = $"{type.Namespace}.";
                }
            }

            if (arguments.Length == 0)
            {
                return $"{qualifier}{name}";
            }

            return $"{qualifier}{name}<{string.Join(", ", arguments.Select(a => Format(a, qualified)))}>";
        }
    }
}
