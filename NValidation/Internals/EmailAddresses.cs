using System.Text;

namespace NValidation.Internals
{
    // The grammar EmailAddress() reads: RFC 5321 §4.1.2 Mailbox, with UTF-8 where RFC 6531 admits it,
    // narrowed to the everyday shape unless the options widen it. One pass over the span, allocating
    // nothing — except where the .NET APIs consulted for an internationalized domain and an IPv6 literal
    // offer no allocation-free way in. What a domain is lives in HostNames, shared with Url().
    internal static class EmailAddresses
    {
        // RFC 5321 §4.5.3.1: a local part is at most 64 octets and a domain at most 255. A character is at
        // least one octet, so a part with more characters than that is over before it is read.
        private const int MaximumLocalPartOctets = 64;
        private const int MaximumAddressLength = MaximumLocalPartOctets + 1 + HostNames.MaximumHostOctets;

        private const string IPv6Tag = "IPv6:";

        // atext, RFC 5322 §3.2.3 — the letters, the digits and !#$%&'*+-/=?^_`{|}~ — as one bit per ASCII
        // code, so that asking is one load rather than a run of comparisons.
        private static ReadOnlySpan<byte> AtextBits =>
            [0x00, 0x00, 0x00, 0x00, 0xFA, 0xAC, 0xFF, 0xA3, 0xFE, 0xFF, 0xFF, 0xC7, 0xFF, 0xFF, 0xFF, 0x7F];

        public static bool TryParse(ReadOnlySpan<char> value, EmailAddressOptions options, out EmailAddressParts parts)
        {
            parts = default;

            if (value.IsEmpty || value.Length > MaximumAddressLength || (value[0] == '"' && !options.AllowQuotedLocalPart))
            {
                return false;
            }

            // A quoted local part may itself contain '@', so it is read to its closing quote first rather
            // than the first '@' being taken.
            var at = value[0] == '"' ? IndexAfterQuotedString(value) : value.IndexOf('@');

            if (at <= 0 || at >= value.Length - 1 || value[at] != '@' || !IsLocalPart(value[..at]))
            {
                return false;
            }

            var domain = value[(at + 1)..];

            if (domain[0] == '[')
            {
                if (!options.AllowAddressLiteral || !IsAddressLiteral(domain))
                {
                    return false;
                }

                parts = new EmailAddressParts(domain, isAddressLiteral: true);

                return true;
            }

            if (!HostNames.IsName(domain, options.AllowSingleLabelDomain))
            {
                return false;
            }

            parts = new EmailAddressParts(domain, isAddressLiteral: false);

            return true;
        }

        // Quoted-string = DQUOTE *(qtextSMTP / quoted-pairSMTP) DQUOTE, RFC 5321 §4.1.2. The index just past
        // the closing quote, or -1 for a value that does not start with a quoted string.
        private static int IndexAfterQuotedString(ReadOnlySpan<char> value)
        {
            var escaped = false;

            for (var i = 1; i < value.Length; i++)
            {
                var character = value[i];

                if (escaped)
                {
                    // quoted-pairSMTP = "\" %d32-126
                    if (!IsPrintableAscii(character))
                    {
                        return -1;
                    }

                    escaped = false;
                }
                else if (character == '"')
                {
                    return i + 1;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character < 0x80 && !IsPrintableAscii(character))
                {
                    // qtextSMTP = %d32-33 / %d35-91 / %d93-126: printable ASCII but for the two above.
                    return -1;
                }
            }

            return -1;
        }

        // Local-part = Dot-string / Quoted-string, at most 64 octets. A quoted string was read, and checked,
        // when the value was split.
        private static bool IsLocalPart(ReadOnlySpan<char> localPart)
        {
            if (localPart.Length > MaximumLocalPartOctets)
            {
                return false;
            }

            if (localPart[0] == '"')
            {
                return Ascii.IsValid(localPart) || IsTextWithinOctets(localPart, MaximumLocalPartOctets);
            }

            return IsDotString(localPart, out var ascii) && (ascii || IsTextWithinOctets(localPart, MaximumLocalPartOctets));
        }

        // The rare path for a part beyond ASCII: well-formed — a stray half of a surrogate pair is not a
        // character at all — and within its octet limit once encoded.
        private static bool IsTextWithinOctets(ReadOnlySpan<char> part, int maximumOctets)
        {
            return HostNames.IsWellFormed(part) && Encoding.UTF8.GetByteCount(part) <= maximumOctets;
        }

        // Dot-string = Atom *("." Atom), Atom = 1*atext: no dot at either end and none doubled. Anything
        // beyond ASCII is taken as text here and checked to be well-formed afterwards, so that the loop
        // over the everyday address, which is all ASCII, does nothing but the structure.
        private static bool IsDotString(ReadOnlySpan<char> localPart, out bool ascii)
        {
            ascii = true;

            if (localPart[0] == '.' || localPart[^1] == '.')
            {
                return false;
            }

            var previousWasDot = false;

            foreach (var character in localPart)
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

                if (character < 0x80)
                {
                    if (!IsAtext(character))
                    {
                        return false;
                    }
                }
                else
                {
                    ascii = false;
                }

                previousWasDot = false;
            }

            return true;
        }

        private static bool IsAtext(char character)
        {
            return character < 0x80 && (AtextBits[character >> 3] & (1 << (character & 7))) != 0;
        }

        private static bool IsPrintableAscii(char character)
        {
            return character is >= ' ' and <= '~';
        }

        // address-literal = "[" ( IPv4-address-literal / IPv6-address-literal ) "]", RFC 5321 §4.1.3. The
        // General-address-literal form takes a tag from a registry nothing has been added to; it is not read.
        private static bool IsAddressLiteral(ReadOnlySpan<char> domain)
        {
            if (domain.Length < 2 || domain[^1] != ']')
            {
                return false;
            }

            var literal = domain[1..^1];

            return literal.StartsWith(IPv6Tag, StringComparison.OrdinalIgnoreCase)
                ? HostNames.TryParseIPv6(literal[IPv6Tag.Length..], out _)
                : HostNames.IsIPv4Address(literal);
        }
    }
}
