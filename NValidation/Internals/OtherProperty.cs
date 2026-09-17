using System.Linq.Expressions;

namespace NValidation.Internals
{
    internal static class OtherProperty
    {
        public static OtherProperty<T, TValue> Of<T, TValue>(Expression<Func<T, TValue>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            var propertyName = PropertyPath.From(expression);

            return new OtherProperty<T, TValue>(propertyName, PropertyAccessor.For(propertyName, expression), ReachabilityGuard.For(propertyName, expression));
        }
    }

    internal sealed class OtherProperty<T, TValue>
    {
        private readonly Func<T, TValue> read;

        private readonly Func<T, bool>? isReachable;

        public OtherProperty(string propertyName, Func<T, TValue> read, Func<T, bool>? isReachable)
        {
            this.PropertyName = propertyName;
            this.read = read;
            this.isReachable = isReachable;
        }

        public string PropertyName { get; }

        /// <summary>
        /// Reads the compared property, or reports that there is nothing to compare against. The property is
        /// guarded exactly as the validated one is: a payload which omitted an object on the way to it has
        /// nothing to compare against, and a comparison with nothing on one side passes — the same answer the
        /// rest of the library gives to something absent. Left unguarded this would dereference the missing
        /// object and turn a bad request into a server error. Whether the object in between has to be there
        /// at all is a question for a rule of its own.
        /// </summary>
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
        /// Reports the failure under the property being validated, naming the compared property by whatever
        /// display name it declared. Generic in the validated property's own type, because the two sides of a
        /// comparison do not have to be declared with the same nullability.
        /// </summary>
        public void AddError<TProperty>(RuleContext<T, TProperty> context, ComparisonKind kind)
        {
            context.AddError(
                Comparison.OtherPropertyErrorCode(kind),
                (ValidationMessagePlaceholders.OtherPropertyName, context.GetDisplayName(this.PropertyName)));
        }

        public void AddError<TProperty>(RuleContext<T, TProperty> context, EqualityKind kind)
        {
            context.AddError(
                Equality.OtherPropertyErrorCode(kind),
                (ValidationMessagePlaceholders.OtherPropertyName, context.GetDisplayName(this.PropertyName)));
        }
    }
}
