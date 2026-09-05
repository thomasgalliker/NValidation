using System.Net.Mail;
using System.Text.RegularExpressions;

namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// How long a caller-supplied pattern may run before it is abandoned. A pattern is data as much
        /// as a value is, and a pathological one must not be able to occupy a request indefinitely.
        /// </summary>
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Requires at least <paramref name="minimumLength"/> characters. A missing value passes; use
        /// <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <remarks>
        /// The text is measured as it arrived. Surrounding whitespace counts, and rejecting a blank
        /// value is <c>NotEmpty</c>'s job, not this rule's.
        /// </remarks>
        public static PropertyRuleBuilder<T, string?> MinimumLength<T>(this PropertyRuleBuilder<T, string?> builder, int minimumLength)
        {
            return builder.Add(context =>
            {
                if (context.Value != null && context.Value.Length < minimumLength)
                {
                    context.AddError(ValidationMessageKeys.MinimumLength, (ValidationMessagePlaceholders.MinLength, minimumLength));
                }
            });
        }

        /// <summary>
        /// Caps the number of characters, typically to whatever the column behind it holds. A missing
        /// value passes.
        /// </summary>
        public static PropertyRuleBuilder<T, string?> MaximumLength<T>(this PropertyRuleBuilder<T, string?> builder, int maximumLength)
        {
            return builder.Add(context =>
            {
                if (context.Value != null && context.Value.Length > maximumLength)
                {
                    context.AddError(ValidationMessageKeys.MaximumLength, (ValidationMessagePlaceholders.MaxLength, maximumLength));
                }
            });
        }

        /// <summary>
        /// Requires an exact number of characters, e.g. an ISO currency code.
        /// </summary>
        /// <inheritdoc cref="MinimumLength{T}" path="/remarks"/>
        public static PropertyRuleBuilder<T, string?> Length<T>(this PropertyRuleBuilder<T, string?> builder, int length)
        {
            return builder.Add(context =>
            {
                if (context.Value != null && context.Value.Length != length)
                {
                    context.AddError(ValidationMessageKeys.Length, (ValidationMessagePlaceholders.Length, length));
                }
            });
        }

        /// <summary>
        /// Requires the number of characters to lie between <paramref name="minimumLength"/> and
        /// <paramref name="maximumLength"/>, both included.
        /// </summary>
        public static PropertyRuleBuilder<T, string?> Length<T>(this PropertyRuleBuilder<T, string?> builder, int minimumLength, int maximumLength)
        {
            if (minimumLength > maximumLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumLength),
                    minimumLength,
                    $"The minimum length must not be greater than the maximum length of {maximumLength}.");
            }

            return builder.Add(context =>
            {
                if (context.Value != null && (context.Value.Length < minimumLength || context.Value.Length > maximumLength))
                {
                    context.AddError(
                        ValidationMessageKeys.LengthBetween,
                        (ValidationMessagePlaceholders.MinLength, minimumLength),
                        (ValidationMessagePlaceholders.MaxLength, maximumLength));
                }
            });
        }

        /// <summary>
        /// Requires the text to match <paramref name="regex"/>. A missing or blank value passes; use
        /// <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <remarks>
        /// The caller owns the match timeout here: a <see cref="Regex"/> built without one runs under
        /// <see cref="Regex.InfiniteMatchTimeout"/>, and a pattern that backtracks pathologically can
        /// then occupy the request for as long as it likes. Give the instance a timeout, or use the
        /// pattern overload, which applies one. A value the pattern cannot decide within its timeout
        /// counts as not matching.
        /// </remarks>
        public static PropertyRuleBuilder<T, string?> Matches<T>(this PropertyRuleBuilder<T, string?> builder, Regex regex)
        {
            ArgumentNullException.ThrowIfNull(regex);

            return builder.Add(context =>
            {
                if (string.IsNullOrWhiteSpace(context.Value))
                {
                    return;
                }

                bool matched;

                try
                {
                    matched = regex.IsMatch(context.Value);
                }
                catch (RegexMatchTimeoutException)
                {
                    // A value the pattern cannot decide in time is a value that does not match. Letting
                    // the exception out would turn a bad payload into a server error, which is the
                    // failure the timeout exists to prevent.
                    matched = false;
                }

                if (!matched)
                {
                    context.AddError(ValidationMessageKeys.Matches, (ValidationMessagePlaceholders.Pattern, regex.ToString()));
                }
            });
        }

        /// <summary>
        /// The same, from a pattern. The pattern is compiled once, when the rule is declared, with a
        /// match timeout.
        /// </summary>
        /// <remarks>
        /// Pass a <see cref="Regex"/> instead when the pattern is reused across validators, or when it
        /// needs options this overload does not expose.
        /// </remarks>
        public static PropertyRuleBuilder<T, string?> Matches<T>(
            this PropertyRuleBuilder<T, string?> builder,
            string pattern,
            RegexOptions options = RegexOptions.None)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            return builder.Matches(new Regex(pattern, options, MatchTimeout));
        }

        /// <summary>
        /// Accepts anything the framework can parse as a mail address. Deliberately permissive: the
        /// address forms that are legal are far broader than most hand-written patterns allow, so a
        /// stricter rule here would reject addresses that actually deliver.
        /// </summary>
        public static PropertyRuleBuilder<T, string?> EmailAddress<T>(this PropertyRuleBuilder<T, string?> builder)
        {
            return builder.Add(context =>
            {
                if (!string.IsNullOrWhiteSpace(context.Value) && !MailAddress.TryCreate(context.Value, out _))
                {
                    context.AddError(ValidationMessageKeys.EmailAddress);
                }
            });
        }
    }
}
