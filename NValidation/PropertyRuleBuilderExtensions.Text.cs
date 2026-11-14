using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NValidation.Internals;

namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// How long a caller-supplied pattern may run before it is abandoned. A pattern is data as much as a
        /// value is, and a pathological one must not be able to occupy a request indefinitely.
        /// </summary>
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Requires at least <paramref name="minimumLength"/> characters. A missing value passes;
        /// use <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.MinimumLength"/> with
        /// <see cref="ValidationMessagePlaceholders.MinLength"/>.
        /// The text is measured as it arrived: surrounding whitespace counts, and rejecting a blank value is
        /// <c>NotEmpty</c>'s job.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLength"/> is negative.</exception>
        public static PropertyRuleBuilder<T, string?> MinimumLength<T>(this PropertyRuleBuilder<T, string?> builder, int minimumLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);

            return builder.Add(context =>
            {
                if (context.Value != null && context.Value.Length < minimumLength)
                {
                    context.AddError(ValidationErrorCodes.MinimumLength, (ValidationMessagePlaceholders.MinLength, minimumLength));
                }
            });
        }

        /// <summary>
        /// Caps the number of characters, typically to whatever the column behind it holds. A
        /// missing value passes.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.MaximumLength"/> with
        /// <see cref="ValidationMessagePlaceholders.MaxLength"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maximumLength"/> is negative.</exception>
        public static PropertyRuleBuilder<T, string?> MaximumLength<T>(this PropertyRuleBuilder<T, string?> builder, int maximumLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maximumLength);

            return builder.Add(context =>
            {
                if (context.Value != null && context.Value.Length > maximumLength)
                {
                    context.AddError(ValidationErrorCodes.MaximumLength, (ValidationMessagePlaceholders.MaxLength, maximumLength));
                }
            });
        }

        /// <summary>
        /// Requires an exact number of characters, e.g. an ISO currency code. A missing value
        /// passes.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.Length"/> with
        /// <see cref="ValidationMessagePlaceholders.Length"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public static PropertyRuleBuilder<T, string?> Length<T>(this PropertyRuleBuilder<T, string?> builder, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            return builder.Add(context =>
            {
                if (context.Value != null && context.Value.Length != length)
                {
                    context.AddError(ValidationErrorCodes.Length, (ValidationMessagePlaceholders.Length, length));
                }
            });
        }

        /// <summary>
        /// Requires the number of characters to lie between <paramref name="minimumLength"/> and
        /// <paramref name="maximumLength"/>, both included. A missing value passes.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.LengthBetween"/> with
        /// <see cref="ValidationMessagePlaceholders.MinLength"/> and <see cref="ValidationMessagePlaceholders.MaxLength"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="minimumLength"/> is negative, or greater than <paramref name="maximumLength"/>.
        /// </exception>
        public static PropertyRuleBuilder<T, string?> Length<T>(this PropertyRuleBuilder<T, string?> builder, int minimumLength, int maximumLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(minimumLength);

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
                        ValidationErrorCodes.LengthBetween,
                        (ValidationMessagePlaceholders.MinLength, minimumLength),
                        (ValidationMessagePlaceholders.MaxLength, maximumLength));
                }
            });
        }

        /// <summary>
        /// Requires the text to match <paramref name="regex"/>. A missing or blank value passes;
        /// use <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.Matches"/> with
        /// <see cref="ValidationMessagePlaceholders.Pattern"/>.
        /// The caller owns the match timeout here: a <see cref="Regex"/> built without one runs under
        /// <see cref="Regex.InfiniteMatchTimeout"/>, and a pattern that backtracks pathologically can then
        /// occupy the request for as long as it likes. Give the instance a timeout, or use the pattern
        /// overload, which applies one. A value the pattern cannot decide within its timeout counts as not
        /// matching.
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
                    context.AddError(ValidationErrorCodes.Matches, (ValidationMessagePlaceholders.Pattern, regex.ToString()));
                }
            });
        }

        /// <summary>
        /// Requires the text to match <paramref name="pattern"/>, which is compiled once when the
        /// rule is declared and carries a match timeout. A missing or blank value passes.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.Matches"/> with
        /// <see cref="ValidationMessagePlaceholders.Pattern"/>.
        /// Pass a <see cref="Regex"/> instead when the pattern is reused across validators, or when it needs
        /// options this overload does not expose.
        /// </remarks>
        public static PropertyRuleBuilder<T, string?> Matches<T>(
            this PropertyRuleBuilder<T, string?> builder, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
        {
            return builder.Matches(pattern, RegexOptions.None);
        }

        /// <inheritdoc cref="Matches{T}(PropertyRuleBuilder{T, string}, string)"/>
        public static PropertyRuleBuilder<T, string?> Matches<T>(
            this PropertyRuleBuilder<T, string?> builder,
            [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern,
            RegexOptions options)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            return builder.Matches(new Regex(pattern, options, MatchTimeout));
        }

        /// <summary>
        /// Requires the value to be one mail address of the everyday shape and nothing else: a dot-separated
        /// local part, one <c>@</c>, then a domain of two or more labels. A missing or blank value passes;
        /// use <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.EmailAddress"/>. The address is read against RFC 5321,
        /// with an internationalized domain checked against IDNA, and never matched to a pattern. The legal
        /// forms this refuses — a quoted local part, an address literal, a domain of a single label — are
        /// admitted by the refinements on the <see cref="EmailAddressRuleBuilder{T}"/> it returns, which also
        /// carries the domain rules; write those before any other rule or decoration. A syntax rule says an
        /// address is well-formed, never that it exists.
        /// </remarks>
        public static EmailAddressRuleBuilder<T> EmailAddress<T>(this PropertyRuleBuilder<T, string?> builder)
        {
            var options = new EmailAddressOptions();

            builder.Add(context =>
            {
                if (string.IsNullOrWhiteSpace(context.Value))
                {
                    return;
                }

                if (!EmailAddresses.TryParse(context.Value, options, out var parts))
                {
                    context.AddError(ValidationErrorCodes.EmailAddress);
                    return;
                }

                if (options.RequiredTopLevelDomains is null && options.RefusedTopLevelDomains is null)
                {
                    return;
                }

                // The domain rules refine this rule rather than being rules of their own, so they are
                // asked in turn over the one parse and the first to object is the failure reported.
                var topLevelDomain = parts.TopLevelDomain;

                if (options.RequiredTopLevelDomains is { } required
                    && (topLevelDomain.IsEmpty || !HostNames.IsOneOf(topLevelDomain, required)))
                {
                    context.AddError(
                        ValidationErrorCodes.EmailTopLevelDomain,
                        (ValidationMessagePlaceholders.TopLevelDomains, options.RequiredTopLevelDomainsText));
                    return;
                }

                if (options.RefusedTopLevelDomains is { } refused
                    && !topLevelDomain.IsEmpty
                    && HostNames.IsOneOf(topLevelDomain, refused))
                {
                    context.AddError(
                        ValidationErrorCodes.EmailTopLevelDomainNotAllowed,
                        (ValidationMessagePlaceholders.TopLevelDomain, topLevelDomain.ToString()));
                }
            });

            return new EmailAddressRuleBuilder<T>(builder, options);
        }

        /// <summary>
        /// Refuses text that contains any of <paramref name="values"/> — a list of terms a
        /// public-facing field will not carry, typically. Compared without regard to case; a missing value
        /// passes.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.NotContaining"/>.
        /// The message does not say which term matched: a blocklist that reports its own entries is one the
        /// next value works around.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty, or names a blank entry.</exception>
        public static PropertyRuleBuilder<T, string?> NotContaining<T>(this PropertyRuleBuilder<T, string?> builder, params string[] values)
        {
            return builder.NotContaining(StringComparison.OrdinalIgnoreCase, values);
        }

        /// <summary>
        /// Refuses text that contains any of <paramref name="values"/>, compared the way
        /// <paramref name="comparison"/> says. A missing value passes.
        /// </summary>
        /// <inheritdoc cref="NotContaining{T}(PropertyRuleBuilder{T, System.String}, System.String[])" path="/remarks"/>
        /// <inheritdoc cref="NotContaining{T}(PropertyRuleBuilder{T, System.String}, System.String[])" path="/exception"/>
        public static PropertyRuleBuilder<T, string?> NotContaining<T>(
            this PropertyRuleBuilder<T, string?> builder,
            StringComparison comparison,
            params string[] values)
        {
            var refused = Terms.Require(values, nameof(values));

            return builder.Add(context =>
            {
                if (context.Value is not { } value)
                {
                    return;
                }

                foreach (var refusedValue in refused)
                {
                    if (value.Contains(refusedValue, comparison))
                    {
                        context.AddError(ValidationErrorCodes.NotContaining);
                        return;
                    }
                }
            });
        }
    }
}
