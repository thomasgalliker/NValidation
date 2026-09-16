namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires an exact multiple of <paramref name="step"/>, e.g. a price in five-cent increments.
        /// </summary>
        /// <remarks>
        /// Decimal arithmetic is exact for these values, so the remainder needs no epsilon.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero or negative.</exception>
        public static PropertyRuleBuilder<T, decimal> MultipleOf<T>(this PropertyRuleBuilder<T, decimal> builder, decimal step)
        {
            RequirePositiveStep(step);

            return builder.Add(context =>
            {
                if (context.Value % step != 0m)
                {
                    context.AddError(ValidationErrorCodes.MultipleOf, (ValidationMessagePlaceholders.Step, step));
                }
            });
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero or negative.</exception>
        public static PropertyRuleBuilder<T, decimal?> MultipleOf<T>(this PropertyRuleBuilder<T, decimal?> builder, decimal step)
        {
            RequirePositiveStep(step);

            return builder.Add(context =>
            {
                if (context.Value is { } value && value % step != 0m)
                {
                    context.AddError(ValidationErrorCodes.MultipleOf, (ValidationMessagePlaceholders.Step, step));
                }
            });
        }

        /// <summary>
        /// The whole-number form, e.g. a quantity which may only be ordered by the dozen.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero or negative.</exception>
        public static PropertyRuleBuilder<T, int> MultipleOf<T>(this PropertyRuleBuilder<T, int> builder, int step)
        {
            RequirePositiveStep(step);

            return builder.Add(context =>
            {
                if (context.Value % step != 0)
                {
                    context.AddError(ValidationErrorCodes.MultipleOf, (ValidationMessagePlaceholders.Step, step));
                }
            });
        }

        /// <summary>
        /// The form for a whole number which may be absent. A missing value passes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero or negative.</exception>
        public static PropertyRuleBuilder<T, int?> MultipleOf<T>(this PropertyRuleBuilder<T, int?> builder, int step)
        {
            RequirePositiveStep(step);

            return builder.Add(context =>
            {
                if (context.Value is { } value && value % step != 0)
                {
                    context.AddError(ValidationErrorCodes.MultipleOf, (ValidationMessagePlaceholders.Step, step));
                }
            });
        }

        /// <summary>
        /// Caps how many digits a number carries: at most <paramref name="precision"/> in total, of
        /// which at most <paramref name="scale"/> follow the decimal point — typically the shape of the
        /// column behind it.
        /// </summary>
        /// <remarks>
        /// Trailing zeros are representation rather than value: <c>1.50m</c> and <c>1.5m</c> are the
        /// same number, and the column accepts both. They are normalised away, so there is one
        /// behaviour rather than a flag to choose between two.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="precision"/> is zero or negative, <paramref name="scale"/> is negative, or
        /// <paramref name="scale"/> is greater than <paramref name="precision"/>.
        /// </exception>
        public static PropertyRuleBuilder<T, decimal> PrecisionScale<T>(this PropertyRuleBuilder<T, decimal> builder, int precision, int scale)
        {
            RequirePrecisionAndScale(precision, scale);

            return builder.Add(context =>
            {
                if (Exceeds(context.Value, precision, scale))
                {
                    AddPrecisionScaleError(context, precision, scale);
                }
            });
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="PrecisionScale{T}(PropertyRuleBuilder{T, decimal}, int, int)" path="/remarks"/>
        /// <inheritdoc cref="PrecisionScale{T}(PropertyRuleBuilder{T, decimal}, int, int)" path="/exception"/>
        public static PropertyRuleBuilder<T, decimal?> PrecisionScale<T>(this PropertyRuleBuilder<T, decimal?> builder, int precision, int scale)
        {
            RequirePrecisionAndScale(precision, scale);

            return builder.Add(context =>
            {
                if (context.Value is { } value && Exceeds(value, precision, scale))
                {
                    AddPrecisionScaleError(context, precision, scale);
                }
            });
        }

        /// <summary>
        /// Requires a real number. <see cref="double.NaN"/> is what a measurement carries when it was
        /// never taken, so it counts as missing rather than as a value out of range — a comparison
        /// against <see cref="double.NaN"/> is false either way and would let it pass a range rule.
        /// </summary>
        public static PropertyRuleBuilder<T, double> NotNaN<T>(this PropertyRuleBuilder<T, double> builder)
        {
            return builder.Add(context =>
            {
                if (double.IsNaN(context.Value))
                {
                    context.AddError(ValidationErrorCodes.NotNaN);
                }
            });
        }

        /// <inheritdoc cref="NotNaN{T}(PropertyRuleBuilder{T, double})"/>
        public static PropertyRuleBuilder<T, double?> NotNaN<T>(this PropertyRuleBuilder<T, double?> builder)
        {
            return builder.Add(context =>
            {
                if (context.Value is { } value && double.IsNaN(value))
                {
                    context.AddError(ValidationErrorCodes.NotNaN);
                }
            });
        }

        /// <inheritdoc cref="NotNaN{T}(PropertyRuleBuilder{T, double})"/>
        public static PropertyRuleBuilder<T, float> NotNaN<T>(this PropertyRuleBuilder<T, float> builder)
        {
            return builder.Add(context =>
            {
                if (float.IsNaN(context.Value))
                {
                    context.AddError(ValidationErrorCodes.NotNaN);
                }
            });
        }

        /// <inheritdoc cref="NotNaN{T}(PropertyRuleBuilder{T, double})"/>
        public static PropertyRuleBuilder<T, float?> NotNaN<T>(this PropertyRuleBuilder<T, float?> builder)
        {
            return builder.Add(context =>
            {
                if (context.Value is { } value && float.IsNaN(value))
                {
                    context.AddError(ValidationErrorCodes.NotNaN);
                }
            });
        }

        /// <summary>
        /// Whether the value needs more digits than the contract allows.
        /// </summary>
        private static bool Exceeds(decimal value, int precision, int scale)
        {
            // Dividing by one at full scale is the documented way to drop trailing zeros: 1.50m becomes
            // 1.5m, so the rule judges the number rather than how it happened to be written.
            var normalized = value / 1.000000000000000000000000000000000m;

            var actualScale = (decimal.GetBits(normalized)[3] >> 16) & 0xFF;

            return actualScale > scale || DigitsBeforeThePoint(normalized) > precision - scale;
        }

        /// <remarks>
        /// Zero has none, so <c>0.5m</c> against <c>PrecisionScale(2, 2)</c> passes.
        /// </remarks>
        private static int DigitsBeforeThePoint(decimal value)
        {
            var whole = decimal.Truncate(Math.Abs(value));
            var digits = 0;

            while (whole >= 1m)
            {
                whole = decimal.Truncate(whole / 10m);
                digits++;
            }

            return digits;
        }

        private static void AddPrecisionScaleError<T, TProperty>(RuleContext<T, TProperty> context, int precision, int scale)
        {
            context.AddError(
                ValidationErrorCodes.PrecisionScale,
                (ValidationMessagePlaceholders.Precision, precision),
                (ValidationMessagePlaceholders.Scale, scale));
        }

        private static void RequirePrecisionAndScale(int precision, int scale)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(precision);
            ArgumentOutOfRangeException.ThrowIfNegative(scale);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(scale, precision);
        }

        /// <remarks>
        /// A negative step tests for the same multiples its absolute value does, so allowing one would
        /// only add a way of writing the rule that reads as if it meant something else. Rejecting it
        /// also keeps the remainder away from <c>int.MinValue % -1</c>, which the ECMA spec leaves free
        /// to throw.
        /// </remarks>
        private static void RequirePositiveStep(decimal step)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        }

        /// <inheritdoc cref="RequirePositiveStep(decimal)"/>
        private static void RequirePositiveStep(int step)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        }
    }
}
