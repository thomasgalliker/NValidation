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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero.</exception>
        public static PropertyRuleBuilder<T, decimal> MultipleOf<T>(this PropertyRuleBuilder<T, decimal> builder, decimal step)
        {
            RequireNonZeroStep(step);

            return builder.Add(context =>
            {
                if (context.Value % step != 0m)
                {
                    context.AddError(ValidationMessageKeys.MultipleOf, (ValidationMessagePlaceholders.Step, step));
                }
            });
        }

        /// <summary>
        /// The form for a property which may be absent. A missing value passes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero.</exception>
        public static PropertyRuleBuilder<T, decimal?> MultipleOf<T>(this PropertyRuleBuilder<T, decimal?> builder, decimal step)
        {
            RequireNonZeroStep(step);

            return builder.Add(context =>
            {
                if (context.Value is { } value && value % step != 0m)
                {
                    context.AddError(ValidationMessageKeys.MultipleOf, (ValidationMessagePlaceholders.Step, step));
                }
            });
        }

        /// <summary>
        /// The whole-number form, e.g. a quantity which may only be ordered by the dozen.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> is zero.</exception>
        public static PropertyRuleBuilder<T, int> MultipleOf<T>(this PropertyRuleBuilder<T, int> builder, int step)
        {
            RequireNonZeroStep(step);

            return builder.Add(context =>
            {
                if (context.Value % step != 0)
                {
                    context.AddError(ValidationMessageKeys.MultipleOf, (ValidationMessagePlaceholders.Step, step));
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
                    context.AddError(ValidationMessageKeys.NotNaN);
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
                    context.AddError(ValidationMessageKeys.NotNaN);
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
                    context.AddError(ValidationMessageKeys.NotNaN);
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
                    context.AddError(ValidationMessageKeys.NotNaN);
                }
            });
        }

        private static void RequireNonZeroStep(decimal step)
        {
            if (step == 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(step), step, "The step must not be zero.");
            }
        }

        private static void RequireNonZeroStep(int step)
        {
            if (step == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(step), step, "The step must not be zero.");
            }
        }
    }
}
