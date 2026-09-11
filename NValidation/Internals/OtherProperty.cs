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

            return new OtherProperty<T, TValue>(code, PropertyAccessor.For(code, expression), ReachabilityGuard.For(code, expression));
        }
    }

    /// <inheritdoc cref="OtherProperty"/>
    internal sealed class OtherProperty<T, TValue>
    {
        private readonly Func<T, TValue> read;

        private readonly Func<T, bool>? isReachable;

        public OtherProperty(string code, Func<T, TValue> read, Func<T, bool>? isReachable)
        {
            this.Code = code;
            this.read = read;
            this.isReachable = isReachable;
        }

        public string Code { get; }

        /// <summary>
        /// Reads the compared property, or reports that there is nothing to compare against.
        /// </summary>
        /// <remarks>
        /// The property is guarded exactly as the validated one is: a payload which omitted an object on
        /// the way to it has nothing to compare against, and a comparison with nothing on one side
        /// passes — the same answer the rest of the library gives to something absent. Left unguarded
        /// this would dereference the missing object and turn a bad request into a server error.
        /// Whether the object in between has to be there at all is a question for a rule of its own.
        /// </remarks>
        public bool TryRead(T instance, out TValue value)
        {
            if (this.isReachable != null && !this.isReachable(instance))
            {
                value = default!;
                return false;
            }

            value = this.read(instance);
            return true;
        }

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

        /// <inheritdoc cref="AddError{TProperty}(RuleContext{T, TProperty}, ComparisonKind)"/>
        public void AddError<TProperty>(RuleContext<T, TProperty> context, EqualityKind kind)
        {
            context.AddError(
                Equality.OtherPropertyMessageKey(kind),
                (ValidationMessagePlaceholders.OtherPropertyName, context.GetDisplayName(this.Code)));
        }
    }
}
