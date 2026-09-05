using System.Linq.Expressions;

namespace NValidation.Internals
{
    /// <summary>
    /// The other side of a rule which compares two properties of the same object: the code it is
    /// reported under and the compiled accessor that reads it.
    /// </summary>
    internal static class OtherProperty
    {
        public static OtherProperty<T, TValue> Of<T, TValue>(Expression<Func<T, TValue>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            var code = PropertyPath.From(expression);

            return new OtherProperty<T, TValue>(code, PropertyAccessor.For(code, expression));
        }
    }

    /// <inheritdoc cref="OtherProperty"/>
    internal sealed class OtherProperty<T, TValue>
    {
        public OtherProperty(string code, Func<T, TValue> read)
        {
            this.Code = code;
            this.Read = read;
        }

        public string Code { get; }

        public Func<T, TValue> Read { get; }

        /// <summary>
        /// Reports the failure under the property being validated, naming the compared property by
        /// whatever display name it declared.
        /// </summary>
        /// <remarks>
        /// Generic in the validated property's own type, because the two sides of a comparison do not
        /// have to be declared with the same nullability.
        /// </remarks>
        public void AddError<TProperty>(RuleContext<T, TProperty> context, ComparisonKind kind)
        {
            context.AddError(
                Comparison.OtherPropertyMessageKey(kind),
                (ValidationMessagePlaceholders.OtherPropertyName, context.GetDisplayName(this.Code)));
        }
    }
}
