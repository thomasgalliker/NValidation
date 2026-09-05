using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NValidation.Internals
{
    /// <summary>
    /// Whether the objects on the way to a property are there to be read through.
    /// </summary>
    /// <remarks>
    /// A chain declared for <c>c =&gt; c.Model.Manufacturer.Name</c> compiles to an accessor that
    /// dereferences <c>Model</c> and <c>Manufacturer</c>. Left alone it would throw a
    /// <see cref="NullReferenceException"/> for a payload that simply omitted one of them, turning a bad
    /// request into a server error. So the chain is skipped instead, which is what the rest of the
    /// library already does with something absent — a null nested object is skipped by
    /// <c>SetValidator</c>, a missing collection by its rules, an absent value by a comparison. Whether
    /// the object in between has to be there at all is a question for a rule of its own,
    /// <c>Property(c =&gt; c.Model).NotNull()</c>.
    /// </remarks>
    internal static class ReachabilityGuard
    {
        private static readonly ConcurrentDictionary<(Type Owner, Type Property, string Path), Delegate?> Guards = new();

        /// <summary>
        /// A predicate that is <c>false</c> when something on the way to the property is <c>null</c>, or
        /// <c>null</c> where the path dereferences nothing and so can always be read.
        /// </summary>
        public static Func<T, bool>? For<T, TProperty>(string path, Expression<Func<T, TProperty>> expression)
        {
            return (Func<T, bool>?)Guards.GetOrAdd(
                (typeof(T), typeof(TProperty), path),
                static (_, state) => Build(state),
                expression);
        }

        private static Delegate? Build<T, TProperty>(Expression<Func<T, TProperty>> expression)
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

            Expression? guard = null;

            foreach (var owner in dereferenced)
            {
                var isPresent = Expression.NotEqual(owner, Expression.Constant(null, owner.Type));

                // AndAlso, and the shallower test in front: the deeper one cannot be evaluated until
                // the one before it is known to hold.
                guard = guard == null ? isPresent : Expression.AndAlso(isPresent, guard);
            }

            return Expression.Lambda<Func<T, bool>>(guard!, expression.Parameters).Compile();
        }

        private static bool CanBeNull(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }
    }
}
