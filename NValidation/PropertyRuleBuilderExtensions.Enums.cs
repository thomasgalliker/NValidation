using System.Globalization;

namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires the value to be a member of its enum. A client sends a number, and nothing stops it
        /// sending one no member has.
        /// </summary>
        /// <remarks>
        /// For an enum marked <see cref="FlagsAttribute"/> a combination of members is a legitimate
        /// value even though it is not itself a declared member, so such a value is accepted when every
        /// bit it sets belongs to some member.
        /// </remarks>
        public static PropertyRuleBuilder<T, TEnum> IsInEnum<T, TEnum>(this PropertyRuleBuilder<T, TEnum> builder)
            where TEnum : struct, Enum
        {
            return builder.Add(context =>
            {
                if (!EnumInfo<TEnum>.IsValid(context.Value))
                {
                    context.AddError(ValidationMessageKeys.IsInEnum);
                }
            });
        }

        /// <summary>
        /// What counts as a valid value of <typeparamref name="TEnum"/>, worked out once per enum rather
        /// than on every validation.
        /// </summary>
        private static class EnumInfo<TEnum>
            where TEnum : struct, Enum
        {
            private static readonly bool HasFlags = typeof(TEnum).IsDefined(typeof(FlagsAttribute), false);

            private static readonly Type UnderlyingType = Enum.GetUnderlyingType(typeof(TEnum));

            private static readonly ulong DefinedBits = GetDefinedBits();

            public static bool IsValid(TEnum value)
            {
                if (Enum.IsDefined(value))
                {
                    return true;
                }

                // A combination of declared flags sets no bit of its own.
                return HasFlags && (ToBits(value) & ~DefinedBits) == 0;
            }

            private static ulong GetDefinedBits()
            {
                var definedBits = 0UL;

                foreach (var member in Enum.GetValues<TEnum>())
                {
                    definedBits |= ToBits(member);
                }

                return definedBits;
            }

            private static ulong ToBits(TEnum value)
            {
                // Reinterpreted rather than converted: a negative member has to keep the bits it set,
                // and Convert.ToUInt64 rejects a negative value outright instead of reinterpreting it.
                // Unsigned underlying types are read on their own terms, because a value above
                // long.MaxValue does not survive the trip through Int64 either.
                return UnderlyingType == typeof(ulong)
                    ? Convert.ToUInt64(value, CultureInfo.InvariantCulture)
                    : unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture));
            }
        }
    }
}
