using System.Linq.Expressions;

namespace NValidation.Internals
{
    internal static class PropertyPath
    {
        /// <summary>
        /// The path of a rule declared for the instance itself rather than for one of its properties. Empty
        /// because there is no member to name: an element of a collection of scalars is identified by its
        /// position alone, so ServiceMileages[1] is the whole name.
        /// </summary>
        public const string Self = "";

        public static string From(LambdaExpression expression)
        {
            var body = expression.Body;

            // A value-typed property reached through Func<T, object> is wrapped in a conversion.
            while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            {
                body = unary.Operand;
            }

            // Counted before anything is built, so the overwhelmingly common single-segment path costs
            // no list, no array and no join, and a deeper one fills an array of exactly the right size.
            var depth = 0;
            var innermost = body;

            while (innermost is MemberExpression member)
            {
                depth++;
                innermost = member.Expression;
            }

            // The walk has to arrive at the lambda's own parameter. Anything else — an indexer, a method
            // call, a captured variable, a static member — is dropped by the walk above, leaving a path
            // that names only the trailing members: x => x.Items[0].Name and x => x.Items[1].Name would
            // both come out as "Name". That path is the property name the error is reported under, and it
            // is also what the compiled accessor and the reachability guard are cached under, so two
            // expressions sharing one would silently share a delegate and validate the wrong value.
            if (depth == 0 || !IsTheLambdasParameter(expression, innermost))
            {
                throw new ArgumentException(
                    $"'{expression}' must select a property of the validated object through its own parameter, " +
                    "e.g. x => x.Name or x => x.Address.Street.",
                    nameof(expression));
            }

            if (depth == 1)
            {
                return ((MemberExpression)body).Member.Name;
            }

            var segments = new string[depth];
            var node = body;

            // Filled back to front, because the walk reaches the outermost member first.
            for (var i = depth - 1; i >= 0; i--)
            {
                var member = (MemberExpression)node!;

                segments[i] = member.Member.Name;
                node = member.Expression;
            }

            return string.Join('.', segments);
        }

        private static bool IsTheLambdasParameter(LambdaExpression expression, Expression? body)
        {
            return expression.Parameters.Count == 1 && ReferenceEquals(body, expression.Parameters[0]);
        }
    }
}
