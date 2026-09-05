namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires the date to lie strictly before now. A missing value passes; use <c>NotEmpty()</c>
        /// to require one.
        /// </summary>
        /// <remarks>
        /// <paramref name="timeProvider"/> is what "now" means: it defaults to
        /// <see cref="TimeProvider.System"/>, and a test passes one to make the rule deterministic.
        /// <para>
        /// The comparison happens in UTC. A <see cref="DateTimeKind.Local"/> value is converted, and a
        /// <see cref="DateTimeKind.Unspecified"/> one — which is what a date deserialized without an
        /// offset carries — is read as UTC rather than as local time, so the verdict does not depend on
        /// the machine's time zone.
        /// </para>
        /// </remarks>
        public static PropertyRuleBuilder<T, DateTime> InThePast<T>(this PropertyRuleBuilder<T, DateTime> builder, TimeProvider? timeProvider = null)
        {
            return builder.Add(context =>
            {
                if (AsUtc(context.Value) >= UtcNow(timeProvider))
                {
                    context.AddError(ValidationMessageKeys.InThePast);
                }
            });
        }

        /// <inheritdoc cref="InThePast{T}(PropertyRuleBuilder{T, DateTime}, TimeProvider)"/>
        public static PropertyRuleBuilder<T, DateTime?> InThePast<T>(this PropertyRuleBuilder<T, DateTime?> builder, TimeProvider? timeProvider = null)
        {
            return builder.Add(context =>
            {
                if (context.Value is { } value && AsUtc(value) >= UtcNow(timeProvider))
                {
                    context.AddError(ValidationMessageKeys.InThePast);
                }
            });
        }

        /// <summary>
        /// The unambiguous form: a <see cref="DateTimeOffset"/> carries its own offset, so there is
        /// nothing to assume about the time zone.
        /// </summary>
        public static PropertyRuleBuilder<T, DateTimeOffset> InThePast<T>(this PropertyRuleBuilder<T, DateTimeOffset> builder, TimeProvider? timeProvider = null)
        {
            return builder.Add(context =>
            {
                if (context.Value.UtcDateTime >= UtcNow(timeProvider))
                {
                    context.AddError(ValidationMessageKeys.InThePast);
                }
            });
        }

        /// <inheritdoc cref="InThePast{T}(PropertyRuleBuilder{T, DateTimeOffset}, TimeProvider)"/>
        public static PropertyRuleBuilder<T, DateTimeOffset?> InThePast<T>(this PropertyRuleBuilder<T, DateTimeOffset?> builder, TimeProvider? timeProvider = null)
        {
            return builder.Add(context =>
            {
                if (context.Value is { } value && value.UtcDateTime >= UtcNow(timeProvider))
                {
                    context.AddError(ValidationMessageKeys.InThePast);
                }
            });
        }

        /// <summary>
        /// Requires the date to lie strictly after now. A missing value passes; use <c>NotEmpty()</c>
        /// to require one.
        /// </summary>
        /// <inheritdoc cref="InThePast{T}(PropertyRuleBuilder{T, DateTime}, TimeProvider)" path="/remarks"/>
        public static PropertyRuleBuilder<T, DateTime> InTheFuture<T>(this PropertyRuleBuilder<T, DateTime> builder, TimeProvider? timeProvider = null)
        {
            return builder.Add(context =>
            {
                if (AsUtc(context.Value) <= UtcNow(timeProvider))
                {
                    context.AddError(ValidationMessageKeys.InTheFuture);
                }
            });
        }

        /// <inheritdoc cref="InTheFuture{T}(PropertyRuleBuilder{T, DateTime}, TimeProvider)"/>
        public static PropertyRuleBuilder<T, DateTime?> InTheFuture<T>(this PropertyRuleBuilder<T, DateTime?> builder, TimeProvider? timeProvider = null)
        {
            return builder.Add(context =>
            {
                if (context.Value is { } value && AsUtc(value) <= UtcNow(timeProvider))
                {
                    context.AddError(ValidationMessageKeys.InTheFuture);
                }
            });
        }

        /// <inheritdoc cref="InThePast{T}(PropertyRuleBuilder{T, DateTimeOffset}, TimeProvider)" path="/summary"/>
        public static PropertyRuleBuilder<T, DateTimeOffset> InTheFuture<T>(this PropertyRuleBuilder<T, DateTimeOffset> builder, TimeProvider? timeProvider = null)
        {
            return builder.Add(context =>
            {
                if (context.Value.UtcDateTime <= UtcNow(timeProvider))
                {
                    context.AddError(ValidationMessageKeys.InTheFuture);
                }
            });
        }

        /// <inheritdoc cref="InTheFuture{T}(PropertyRuleBuilder{T, DateTimeOffset}, TimeProvider)"/>
        public static PropertyRuleBuilder<T, DateTimeOffset?> InTheFuture<T>(this PropertyRuleBuilder<T, DateTimeOffset?> builder, TimeProvider? timeProvider = null)
        {
            return builder.Add(context =>
            {
                if (context.Value is { } value && value.UtcDateTime <= UtcNow(timeProvider))
                {
                    context.AddError(ValidationMessageKeys.InTheFuture);
                }
            });
        }

        private static DateTime UtcNow(TimeProvider? timeProvider)
        {
            return (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        }

        private static DateTime AsUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }
    }
}
