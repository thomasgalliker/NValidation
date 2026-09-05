using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// The rules shipped with the validation core. Application-specific rules are added the same way, as
    /// extension methods on <see cref="PropertyRuleBuilder{T, TProperty}"/> from the application's own
    /// namespace.
    /// </summary>
    /// <remarks>
    /// Split across several files by subject — <c>.NullOrEmpty</c>, <c>.Text</c>, <c>.Comparison</c>,
    /// <c>.Numbers</c>, <c>.Dates</c>, <c>.Collections</c>, <c>.Enums</c> — with the two rules that take
    /// the caller's own predicate or validator here.
    /// </remarks>
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// A one-off rule which does not deserve a shared one. The message is supplied by the caller,
        /// already localized.
        /// </summary>
        public static PropertyRuleBuilder<T, TProperty> Must<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty> builder,
            Func<TProperty, bool> predicate,
            string message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return builder.Must(predicate, () => message);
        }

        /// <summary>
        /// The same, for a message which has to be resolved while the rule runs rather than while it is
        /// declared — a localized resource, for instance, depends on the culture of the current thread.
        /// </summary>
        public static PropertyRuleBuilder<T, TProperty> Must<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty> builder,
            Func<TProperty, bool> predicate,
            Func<string> message)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(message);

            return builder.Add(context =>
            {
                if (!predicate(context.Value))
                {
                    context.AddError(new ValidationError(context.Code, message()));
                }
            });
        }

        /// <summary>
        /// A one-off rule which needs another property of the same object to decide, e.g. a discount
        /// which may only be set on an order that has one.
        /// </summary>
        public static PropertyRuleBuilder<T, TProperty> Must<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty> builder,
            Func<T, TProperty, bool> predicate,
            string message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return builder.Must(predicate, () => message);
        }

        /// <summary>
        /// The same, for a message which has to be resolved while the rule runs.
        /// </summary>
        public static PropertyRuleBuilder<T, TProperty> Must<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty> builder,
            Func<T, TProperty, bool> predicate,
            Func<string> message)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(message);

            return builder.Add(context =>
            {
                if (!predicate(context.Instance, context.Value))
                {
                    context.AddError(new ValidationError(context.Code, message()));
                }
            });
        }

        /// <summary>
        /// Validates a nested object with its own validator and merges the result, prefixing each error
        /// with this property's code (so <c>Street</c> is reported as <c>Address.Street</c>). The child
        /// keeps its own flat codes and stays independently testable.
        /// </summary>
        public static PropertyRuleBuilder<T, TProperty?> SetValidator<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty?> builder,
            IValidator<TProperty> validator)
            where TProperty : class
        {
            ArgumentNullException.ThrowIfNull(validator);

            return builder.AddAsync(async (context, cancellationToken) =>
            {
                if (context.Value == null)
                {
                    return;
                }

                var result = await NestedValidation.ValidateAsync(
                    validator, context.Value, context.Messages, cancellationToken);

                foreach (var error in result.Errors)
                {
                    context.AddError(new ValidationError($"{context.Code}.{error.Code}", error.Message));
                }
            });
        }
    }
}
