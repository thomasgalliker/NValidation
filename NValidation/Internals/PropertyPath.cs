using System.Linq.Expressions;

namespace NValidation.Internals
{
    /// <summary>
    /// Turns a property expression into the dotted code the error is reported under.
    /// </summary>
    internal static class PropertyPath
    {
        /// <summary>
        /// The path of a rule declared for the instance itself rather than for one of its properties.
        /// Empty because there is no member to name: an element of a collection of scalars is identified
        /// by its position alone, so <c>ServiceMileages[1]</c> is the whole code.
        /// </summary>
        public const string Self = "";

        public static string From(LambdaExpression expression)
        {
            var segments = new List<string>();
            var body = expression.Body;

            // A value-typed property reached through Func<T, object> is wrapped in a conversion.
            while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            {
                body = unary.Operand;
            }

            while (body is MemberExpression member)
            {
                segments.Add(member.Member.Name);
                body = member.Expression;
            }

            // The walk has to arrive at the lambda's own parameter. Anything else — an indexer, a method
            // call, a captured variable, a static member — is dropped by the walk above, leaving a path
            // that names only the trailing members: x => x.Items[0].Name and x => x.Items[1].Name would
            // both come out as "Name". That path is the error code, and it is also what the compiled
            // accessor and the reachability guard are cached under, so two expressions sharing one would
            // silently share a delegate and validate the wrong value.
            if (segments.Count == 0 || !IsTheLambdasParameter(expression, body))
            {
                throw new ArgumentException(
                    $"'{expression}' must select a property of the validated object through its own parameter, " +
                    "e.g. x => x.Name or x => x.Address.Street.",
                    nameof(expression));
            }

            segments.Reverse();

            return string.Join('.', segments);
        }

        private static bool IsTheLambdasParameter(LambdaExpression expression, Expression? body)
        {
            return expression.Parameters.Count == 1 && ReferenceEquals(body, expression.Parameters[0]);
        }
    }
}
