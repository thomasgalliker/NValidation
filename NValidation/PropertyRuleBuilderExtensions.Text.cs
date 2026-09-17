using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
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
        /// Caps the number of characters, typically to whatever the column behind it holds. A missing
        /// value passes.
        /// </summary>
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
        /// Requires an exact number of characters, e.g. an ISO currency code.
        /// </summary>
        /// <inheritdoc cref="MinimumLength{T}" path="/remarks"/>
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
        /// <paramref name="maximumLength"/>, both included.
        /// </summary>
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
                    context.AddError(ValidationErrorCodes.Matches, (ValidationMessagePlaceholders.Pattern, regex.ToString()));
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
        /// Requires the value to be one mail address and nothing else. A missing or blank value passes;
        /// use <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <remarks>
        /// Parsed by <see cref="MailAddress"/> rather than matched against a pattern. The address forms
        /// that are legal are far broader than a hand-written pattern allows — a quoted local part, an
        /// IP literal, an internationalized domain — and a pattern wide enough to admit them is one
        /// nobody can read, let alone review. A parser also cannot be made to backtrack by a hostile
        /// value.
        /// <para>
        /// The value has to be the address <em>alone</em>. <see cref="MailAddress"/> parses the header
        /// forms too, so <c>Foo &lt;a@b.com&gt;</c>, <c>a@b.com, c@d.com</c> and a value with
        /// surrounding whitespace all parse — and each of them is something other than the single
        /// address the field asked for. What a host does with such a value later, in a mail header or a
        /// recipient list, is not this rule's to assume, so they are rejected here.
        /// </para>
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
        /// domains a tenant is allowed to invite from. Entries are compared without regard to case, and
        /// may be written with or without their leading dot.
        /// </summary>
        /// <remarks>
        /// Declare it after <see cref="EmailAddress{T}"/>: this rule asks which domain the address is
        /// under, and a value that is not an address has no answer, so it passes here and is reported by
        /// the rule whose question it is. An address whose host carries no top-level domain — a bare
        /// host name, or an IP literal — is not under any of them and is reported.
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
        /// The inverse: refuses a mail address under any of <paramref name="topLevelDomains"/> — the
        /// throwaway domains a signup form will not take, typically.
        /// </summary>
        /// <inheritdoc cref="EmailTopLevelDomainIn{T}" path="/remarks"/>
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
        /// Refuses text that contains any of <paramref name="values"/> — a list of terms a public-facing
        /// field will not carry, typically. Compared without regard to case; a missing value passes.
        /// </summary>
        /// <remarks>
        /// The message does not say which term matched. A blocklist that reports its own entries is a
        /// blocklist the next value works around, and the reader is being told to write something else
        /// rather than to guess a word.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty, or names a blank entry.</exception>
        public static PropertyRuleBuilder<T, string?> NotContaining<T>(this PropertyRuleBuilder<T, string?> builder, params string[] values)
        {
            return builder.NotContaining(StringComparison.OrdinalIgnoreCase, values);
        }

        /// <summary>
        /// The same, comparing the way <paramref name="comparison"/> says.
        /// </summary>
        /// <inheritdoc cref="NotContaining{T}(PropertyRuleBuilder{T, string}, string[])" path="/remarks"/>
        /// <inheritdoc cref="NotContaining{T}(PropertyRuleBuilder{T, string}, string[])" path="/exception"/>
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
        /// Whether the value is one mail address and nothing else.
        /// </summary>
        /// <remarks>
        /// The everyday shape is decided over the characters themselves, because
        /// <see cref="MailAddress"/> allocates the parsed address and its parts, and a value the rule
        /// accepts should not pay for producing something nobody goes on to read. The span pass only
        /// ever answers yes: everything it is not certain about — a quoted local part, an address
        /// literal, an internationalized domain, a host without a dot — falls through to the parser,
        /// which is what decides those. A rule which needs the parsed address itself, rather than a
        /// verdict about it, still asks <see cref="TryGetBareAddress"/>.
        /// </remarks>
        private static bool IsBareAddress(string value)
        {
            return IsOrdinaryAddress(value) || TryGetBareAddress(value, out _);
        }

        /// <summary>
        /// Whether the value is an address of the everyday shape — a dot-atom local part of unreserved
        /// ASCII, one <c>@</c>, then a dotted host of letters, digits and hyphens — which
        /// <see cref="MailAddress"/> parses to exactly itself, so accepting it here and accepting it
        /// there are the same answer.
        /// </summary>
        /// <remarks>
        /// <c>false</c> means "not decided here", never "not an address".
        /// </remarks>
        private static bool IsOrdinaryAddress(ReadOnlySpan<char> value)
        {
            var at = value.IndexOf('@');

            if (at <= 0 || at == value.Length - 1)
            {
                return false;
            }

            return IsDotAtom(value[..at]) && IsDottedHost(value[(at + 1)..]);
        }

        /// <summary>
        /// A local part of unreserved ASCII, with dots between its atoms rather than at either end or
        /// doubled.
        /// </summary>
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

        /// <summary>
        /// A host of dot-separated labels of letters, digits and hyphens, each label non-empty and
        /// neither starting nor ending with a hyphen. A host carrying no dot is legal and left to the
        /// parser.
        /// </summary>
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

        /// <summary>
        /// The value read as one mail address and nothing else.
        /// </summary>
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

        /// <summary>
        /// The last label of the address's host, or <c>null</c> where there is none to take: a host with
        /// no dot in it, or an IP literal, which is written in brackets and is not a domain at all.
        /// </summary>
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
