using System.Collections;
using NValidation.Internals;

namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires a value to be present: not <c>null</c>, and not blank for a string.
        /// </summary>
        public static PropertyRuleBuilder<T, string?> NotEmpty<T>(this PropertyRuleBuilder<T, string?> builder)
        {
            return builder.Add(context =>
            {
                if (string.IsNullOrWhiteSpace(context.Value))
                {
                    context.AddError(ValidationMessageKeys.NotEmpty);
                }
            });
        }

        /// <summary>
        /// Requires a value type to have been set. Its default — an enum's zero member, a
        /// <see cref="DateTime"/> of <see cref="DateTime.MinValue"/>, a zero <see cref="Guid"/> — is
        /// what arrives while nothing was chosen, so it counts as missing.
        /// </summary>
        /// <remarks>
        /// The counterpart of <c>NotEmpty</c>: <c>NotEmpty</c> asks whether there is any content, which
        /// only a string or a collection can answer, while this asks whether a value was set at all.
        /// Pair it with <see cref="IsInEnum{T, TEnum}"/> on an enum to also reject a number which is no
        /// member at all.
        /// </remarks>
        public static PropertyRuleBuilder<T, TValue> NotDefault<T, TValue>(this PropertyRuleBuilder<T, TValue> builder)
            where TValue : struct
        {
            return builder.Add(context =>
            {
                // EqualityComparer<TValue>.Default is devirtualized for a struct; Equals(object) would
                // box the value and the default it is compared against, on every check.
                if (EqualityComparer<TValue>.Default.Equals(context.Value, default))
                {
                    context.AddError(ValidationMessageKeys.NotDefault);
                }
            });
        }

        /// <summary>
        /// The form for a property which may be absent: both a missing value and a default one count as
        /// missing.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> NotDefault<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder)
            where TValue : struct
        {
            return builder.Add(context =>
            {
                if (context.Value is not { } value || EqualityComparer<TValue>.Default.Equals(value, default))
                {
                    context.AddError(ValidationMessageKeys.NotDefault);
                }
            });
        }

        /// <summary>
        /// Requires a collection to hold at least one entry. A missing collection counts as empty.
        /// </summary>
        /// <remarks>
        /// Declared for any <see cref="IEnumerable"/>, so it works whatever collection type the property
        /// is declared as — <c>List&lt;T&gt;</c> and arrays included, not only the interfaces. A
        /// <see cref="string"/> is also enumerable, but the string overload above is the more specific
        /// one and is what a string property binds to.
        /// </remarks>
        public static PropertyRuleBuilder<T, TCollection> NotEmpty<T, TCollection>(this PropertyRuleBuilder<T, TCollection> builder)
            where TCollection : IEnumerable?
        {
            return builder.Add(context =>
            {
                if (context.Value == null || CollectionCount.IsEmpty(context.Value))
                {
                    context.AddError(ValidationMessageKeys.NotEmpty);
                }
            });
        }

        /// <summary>
        /// Requires the object to be present. A nested object is validated by its own validator through
        /// <see cref="SetValidator{T, TProperty}"/>; this is what requires it to be there at all.
        /// </summary>
        public static PropertyRuleBuilder<T, TProperty?> NotNull<T, TProperty>(this PropertyRuleBuilder<T, TProperty?> builder)
            where TProperty : class
        {
            return builder.Add(context =>
            {
                if (context.Value == null)
                {
                    context.AddError(ValidationMessageKeys.NotNull);
                }
            });
        }

        /// <summary>
        /// The form for a value type — a quantity which the contract makes optional but this payload
        /// cannot go without.
        /// </summary>
        public static PropertyRuleBuilder<T, TProperty?> NotNull<T, TProperty>(this PropertyRuleBuilder<T, TProperty?> builder)
            where TProperty : struct
        {
            return builder.Add(context =>
            {
                if (context.Value == null)
                {
                    context.AddError(ValidationMessageKeys.NotNull);
                }
            });
        }
    }
}
