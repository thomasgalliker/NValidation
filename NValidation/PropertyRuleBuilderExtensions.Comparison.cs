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
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// Requires the value to be greater than another property of the same object.
        /// </summary>
        /// <remarks>
        /// The message names the other property by the display name that property declared, so
        /// <c>this.Property(x =&gt; x.Other).WithDisplayName(...)</c> is what keeps this message readable.
        /// Without one it falls back to the other property's C# name.
        /// </remarks>
        public static PropertyRuleBuilder<T, TValue> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// The same, where this property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThan);
        }

        /// <summary>
        /// Requires the value to be greater than or equal to <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(value, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be greater than or equal to another property of the same object.
        /// </summary>
        /// <remarks>
        /// The message names the other property by the display name that property declared, so
        /// <c>this.Property(x =&gt; x.Other).WithDisplayName(...)</c> is what keeps this message readable.
        /// Without one it falls back to the other property's C# name.
        /// </remarks>
        public static PropertyRuleBuilder<T, TValue> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// The same, where this property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> GreaterThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be less than <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(value, ComparisonKind.LessThan);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(value, ComparisonKind.LessThan);
        }

        /// <summary>
        /// Requires the value to be less than another property of the same object.
        /// </summary>
        /// <remarks>
        /// The message names the other property by the display name that property declared, so
        /// <c>this.Property(x =&gt; x.Other).WithDisplayName(...)</c> is what keeps this message readable.
        /// Without one it falls back to the other property's C# name.
        /// </remarks>
        public static PropertyRuleBuilder<T, TValue> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThan);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThan);
        }

        /// <summary>
        /// The same, where this property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThan);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThan<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThan);
        }

        /// <summary>
        /// Requires the value to be less than or equal to <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(value, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes; use <c>NotNull()</c> to
        /// require one.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(value, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// Requires the value to be less than or equal to another property of the same object.
        /// </summary>
        /// <remarks>
        /// The message names the other property by the display name that property declared, so
        /// <c>this.Property(x =&gt; x.Other).WithDisplayName(...)</c> is what keeps this message readable.
        /// Without one it falls back to the other property's C# name.
        /// </remarks>
        public static PropertyRuleBuilder<T, TValue> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// The same, where the other property may be absent. Nothing to compare against passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// The same, where this property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThanOrEqualTo);
        }

        /// <summary>
        /// The same, where either property may be absent.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> LessThanOrEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty)
            where TValue : struct, IComparable<TValue>
        {
            return builder.CompareTo(otherProperty, ComparisonKind.LessThanOrEqualTo);
        }


        /// <summary>
        /// Requires the value to lie between <paramref name="from"/> and <paramref name="to"/>, by
        /// default including both bounds.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="from"/> is greater than <paramref name="to"/>, which is a range nothing can
        /// satisfy.
        /// </exception>
        public static PropertyRuleBuilder<T, TValue> Between<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue from, TValue to, bool inclusive = true)
            where TValue : struct, IComparable<TValue>
        {
            return builder.Between(from, to, inclusive, inclusive);
        }

        /// <summary>
        /// The same, with each bound included or excluded on its own — e.g. a value which may reach its
        /// maximum but must stay above zero.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue, bool)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue> Between<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue from, TValue to, bool inclusiveFrom, bool inclusiveTo)
            where TValue : struct, IComparable<TValue>
        {
            RequireOrderedBounds(from, to);

            return builder.Add(context =>
            {
                if (IsOutside(context.Value, from, to, inclusiveFrom, inclusiveTo))
                {
                    AddBetweenError(context, from, to);
                }
            });
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue, bool)" path="/exception"/>
        public static PropertyRuleBuilder<T, TValue?> Between<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue from, TValue to, bool inclusive = true)
            where TValue : struct, IComparable<TValue>
        {
            return builder.Between(from, to, inclusive, inclusive);
        }

        /// <inheritdoc cref="Between{T, TValue}(PropertyRuleBuilder{T, TValue}, TValue, TValue, bool, bool)"/>
        public static PropertyRuleBuilder<T, TValue?> Between<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue from, TValue to, bool inclusiveFrom, bool inclusiveTo)
            where TValue : struct, IComparable<TValue>
        {
            RequireOrderedBounds(from, to);

            return builder.Add(context =>
            {
                if (context.Value is { } value && IsOutside(value, from, to, inclusiveFrom, inclusiveTo))
                {
                    AddBetweenError(context, from, to);
                }
            });
        }

        /// <summary>
        /// Requires the value to equal <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct, IEquatable<TValue>
        {
            return builder.Add(context =>
            {
                if (!context.Value.Equals(value))
                {
                    context.AddError(ValidationMessageKeys.EqualTo, (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue?> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct, IEquatable<TValue>
        {
            return builder.Add(context =>
            {
                if (context.Value is { } actual && !actual.Equals(value))
                {
                    context.AddError(ValidationMessageKeys.EqualTo, (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        /// <summary>
        /// Requires the value to differ from <paramref name="value"/>.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, TValue value)
            where TValue : struct, IEquatable<TValue>
        {
            return builder.Add(context =>
            {
                if (context.Value.Equals(value))
                {
                    context.AddError(ValidationMessageKeys.NotEqualTo, (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, System.Nullable{TValue}}, TValue)" path="/summary"/>
        public static PropertyRuleBuilder<T, TValue?> NotEqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue?> builder, TValue value)
            where TValue : struct, IEquatable<TValue>
        {
            return builder.Add(context =>
            {
                if (context.Value is { } actual && actual.Equals(value))
                {
                    context.AddError(ValidationMessageKeys.NotEqualTo, (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        /// <summary>
        /// Requires the text to equal <paramref name="value"/>, by default exactly.
        /// </summary>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(
            this PropertyRuleBuilder<T, string?> builder,
            string? value,
            StringComparison comparison = StringComparison.Ordinal)
        {
            return builder.Add(context =>
            {
                if (!string.Equals(context.Value, value, comparison))
                {
                    context.AddError(ValidationMessageKeys.EqualTo, (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        /// <summary>
        /// Requires the text to differ from <paramref name="value"/>, by default exactly.
        /// </summary>
        public static PropertyRuleBuilder<T, string?> NotEqualTo<T>(
            this PropertyRuleBuilder<T, string?> builder,
            string? value,
            StringComparison comparison = StringComparison.Ordinal)
        {
            return builder.Add(context =>
            {
                if (string.Equals(context.Value, value, comparison))
                {
                    context.AddError(ValidationMessageKeys.NotEqualTo, (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        /// <summary>
        /// Requires the value to equal another property of the same object — a confirmation field,
        /// typically.
        /// </summary>
        public static PropertyRuleBuilder<T, TValue> EqualTo<T, TValue>(this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue>> otherProperty)
            where TValue : struct, IEquatable<TValue>
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (!context.Value.Equals(other.Read(context.Instance)))
                {
                    context.AddError(
                        ValidationMessageKeys.EqualToOtherProperty,
                        (ValidationMessagePlaceholders.OtherPropertyName, context.GetDisplayName(other.Code)));
                }
            });
        }

        /// <inheritdoc cref="EqualTo{T, TValue}(PropertyRuleBuilder{T, TValue}, Expression{Func{T, TValue}})" path="/summary"/>
        public static PropertyRuleBuilder<T, string?> EqualTo<T>(
            this PropertyRuleBuilder<T, string?> builder,
            Expression<Func<T, string?>> otherProperty,
            StringComparison comparison = StringComparison.Ordinal)
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (!string.Equals(context.Value, other.Read(context.Instance), comparison))
                {
                    context.AddError(
                        ValidationMessageKeys.EqualToOtherProperty,
                        (ValidationMessagePlaceholders.OtherPropertyName, context.GetDisplayName(other.Code)));
                }
            });
        }

        private static bool IsOutside<TValue>(TValue value, TValue from, TValue to, bool inclusiveFrom, bool inclusiveTo)
            where TValue : struct, IComparable<TValue>
        {
            var belowFrom = inclusiveFrom ? value.CompareTo(from) < 0 : value.CompareTo(from) <= 0;
            var aboveTo = inclusiveTo ? value.CompareTo(to) > 0 : value.CompareTo(to) >= 0;

            return belowFrom || aboveTo;
        }

        private static void AddBetweenError<T, TProperty, TValue>(RuleContext<T, TProperty> context, TValue from, TValue to)
            where TValue : struct, IComparable<TValue>
        {
            context.AddError(
                ValidationMessageKeys.Between,
                (ValidationMessagePlaceholders.From, from),
                (ValidationMessagePlaceholders.To, to));
        }

        private static void RequireOrderedBounds<TValue>(TValue from, TValue to)
            where TValue : struct, IComparable<TValue>
        {
            if (from.CompareTo(to) > 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(from), from, $"The lower bound must not be greater than the upper bound of {to}.");
            }
        }

        private static PropertyRuleBuilder<T, TValue> CompareTo<T, TValue>(
            this PropertyRuleBuilder<T, TValue> builder, TValue value, ComparisonKind kind)
            where TValue : struct, IComparable<TValue>
        {
            return builder.Add(context =>
            {
                if (!Comparison.IsSatisfied(context.Value.CompareTo(value), kind))
                {
                    context.AddError(Comparison.ValueMessageKey(kind), (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue?> CompareTo<T, TValue>(
            this PropertyRuleBuilder<T, TValue?> builder, TValue value, ComparisonKind kind)
            where TValue : struct, IComparable<TValue>
        {
            return builder.Add(context =>
            {
                if (context.Value is { } actual && !Comparison.IsSatisfied(actual.CompareTo(value), kind))
                {
                    context.AddError(Comparison.ValueMessageKey(kind), (ValidationMessagePlaceholders.OtherValue, value));
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue> CompareTo<T, TValue>(
            this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue>> otherProperty, ComparisonKind kind)
            where TValue : struct, IComparable<TValue>
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (!Comparison.IsSatisfied(context.Value.CompareTo(other.Read(context.Instance)), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue> CompareTo<T, TValue>(
            this PropertyRuleBuilder<T, TValue> builder, Expression<Func<T, TValue?>> otherProperty, ComparisonKind kind)
            where TValue : struct, IComparable<TValue>
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (other.Read(context.Instance) is { } expected &&
                    !Comparison.IsSatisfied(context.Value.CompareTo(expected), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue?> CompareTo<T, TValue>(
            this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue>> otherProperty, ComparisonKind kind)
            where TValue : struct, IComparable<TValue>
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (context.Value is { } actual &&
                    !Comparison.IsSatisfied(actual.CompareTo(other.Read(context.Instance)), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }

        private static PropertyRuleBuilder<T, TValue?> CompareTo<T, TValue>(
            this PropertyRuleBuilder<T, TValue?> builder, Expression<Func<T, TValue?>> otherProperty, ComparisonKind kind)
            where TValue : struct, IComparable<TValue>
        {
            var other = OtherProperty.Of(otherProperty);

            return builder.Add(context =>
            {
                if (context.Value is { } actual && other.Read(context.Instance) is { } expected &&
                    !Comparison.IsSatisfied(actual.CompareTo(expected), kind))
                {
                    other.AddError(context, kind);
                }
            });
        }
    }
}