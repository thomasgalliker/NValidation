using System.Linq.Expressions;
using NValidation.Internals;

namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires the value to be greater than <paramref name="value"/>.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.GreaterThan"/>.</remarks>
        public static PropertyRuleBuilder<T, TValue> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// Requires the value to be greater than <paramref name="value"/>. A missing value passes;
        /// use <c>NotNull()</c> to require one.
        /// </summary>
        /// <inheritdoc cref="GreaterThan{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// Requires the value to be greater than <paramref name="otherProperty"/>. A missing value on
        /// the other property passes.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.GreaterThanOtherProperty"/>, naming that property.</remarks>
        public static PropertyRuleBuilder<T, TValue> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// Requires the value to be greater than <paramref name="otherProperty"/>. A missing value on
        /// either side passes.
        /// </summary>
        /// <inheritdoc cref="GreaterThan{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// Requires the value to be greater than or equal to <paramref name="value"/>.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.GreaterThanOrEqualTo"/>.</remarks>
        public static PropertyRuleBuilder<T, TValue> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be greater than or equal to <paramref name="value"/>. A missing value passes;
        /// use <c>NotNull()</c> to require one.
        /// </summary>
        /// <inheritdoc cref="GreaterThanOrEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be greater than or equal to <paramref name="otherProperty"/>. A missing value on
        /// the other property passes.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.GreaterThanOrEqualToOtherProperty"/>, naming that property.</remarks>
        public static PropertyRuleBuilder<T, TValue> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be greater than or equal to <paramref name="otherProperty"/>. A missing value on
        /// either side passes.
        /// </summary>
        /// <inheritdoc cref="GreaterThanOrEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be less than <paramref name="value"/>.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.LessThan"/>.</remarks>
        public static PropertyRuleBuilder<T, TValue> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.LessThan);
        }

        /// <summary>
        /// Requires the value to be less than <paramref name="value"/>. A missing value passes;
        /// use <c>NotNull()</c> to require one.
        /// </summary>
        /// <inheritdoc cref="LessThan{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.LessThan);
        }

        /// <summary>
        /// Requires the value to be less than <paramref name="otherProperty"/>. A missing value on
        /// the other property passes.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.LessThanOtherProperty"/>, naming that property.</remarks>
        public static PropertyRuleBuilder<T, TValue> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThan);
        }

        /// <summary>
        /// Requires the value to be less than <paramref name="otherProperty"/>. A missing value on
        /// either side passes.
        /// </summary>
        /// <inheritdoc cref="LessThan{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThan);
        }

        /// <summary>
        /// Requires the value to be less than or equal to <paramref name="value"/>.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.LessThanOrEqualTo"/>.</remarks>
        public static PropertyRuleBuilder<T, TValue> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be less than or equal to <paramref name="value"/>. A missing value passes;
        /// use <c>NotNull()</c> to require one.
        /// </summary>
        /// <inheritdoc cref="LessThanOrEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be less than or equal to <paramref name="otherProperty"/>. A missing value on
        /// the other property passes.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.LessThanOrEqualToOtherProperty"/>, naming that property.</remarks>
        public static PropertyRuleBuilder<T, TValue> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be less than or equal to <paramref name="otherProperty"/>. A missing value on
        /// either side passes.
        /// </summary>
        /// <inheritdoc cref="LessThanOrEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to lie between <paramref name="from"/> and <paramref name="to"/>, both
        /// bounds included.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.Between"/>, or the matching <c>BetweenExclusive</c>
        /// key where a bound is excluded, so the message never names a value the rule refuses as one
        /// of the permitted ones.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="from"/> is greater than <paramref name="to"/>, which is a range nothing can
        /// satisfy.
        /// </exception>
        public static PropertyRuleBuilder<T, TValue> Between<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue from, TValue to)
            where TValue : struct
        {
            return builder.Between(from, to, inclusiveFrom: true, inclusiveTo: true);
        }

        /// <summary>
        /// Requires the value to lie between <paramref name="from"/> and <paramref name="to"/>, with both
        /// bounds included or both excluded.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/remarks"/>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue> Between<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue from, TValue to, bool inclusive)
            where TValue : struct
        {
            return builder.Between(from, to, inclusive, inclusive);
        }

        /// <summary>
        /// Requires the value to lie between <paramref name="from"/> and <paramref name="to"/>, with each
        /// bound included or excluded on its own — a value which may reach its maximum but must
        /// stay above zero, say.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/remarks"/>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue> Between<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue from, TValue to, bool inclusiveFrom, bool inclusiveTo)
            where TValue : struct
        {
            RequireOrderedBounds(from, to);

            return builder.Add(context =>
            {
                if (IsOutside(context.Value, from, to, inclusiveFrom, inclusiveTo))
                {
                    AddBetweenError(context, from, to, inclusiveFrom, inclusiveTo);
                }
            });
        }

        /// <summary>
        /// Requires the value to lie between <paramref name="from"/> and <paramref name="to"/>, both
        /// bounds included. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/remarks"/>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue?> Between<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue from, TValue to)
            where TValue : struct
        {
            return builder.Between(from, to, inclusiveFrom: true, inclusiveTo: true);
        }

        /// <summary>
        /// Requires the value to lie between <paramref name="from"/> and <paramref name="to"/>, with both
        /// bounds included or both excluded. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/remarks"/>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue?> Between<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue from, TValue to, bool inclusive)
            where TValue : struct
        {
            return builder.Between(from, to, inclusive, inclusive);
        }

        /// <summary>
        /// Requires the value to lie between <paramref name="from"/> and <paramref name="to"/>, with each
        /// bound included or excluded on its own. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/remarks"/>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue?> Between<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue from, TValue to, bool inclusiveFrom, bool inclusiveTo)
            where TValue : struct
        {
            RequireOrderedBounds(from, to);

            return builder.Add(context =>
            {
                if (context.Value is { } value && IsOutside(value, from, to, inclusiveFrom, inclusiveTo))
                {
                    AddBetweenError(context, from, to, inclusiveFrom, inclusiveTo);
                }
            });
        }

        /// <summary>
        /// Requires the value to equal <paramref name="value"/>.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.EqualTo"/>.</remarks>
        public static PropertyRuleBuilder<T, TValue> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CheckAgainst(value, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the value to equal <paramref name="value"/>. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CheckAgainst(value, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the value to differ from <paramref name="value"/>.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.NotEqualTo"/>.</remarks>
        public static PropertyRuleBuilder<T, TValue> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CheckAgainst(value, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the value to differ from <paramref name="value"/>. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="NotEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CheckAgainst(value, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the text to equal <paramref name="value"/>, compared ordinally. A missing
        /// value passes; use <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(this PropertyRuleBuilder<T, string?> builder, string? value)
        {
            return builder.CheckAgainst(value, StringComparison.Ordinal, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the text to equal <paramref name="value"/>, compared the way
        /// <paramref name="comparison"/> says — a country code a client may send in any casing, typically.
        /// </summary>
        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(this PropertyRuleBuilder<T, string?> builder, string? value, StringComparison comparison)
        {
            return builder.CheckAgainst(value, comparison, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the text to differ from <paramref name="value"/>, compared ordinally. A missing
        /// value passes; use <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <inheritdoc cref="NotEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(this PropertyRuleBuilder<T, string?> builder, string? value)
        {
            return builder.CheckAgainst(value, StringComparison.Ordinal, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the text to differ from <paramref name="value"/>, compared the way
        /// <paramref name="comparison"/> says — a country code a client may send in any casing, typically.
        /// </summary>
        /// <inheritdoc cref="NotEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue)" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(this PropertyRuleBuilder<T, string?> builder, string? value, StringComparison comparison)
        {
            return builder.CheckAgainst(value, comparison, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the value to equal <paramref name="otherProperty"/>. A missing value on
        /// the other property passes.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.EqualToOtherProperty"/>, naming that property.</remarks>
        public static PropertyRuleBuilder<T, TValue> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CheckAgainst(otherProperty, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the value to equal <paramref name="otherProperty"/>. A missing value on
        /// either side passes.
        /// </summary>
        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CheckAgainst(otherProperty, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the value to differ from <paramref name="otherProperty"/>. A missing value on
        /// the other property passes.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.NotEqualToOtherProperty"/>, naming that property.</remarks>
        public static PropertyRuleBuilder<T, TValue> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CheckAgainst(otherProperty, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the value to differ from <paramref name="otherProperty"/>. A missing value on
        /// either side passes.
        /// </summary>
        /// <inheritdoc cref="NotEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, TValue?> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CheckAgainst(otherProperty, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the text to equal <paramref name="otherProperty"/>, compared ordinally. A
        /// missing value on either side passes.
        /// </summary>
        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty)
        {
            return builder.CheckAgainst(otherProperty, StringComparison.Ordinal, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the text to equal <paramref name="otherProperty"/>, compared the way
        /// <paramref name="comparison"/> says. A missing value on either side passes.
        /// </summary>
        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty, StringComparison comparison)
        {
            return builder.CheckAgainst(otherProperty, comparison, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the text to differ from <paramref name="otherProperty"/>, compared ordinally. A
        /// missing value on either side passes.
        /// </summary>
        /// <inheritdoc cref="NotEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty)
        {
            return builder.CheckAgainst(otherProperty, StringComparison.Ordinal, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the text to differ from <paramref name="otherProperty"/>, compared the way
        /// <paramref name="comparison"/> says. A missing value on either side passes.
        /// </summary>
        /// <inheritdoc cref="NotEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty, StringComparison comparison)
        {
            return builder.CheckAgainst(otherProperty, comparison, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the value to be one of <paramref name="values"/> — a closed set the contract
        /// names, e.g. the currencies an endpoint settles in.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.OneOf"/>, whose message names the allowed values. An
        /// allowlist may say what it allows; a blocklist which reported its own entries would be one the
        /// next value works around, which is why <c>NotContaining</c> names none of them.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
        public static PropertyRuleBuilder<T, TValue> OneOf<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, params TValue[] values)
            where TValue : struct
        {
            var allowed = RequireValues(values);

            return builder.Add(context =>
            {
                if (!Contains(allowed, context.Value))
                {
                    AddOneOfError(context, allowed);
                }
            });
        }

        /// <summary>
        /// Requires the value to be one of <paramref name="values"/>. A missing value passes; use
        /// <c>NotNull()</c> to require one.
        /// </summary>
        /// <inheritdoc cref="OneOf{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue[])" path="/remarks"/>
        /// <inheritdoc cref="OneOf{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue[])" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue?> OneOf<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, params TValue[] values)
            where TValue : struct
        {
            var allowed = RequireValues(values);

            return builder.Add(context =>
            {
                if (context.Value is { } actual && !Contains(allowed, actual))
                {
                    AddOneOfError(context, allowed);
                }
            });
        }

        /// <summary>
        /// Requires the text to be one of <paramref name="values"/>, compared ordinally. A missing
        /// value passes.
        /// </summary>
        /// <inheritdoc cref="OneOf{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue[])" path="/remarks"/>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty, or names a blank entry.</exception>
        public static PropertyRuleBuilder<T, string?> OneOf<T>(this PropertyRuleBuilder<T, string?> builder, params string[] values)
        {
            return builder.OneOf(StringComparison.Ordinal, values);
        }

        /// <summary>
        /// Requires the text to be one of <paramref name="values"/>, compared the way
        /// <paramref name="comparison"/> says — a code a client may send in any casing, typically.
        /// </summary>
        /// <inheritdoc cref="OneOf{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue[])" path="/remarks"/>
        /// <inheritdoc cref="OneOf{T}(PropertyRuleBuilder{T, System.String}, System.String[])" path="/exception"/>
        public static PropertyRuleBuilder<T, string?> OneOf<T>(
            this PropertyRuleBuilder<T, string?> builder,
            StringComparison comparison,
            params string[] values)
        {
            var allowed = RequireTerms(values, nameof(values));

            return builder.Add(context =>
            {
                if (context.Value is not { } actual)
                {
                    return;
                }

                foreach (var candidate in allowed)
                {
                    if (string.Equals(actual, candidate, comparison))
                    {
                        return;
                    }
                }

                context.AddError(
                    ValidationErrorCodes.OneOf,
                    (ValidationMessagePlaceholders.AllowedValues, string.Join(", ", allowed)));
            });
        }

        private static bool Contains<TValue>(TValue[] allowed, TValue value)
            where TValue : struct
        {
            foreach (var candidate in allowed)
            {
                if (EqualityComparer<TValue>.Default.Equals(candidate, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddOneOfError<T, TProperty, TValue>(RuleContext<T, TProperty> context, TValue[] allowed)
            where TValue : struct
        {
            context.AddError(
                ValidationErrorCodes.OneOf,
                (ValidationMessagePlaceholders.AllowedValues, string.Join(", ", allowed)));
        }

        private static TValue[] RequireValues<TValue>(TValue[] values)
            where TValue : struct
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length == 0)
            {
                throw new ArgumentException(
                    "Name at least one value; a rule which allows nothing refuses everything.", nameof(values));
            }

            return values;
        }

        private static bool IsOutside<TValue>(TValue value, TValue from, TValue to, bool inclusiveFrom, bool inclusiveTo)
            where TValue : struct
        {
            var comparedToFrom = Comparer<TValue>.Default.Compare(value, from);
            var comparedToTo = Comparer<TValue>.Default.Compare(value, to);

            var belowFrom = inclusiveFrom ? comparedToFrom < 0 : comparedToFrom <= 0;
            var aboveTo = inclusiveTo ? comparedToTo > 0 : comparedToTo >= 0;

            return belowFrom || aboveTo;
        }

        /// <summary>
        /// Reports under the key matching the bounds actually applied, so the message never names a value the
        /// rule just refused as one of the permitted ones.
        /// </summary>
        private static void AddBetweenError<T, TProperty, TValue>(
            RuleContext<T, TProperty> context, TValue from, TValue to, bool inclusiveFrom, bool inclusiveTo)
            where TValue : struct
        {
            context.AddError(
                BetweenErrorCode(inclusiveFrom, inclusiveTo),
                (ValidationMessagePlaceholders.From, from),
                (ValidationMessagePlaceholders.To, to));
        }

        private static string BetweenErrorCode(bool inclusiveFrom, bool inclusiveTo)
        {
            return (inclusiveFrom, inclusiveTo) switch
            {
                (true, true) => ValidationErrorCodes.Between,
                (false, false) => ValidationErrorCodes.BetweenExclusive,
                (false, true) => ValidationErrorCodes.BetweenExclusiveFrom,
                (true, false) => ValidationErrorCodes.BetweenExclusiveTo,
            };
        }

        /// <summary>
        /// Refuses a value type nothing can put in order, where the rule is written rather than on the
        /// request that trips it.
        /// </summary>
        private static void RequireOrderable<TValue>()
            where TValue : struct
        {
            if (!Orderable<TValue>.IsSupported)
            {
                throw new ArgumentException(
                    $"{typeof(TValue).GetFormattedFullName()} cannot be ordered: it implements " +
                    $"IComparable<{typeof(TValue).GetFormattedName()}> " +
                    "for no type, so there is no order to compare against. Judge it with Must(...) and " +
                    "name the rule with WithErrorCode.");
            }
        }

        private static void RequireOrderedBounds<TValue>(TValue from, TValue to)
            where TValue : struct
        {
            RequireOrderable<TValue>();

            if (Comparer<TValue>.Default.Compare(from, to) > 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(from), from, $"The lower bound must not be greater than the upper bound of {to}.");
            }
        }

        private static PropertyRuleBuilder<T, TValue> CompareTo<T, TValue>(
            this PropertyRuleBuilder<T, TValue> builder, TValue value, ComparisonKind kind)
            where TValue : struct
        {
            RequireOrderable<TValue>();

            return builder.Add(context =>
            {
                if (!Comparison.IsSatisfied(Comparer<TValue>.Default.Compare(context.Value, value), kind))
                {
                    context.AddError(Comparison.ValueErrorCode(kind), (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue?> CompareTo<T, TValue>(
            this PropertyRuleBuilder<T, TValue?> builder, TValue value, ComparisonKind kind)
            where TValue : struct
        {
            RequireOrderable<TValue>();

            return builder.Add(context =>
            {
                if (context.Value is { } actual && !Comparison.IsSatisfied(Comparer<TValue>.Default.Compare(actual, value), kind))
                {
                    context.AddError(Comparison.ValueErrorCode(kind), (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue> CompareTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty, ComparisonKind kind)
            where TValue : struct
        {
            RequireOrderable<TValue>();

            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (other.TryRead(context.Instance, out var value) && value is { } expected &&
                    !Comparison.IsSatisfied(Comparer<TValue>.Default.Compare(context.Value, expected), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue?> CompareTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty, ComparisonKind kind)
            where TValue : struct
        {
            RequireOrderable<TValue>();

            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (context.Value is { } actual && other.TryRead(context.Instance, out var value) && value is { } expected &&
                    !Comparison.IsSatisfied(Comparer<TValue>.Default.Compare(actual, expected), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue> CheckAgainst<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value, EqualityKind kind)
            where TValue : struct
        {
            return builder.Add(context =>
            {
                if (!Equality.IsSatisfied(EqualityComparer<TValue>.Default.Equals(context.Value, value), kind))
                {
                    context.AddError(Equality.ValueErrorCode(kind), (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue?> CheckAgainst<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value, EqualityKind kind)
            where TValue : struct
        {
            return builder.Add(context =>
            {
                if (context.Value is { } actual && !Equality.IsSatisfied(EqualityComparer<TValue>.Default.Equals(actual, value), kind))
                {
                    context.AddError(Equality.ValueErrorCode(kind), (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        /// <summary>
        /// A missing value passes, as it does for every other rule about a string: whether the text has to be
        /// there at all is NotEmpty's question, not this one's.
        /// </summary>
        private static PropertyRuleBuilder<T, string?> CheckAgainst<T>(
            this PropertyRuleBuilder<T, string?> builder, string? value, StringComparison comparison, EqualityKind kind)
        {
            return builder.Add(context =>
            {
                if (context.Value is { } actual && !Equality.IsSatisfied(string.Equals(actual, value, comparison), kind))
                {
                    context.AddError(Equality.ValueErrorCode(kind), (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue> CheckAgainst<T, TValue>(
            this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty, EqualityKind kind)
            where TValue : struct
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (other.TryRead(context.Instance, out var value) && value is { } expected &&
                    !Equality.IsSatisfied(EqualityComparer<TValue>.Default.Equals(context.Value, expected), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue?> CheckAgainst<T, TValue>(
            this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty, EqualityKind kind)
            where TValue : struct
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (context.Value is { } actual && other.TryRead(context.Instance, out var value) && value is { } expected &&
                    !Equality.IsSatisfied(EqualityComparer<TValue>.Default.Equals(actual, expected), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }

        private static PropertyRuleBuilder<T, string?> CheckAgainst<T>(
            this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty, StringComparison comparison, EqualityKind kind)
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (context.Value is { } actual && other.TryRead(context.Instance, out var expected) &&
                    !Equality.IsSatisfied(string.Equals(actual, expected, comparison), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }
    }
}
