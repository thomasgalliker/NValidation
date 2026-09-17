using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Text.RegularExpressions;

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
        public static PropertyRuleBuilder<T, string?> Matches<T>(this PropertyRuleBuilder<T, string?> builder, string pattern)
        {
            return builder.Matches(pattern, RegexOptions.None);
        }

        /// <inheritdoc cref="Matches{T}(PropertyRuleBuilder{T, string}, string)"/>
        public static PropertyRuleBuilder<T, string?> Matches<T>(this PropertyRuleBuilder<T, string?> builder, string pattern, RegexOptions options)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            return builder.Matches(new Regex(pattern, options, MatchTimeout));
        }

        /// <summary>
        /// Requires the value to be one mail address and nothing else. A missing or blank value
        /// passes; use <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.EmailAddress"/>.
        /// Parsed by <see cref="MailAddress"/> rather than matched against a pattern, which also cannot be
        /// made to backtrack by a hostile value. The value has to be the address <em>alone</em>:
        /// <see cref="MailAddress"/> parses the header forms too, so <c>Foo &lt;a@b.com&gt;</c>,
        /// <c>a@b.com, c@d.com</c> and a value with surrounding whitespace all parse, and each is something
        /// other than the single address the field asked for.
        /// </remarks>
        public static PropertyRuleBuilder<T, string?> EmailAddress<T>(this PropertyRuleBuilder<T, string?> builder)
        {
            return builder.Add(context =>
            {
                if (!string.IsNullOrWhiteSpace(context.Value) && !IsBareAddress(context.Value))
                {
                    context.AddError(ValidationErrorCodes.EmailAddress);
                }
            });
        }

        /// <summary>
        /// Requires a mail address to sit under one of <paramref name="topLevelDomains"/>, e.g. the
        /// domains a tenant is allowed to invite from. Entries are compared without regard to case, and may
        /// be written with or without their leading dot.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.EmailTopLevelDomain"/> with
        /// <see cref="ValidationMessagePlaceholders.TopLevelDomains"/>.
        /// Declare it after <see cref="EmailAddress{T}"/>: this rule asks which domain the address is under,
        /// and a value that is not an address has no answer, so it passes here and is reported by the rule
        /// whose question it is. An address whose host carries no top-level domain — a bare host name, or an
        /// IP literal — is not under any of them and is reported.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="topLevelDomains"/> is empty, or names a blank entry.</exception>
        public static PropertyRuleBuilder<T, string?> EmailTopLevelDomainIn<T>(this PropertyRuleBuilder<T, string?> builder, params string[] topLevelDomains)
        {
            var allowed = ToTopLevelDomains(topLevelDomains, nameof(topLevelDomains));

            return builder.Add(context =>
            {
                if (context.Value is not { } value || !TryGetBareAddress(value, out var address))
                {
                    return;
                }

                if (TopLevelDomainOf(address) is not { } topLevelDomain || !allowed.Contains(topLevelDomain))
                {
                    context.AddError(
                        ValidationErrorCodes.EmailTopLevelDomain,
                        (ValidationMessagePlaceholders.TopLevelDomains, string.Join(", ", allowed)));
                }
            });
        }

        /// <summary>
        /// Refuses a mail address under any of <paramref name="topLevelDomains"/> — the throwaway
        /// domains a signup form will not take, typically. Entries are compared without regard to case, and
        /// may be written with or without their leading dot.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.EmailTopLevelDomainNotAllowed"/> with
        /// <see cref="ValidationMessagePlaceholders.TopLevelDomain"/>.
        /// Declare it after <see cref="EmailAddress{T}"/>, for the same reason as its counterpart: a value
        /// that is not an address has no domain to judge and passes here.
        /// </remarks>
        /// <inheritdoc cref="EmailTopLevelDomainIn{T}" path="/exception"/>
        public static PropertyRuleBuilder<T, string?> EmailTopLevelDomainNotIn<T>(this PropertyRuleBuilder<T, string?> builder, params string[] topLevelDomains)
        {
            var refused = ToTopLevelDomains(topLevelDomains, nameof(topLevelDomains));

            return builder.Add(context =>
            {
                if (context.Value is not { } value || !TryGetBareAddress(value, out var address))
                {
                    return;
                }

                if (TopLevelDomainOf(address) is { } topLevelDomain && refused.Contains(topLevelDomain))
                {
                    context.AddError(
                        ValidationErrorCodes.EmailTopLevelDomainNotAllowed,
                        (ValidationMessagePlaceholders.TopLevelDomain, topLevelDomain));
                }
            });
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
            var refused = RequireTerms(values, nameof(values));

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

        /// <summary>
        /// Whether the value is one mail address and nothing else. Decided over the characters for the
        /// everyday shape, because <c>MailAddress</c> also parses the header forms.
        /// </summary>
        private static bool IsBareAddress(string value)
        {
            return IsOrdinaryAddress(value) || TryGetBareAddress(value, out _);
        }

        private static bool IsOrdinaryAddress(ReadOnlySpan<char> value)
        {
            var at = value.IndexOf('@');

            if (at <= 0 || at == value.Length - 1)
            {
                return false;
            }

            return IsDotAtom(value[..at]) && IsDottedHost(value[(at + 1)..]);
        }

        private static bool IsDotAtom(ReadOnlySpan<char> local)
        {
            if (local[0] == '.' || local[^1] == '.')
            {
                return false;
            }

            var previousWasDot = false;

            foreach (var character in local)
            {
                if (character == '.')
                {
                    if (previousWasDot)
                    {
                        return false;
                    }

                    previousWasDot = true;
                    continue;
                }

                // Deliberately narrower than a dot-atom is allowed to be. The rest of the legal
                // punctuation is rare enough that parsing it is cheaper than describing it here, and a
                // character this does not know is deferred rather than refused.
                if (!char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-' or '+'))
                {
                    return false;
                }

                previousWasDot = false;
            }

            return true;
        }

        private static bool IsDottedHost(ReadOnlySpan<char> host)
        {
            var labelLength = 0;
            var dots = 0;
            var previous = '\0';

            foreach (var character in host)
            {
                if (character == '.')
                {
                    if (labelLength == 0 || previous == '-')
                    {
                        return false;
                    }

                    dots++;
                    labelLength = 0;
                    previous = character;
                    continue;
                }

                if (labelLength == 0 && character == '-')
                {
                    return false;
                }

                if (!char.IsAsciiLetterOrDigit(character) && character != '-')
                {
                    return false;
                }

                labelLength++;
                previous = character;
            }

            return dots > 0 && labelLength > 0 && previous != '-';
        }

        private static bool TryGetBareAddress(string value, [NotNullWhen(true)] out MailAddress? address)
        {
            if (MailAddress.TryCreate(value, out var parsed) && string.Equals(parsed.Address, value, StringComparison.Ordinal))
            {
                address = parsed;
                return true;
            }

            address = null;
            return false;
        }

        private static string? TopLevelDomainOf(MailAddress address)
        {
            var host = address.Host;

            if (host.StartsWith('['))
            {
                return null;
            }

            var lastDot = host.LastIndexOf('.');

            return lastDot >= 0 && lastDot < host.Length - 1 ? host[(lastDot + 1)..] : null;
        }

        private static FrozenSet<string> ToTopLevelDomains(string[] topLevelDomains, string parameterName)
        {
            var terms = RequireTerms(topLevelDomains, parameterName);
            var normalized = new HashSet<string>(terms.Length, StringComparer.OrdinalIgnoreCase);

            foreach (var topLevelDomain in terms)
            {
                normalized.Add(topLevelDomain.TrimStart('.'));
            }

            return normalized.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string[] RequireTerms(string[] values, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(values, parameterName);

            if (values.Length == 0)
            {
                throw new ArgumentException("Name at least one entry; a rule with nothing to look for judges nothing.", parameterName);
            }

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("A blank entry matches everything, which is never what was meant.", parameterName);
                }
            }

            return values;
        }
    }
}
