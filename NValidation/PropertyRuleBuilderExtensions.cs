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
        /// A one-off rule whose message the host resolves, like a shipped rule's:
        /// <c>this.Property(x => x.Plate).Must(p => p is null || p.StartsWith("CH")).WithErrorCode("SwissPlate");</c>
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.Must"/> unless the chain names the rule with
        /// <c>WithErrorCode</c> — which is also what selects the text, so an application's own rule
        /// localizes exactly as a shipped one does instead of carrying a literal. The overloads taking a
        /// message are for the rule whose wording is genuinely a one-off.
        /// </remarks>
        public static PropertyRuleBuilder<T, TProperty> Must<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty> builder,
            Func<TProperty, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            return builder.Add(context =>
            {
                if (!predicate(context.Value))
                {
                    context.AddError(ValidationErrorCodes.Must);
                }
            });
        }

        /// <summary>
        /// The same, for a rule which needs another property of the same object to decide.
        /// </summary>
        /// <inheritdoc cref="Must{T, TProperty}(PropertyRuleBuilder{T, TProperty}, Func{TProperty, bool})" path="/remarks"/>
        public static PropertyRuleBuilder<T, TProperty> Must<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty> builder,
            Func<T, TProperty, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            return builder.Add(context =>
            {
                if (!predicate(context.Instance, context.Value))
                {
                    context.AddError(ValidationErrorCodes.Must);
                }
            });
        }

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
                    context.AddError(new ValidationError(context.PropertyName, message(), ValidationErrorCodes.Must));
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
                    context.AddError(new ValidationError(context.PropertyName, message(), ValidationErrorCodes.Must));
                }
            });
        }

        /// <summary>
        /// A one-off rule which has to await something — a uniqueness check against a database, a
        /// lookup against another service:
        /// <code>this.Property(x => x.Vin).MustAsync((vin, ct) => this.vins.IsFreeAsync(vin, ct));</code>
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.Must"/> like its synchronous counterpart, so
        /// <c>WithErrorCode</c> names it and <c>WithMessage</c> words it. Only the
        /// <see cref="ValueTask{TResult}"/> shape ships: an overload taking a <see cref="Task{TResult}"/>
        /// would make every <c>async</c> lambda ambiguous between the two.
        /// </remarks>
        public static PropertyRuleBuilder<T, TProperty> MustAsync<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty> builder,
            Func<TProperty, CancellationToken, ValueTask<bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            return builder.AddAsync(async (context, cancellationToken) =>
            {
                if (!await predicate(context.Value, cancellationToken))
                {
                    context.AddError(ValidationErrorCodes.Must);
                }
            });
        }

        /// <summary>
        /// The same, for a rule which needs the rest of the object to decide.
        /// </summary>
        /// <inheritdoc cref="MustAsync{T, TProperty}(PropertyRuleBuilder{T, TProperty}, Func{TProperty, CancellationToken, ValueTask{bool}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, TProperty> MustAsync<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty> builder,
            Func<T, TProperty, CancellationToken, ValueTask<bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            return builder.AddAsync(async (context, cancellationToken) =>
            {
                if (!await predicate(context.Instance, context.Value, cancellationToken))
                {
                    context.AddError(ValidationErrorCodes.Must);
                }
            });
        }

        /// <summary>
        /// Validates a nested object with its own validator and merges the result, prefixing each error
        /// with this property's name (so <c>Street</c> is reported as <c>Address.Street</c>). The child
        /// keeps its own flat property names and stays independently testable.
        /// </summary>
        public static PropertyRuleBuilder<T, TProperty?> SetValidator<T, TProperty>(
            this PropertyRuleBuilder<T, TProperty?> builder,
            IValidator<TProperty> validator)
            where TProperty : class
        {
            ArgumentNullException.ThrowIfNull(validator);

            return builder.AddComposed(async (context, cancellationToken) =>
            {
                if (context.Value == null)
                {
                    return;
                }

                var result = await NestedValidation.ValidateAsync(
                    validator, context.Value, context.Messages, context.RequestedBehaviors, cancellationToken);

                foreach (var error in result.Errors)
                {
                    context.AddComposedError(new ValidationError(
                        $"{context.PropertyName}.{error.PropertyName}", error.Message, error.ErrorCode, error.Arguments));
                }
            });
        }
    }
}
