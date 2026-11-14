namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires an exact multiple of <paramref name="step"/>, e.g. a price in five-cent increments.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.MultipleOf"/> with
        /// <see cref="ValidationMessagePlaceholders.Step"/>.
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
        /// Requires an exact multiple of <paramref name="step"/>. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="MultipleOf{T}(PropertyRuleBuilder{T, System.Decimal}, System.Decimal)" path="/remarks"/>
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
        /// Requires an exact multiple of <paramref name="step"/>, e.g. a quantity which may only be
        /// ordered by the dozen.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.MultipleOf"/> with
        /// <see cref="ValidationMessagePlaceholders.Step"/>.
        /// </remarks>
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
        /// Requires an exact multiple of <paramref name="step"/>. A missing value passes.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.MultipleOf"/> with
        /// <see cref="ValidationMessagePlaceholders.Step"/>.
        /// </remarks>
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
        /// Caps how many digits a number carries: at most <paramref name="precision"/> in total,
        /// of which at most <paramref name="scale"/> follow the decimal point — typically the shape of the
        /// column behind it.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.PrecisionScale"/> with
        /// <see cref="ValidationMessagePlaceholders.Precision"/> and <see cref="ValidationMessagePlaceholders.Scale"/>.
        /// Trailing zeros are representation rather than value — <c>1.50m</c> and <c>1.5m</c> are the same
        /// number, and the column accepts both — so they are normalised away rather than left to a flag.
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
        /// Caps how many digits a number carries: at most <paramref name="precision"/> in total,
        /// of which at most <paramref name="scale"/> follow the decimal point. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="PrecisionScale{T}(PropertyRuleBuilder{T, System.Decimal}, System.Int32, System.Int32)" path="/remarks"/>
        /// <inheritdoc cref="PrecisionScale{T}(PropertyRuleBuilder{T, System.Decimal}, System.Int32, System.Int32)" path="/exception"/>
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
        /// Requires a real number. <see cref="double.NaN"/> is what a measurement carries when it
        /// was never taken, so it counts as missing rather than as a value out of range — a comparison
        /// against <see cref="double.NaN"/> is false either way and would let it pass a range rule.
        /// </summary>
        /// <remarks>Reports <see cref="ValidationErrorCodes.NotNaN"/>.</remarks>
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

        /// <inheritdoc cref="NotNaN{T}(PropertyRuleBuilder{T, System.Double})"/>
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

        /// <inheritdoc cref="NotNaN{T}(PropertyRuleBuilder{T, System.Double})"/>
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

        /// <inheritdoc cref="NotNaN{T}(PropertyRuleBuilder{T, System.Double})"/>
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

        private static bool Exceeds(decimal value, int precision, int scale)
        {
            // Dividing by one at full scale is the documented way to drop trailing zeros: 1.50m becomes
            // 1.5m, so the rule judges the number rather than how it happened to be written.
            var normalized = value / 1.000000000000000000000000000000000m;

            var actualScale = (decimal.GetBits(normalized)[3] >> 16) & 0xFF;

            return actualScale > scale || DigitsBeforeThePoint(normalized) > precision - scale;
        }

        /// <summary>
        /// Zero has none, so 0.5m against PrecisionScale(2, 2) passes.
        /// </summary>
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

        /// <summary>
        /// A negative step tests the same multiples its absolute value does, so it is refused rather than
        /// accepted as a second spelling of one rule.
        /// </summary>
        private static void RequirePositiveStep(decimal step)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        }

        private static void RequirePositiveStep(int step)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        }
    }
}
