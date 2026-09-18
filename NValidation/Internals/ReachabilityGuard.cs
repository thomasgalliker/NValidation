using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NValidation.Internals
{
    /// <summary>
    /// Whether the objects on the way to a property are there to be read through, so a chain declared
    /// through something the payload omitted is skipped rather than throwing.
    /// </summary>
    internal static class ReachabilityGuard
    {
        /// <summary>
        /// A predicate that is false when something on the way to the property is null, or null where the
        /// path dereferences nothing and so can always be read.
        /// </summary>
        public static Func<T, bool>? For<T, TProperty>(string propertyName, Expression<Func<T, TProperty>> expression)
        {
            return Guards<T, TProperty>.ByPropertyName.GetOrAdd(
                propertyName,
                static (_, state) => Build(state),
                expression);
        }

        /// <summary>
        /// A <c>null</c> entry is an answer, not a miss: it records that the path dereferences nothing. Held
        /// per closed generic, so a collectible <c>AssemblyLoadContext</c> is not rooted.
        /// </summary>
        private static class Guards<T, TProperty>
        {
            public static readonly ConcurrentDictionary<string, Func<T, bool>?> ByPropertyName =
                new(StringComparer.Ordinal);
        }

        private static Func<T, bool>? Build<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var body = expression.Body;

            // A value-typed property reached through Func<T, object> is wrapped in a conversion.
            while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            {
                body = unary.Operand;
            }

            // Outermost member first, so the owners come out deepest-first and the guard is built
            // inside out.
            var dereferenced = new List<Expression>();

            while (body is MemberExpression member)
            {
                var owner = member.Expression;

                if (owner == null)
                {
                    // A static member owns nothing that could be missing.
                    break;
                }

                if (owner is not ParameterExpression && CanBeNull(owner.Type))
                {
                    dereferenced.Add(owner);
                }

                body = owner;
            }

            if (dereferenced.Count == 0)
            {
                return null;
            }

            // The deepest owner seeds the guard and each shallower one goes in front of it: AndAlso, and
            // the shallower test first, because the deeper one cannot be evaluated until the one before
            // it is known to hold.
            var guard = IsPresent(dereferenced[0]);

            for (var i = 1; i < dereferenced.Count; i++)
            {
                guard = Expression.AndAlso(IsPresent(dereferenced[i]), guard);
            }

            return Expression.Lambda<Func<T, bool>>(guard, expression.Parameters).Compile();
        }

        private static BinaryExpression IsPresent(Expression owner)
        {
            return Expression.NotEqual(owner, Expression.Constant(null, owner.Type));
        }

        private static bool CanBeNull(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }
    }
}
