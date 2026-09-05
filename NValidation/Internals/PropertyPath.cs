using System.Linq.Expressions;

namespace NValidation.Internals
{
    /// <summary>
    /// Turns a property expression into the dotted code the error is reported under.
    /// </summary>
    internal static class PropertyPath
    {
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
                body = member.Expression!;
            }

            if (segments.Count == 0)
            {
                throw new ArgumentException($"'{expression}' must select a property, e.g. x => x.Name.", nameof(expression));
            }

            segments.Reverse();

            return string.Join('.', segments);
        }
    }
}
