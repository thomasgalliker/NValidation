using System.Linq.Expressions;
using NValidation.Internals;

namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires the value to be greater than <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// Requires the value to be greater than or equal to <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be less than <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.LessThan);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.LessThan);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThan);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThan);
        }

        /// <summary>
        /// Requires the value to be less than or equal to <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CompareTo(value, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to lie between <paramref name="from"/> and <paramref name="to"/>, both
        /// bounds included.
        /// </summary>
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
        /// The same, with both bounds included or both excluded.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue> Between<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue from, TValue to, bool inclusive)
            where TValue : struct
        {
            return builder.Between(from, to, inclusive, inclusive);
        }

        /// <summary>
        /// The same, with each bound included or excluded on its own — e.g. a value which may reach its
        /// maximum but must stay above zero.
        /// </summary>
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
        /// The form for a property which may be absent. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue?> Between<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue from, TValue to)
            where TValue : struct
        {
            return builder.Between(from, to, inclusiveFrom: true, inclusiveTo: true);
        }

        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue, bool)"/>
        public static PropertyRuleBuilder<T, TValue?> Between<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue from, TValue to, bool inclusive)
            where TValue : struct
        {
            return builder.Between(from, to, inclusive, inclusive);
        }

        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue, bool, bool)"/>
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
        public static PropertyRuleBuilder<T, TValue> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CheckAgainst(value, EqualityKind.EqualTo);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CheckAgainst(value, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the value to differ from <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct
        {
            return builder.CheckAgainst(value, EqualityKind.NotEqualTo);
        }

        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, System.Nullable{TValue}}, TValue)" path="/summary"/>
        public static PropertyRuleBuilder<T, TValue?> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct
        {
            return builder.CheckAgainst(value, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the text to equal <paramref name="value"/>, exactly. A missing value passes; use
        /// <c>NotEmpty()</c> to require one.
        /// </summary>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(this PropertyRuleBuilder<T, string?> builder, string? value)
        {
            return builder.CheckAgainst(value, StringComparison.Ordinal, EqualityKind.EqualTo);
        }

        /// <summary>
        /// The same, comparing the way <paramref name="comparison"/> says — e.g. a country code a client may send
        /// in any casing.
        /// </summary>
        /// <inheritdoc cref="EqualTo{T}(PropertyRuleBuilder{T, string}, string)" path="/summary"/>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(this PropertyRuleBuilder<T, string?> builder, string? value, StringComparison comparison)
        {
            return builder.CheckAgainst(value, comparison, EqualityKind.EqualTo);
        }

        /// <summary>
        /// Requires the text to differ from <paramref name="value"/>, exactly. A missing value passes.
        /// </summary>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(this PropertyRuleBuilder<T, string?> builder, string? value)
        {
            return builder.CheckAgainst(value, StringComparison.Ordinal, EqualityKind.NotEqualTo);
        }

        /// <inheritdoc cref="EqualTo{T}(PropertyRuleBuilder{T, string}, string, StringComparison)" path="/summary"/>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(this PropertyRuleBuilder<T, string?> builder, string? value, StringComparison comparison)
        {
            return builder.CheckAgainst(value, comparison, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CheckAgainst(otherProperty, EqualityKind.EqualTo);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CheckAgainst(otherProperty, EqualityKind.EqualTo);
        }

        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/summary"/>
        public static PropertyRuleBuilder<T, TValue> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CheckAgainst(otherProperty, EqualityKind.NotEqualTo);
        }

        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, System.Nullable{TValue}}, Expression{Func{T, System.Nullable{TValue}}})" path="/summary"/>
        public static PropertyRuleBuilder<T, TValue?> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct
        {
            return builder.CheckAgainst(otherProperty, EqualityKind.NotEqualTo);
        }

        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/summary"/>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty)
        {
            return builder.CheckAgainst(otherProperty, StringComparison.Ordinal, EqualityKind.EqualTo);
        }

        /// <inheritdoc cref="EqualTo{T}(PropertyRuleBuilder{T, string}, string, StringComparison)" path="/summary"/>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty, StringComparison comparison)
        {
            return builder.CheckAgainst(otherProperty, comparison, EqualityKind.EqualTo);
        }

        /// <inheritdoc cref="NotEqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, System.Nullable{TValue}}})" path="/summary"/>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty)
        {
            return builder.CheckAgainst(otherProperty, StringComparison.Ordinal, EqualityKind.NotEqualTo);
        }

        /// <inheritdoc cref="EqualTo{T}(PropertyRuleBuilder{T, string}, string, StringComparison)" path="/summary"/>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(this PropertyRuleBuilder<T, string?> builder, Expression<Func<T, string?>> otherProperty, StringComparison comparison)
        {
            return builder.CheckAgainst(otherProperty, comparison, EqualityKind.NotEqualTo);
        }

        /// <summary>
        /// Requires the value to be one of <paramref name="values"/> — a closed set the contract names,
        /// e.g. the currencies an endpoint settles in.
        /// </summary>
        /// <remarks>
        /// The message names the allowed values, unlike <c>NotContaining</c>, which names none of the
        /// ones it refuses. An allowlist may say what it allows; a blocklist which reports its own
        /// entries is one the next value works around.
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
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
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
        /// The text form, compared exactly. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="OneOf{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue[])" path="/remarks"/>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty, or names a blank entry.</exception>
        public static PropertyRuleBuilder<T, string?> OneOf<T>(this PropertyRuleBuilder<T, string?> builder, params string[] values)
        {
            return builder.OneOf(StringComparison.Ordinal, values);
        }

        /// <summary>
        /// The same, comparing the way <paramref name="comparison"/> says — a code a client may send in
        /// any casing, typically.
        /// </summary>
        /// <inheritdoc cref="OneOf{T}(PropertyRuleBuilder{T, string}, string[])" path="/remarks"/>
        /// <inheritdoc cref="OneOf{T}(PropertyRuleBuilder{T, string}, string[])" path="/exception"/>
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
        /// Reports the failure under the key that matches the bounds actually applied.
        /// </summary>
        /// <remarks>
        /// A range which excludes a bound has to say so. Reporting the inclusive wording for every form
        /// would name the two values the rule just refused as the permitted ones, and no translation
        /// could put that right, because the inclusivity would never reach the provider.
        /// </remarks>
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
                    $"{typeof(TValue)} cannot be ordered: it implements IComparable<{typeof(TValue).Name}> " +
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

        /// <remarks>
        /// A missing value passes, as it does for every other rule about a string: whether the text has
        /// to be there at all is <c>NotEmpty</c>'s question, not this one's.
        /// </remarks>
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

        /// <inheritdoc cref="CheckAgainst{T}(PropertyRuleBuilder{T, string}, string, StringComparison, EqualityKind)" path="/remarks"/>
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
