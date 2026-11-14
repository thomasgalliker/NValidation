using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace NValidation.Internals
{
    // What both EmailAddress() and Url() mean by a host: the DNS name grammar of RFC 1035 §2.3.1 with
    // IDNA for a name beyond ASCII, and the two address forms a host may take instead. Shared because two
    // definitions of a valid host name in one library drift, and the day they disagree is the day a value
    // passes one rule and fails the other.
    internal static class HostNames
    {
        // RFC 1035 §2.3.4, and what IdnMapping holds the ASCII form of an internationalized name to.
        public const int MaximumHostOctets = 255;
        public const int MaximumLabelOctets = 63;

        // What a label is made of as one bit per ASCII code, so that asking is one load rather than a run
        // of comparisons: the letters, the digits and the hyphen.
        private static ReadOnlySpan<byte> LabelBits =>
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0xFF, 0x03, 0xFE, 0xFF, 0xFF, 0x07, 0xFE, 0xFF, 0xFF, 0x07];

        // Shared because nothing here changes its settings.
        private static readonly IdnMapping Idn = new();

        // Domain = sub-domain *("." sub-domain); sub-domain = Let-dig [Ldh-str]: labels of letters, digits
        // and hyphens, each beginning and ending with a letter or digit. A label beyond ASCII is a U-label,
        // whose rules IdnMapping applies afterwards; here any character beyond ASCII counts as a letter.
        public static bool IsName(ReadOnlySpan<char> host, bool allowSingleLabel)
        {
            if (host.IsEmpty || host.Length > MaximumHostOctets)
            {
                return false;
            }

            var labels = 1;
            var labelLength = 0;
            var ascii = true;
            var previous = '.';

            foreach (var character in host)
            {
                if (character == '.')
                {
                    if (!EndsLabel(labelLength, ascii, previous))
                    {
                        return false;
                    }

                    labels++;
                    labelLength = 0;
                }
                else if (character < 0x80)
                {
                    if (!IsLabelCharacter(character, labelLength))
                    {
                        return false;
                    }

                    labelLength++;
                }
                else
                {
                    ascii = false;
                    labelLength++;
                }

                previous = character;
            }

            if (!EndsLabel(labelLength, ascii, previous) || (labels < 2 && !allowSingleLabel))
            {
                return false;
            }

            return ascii || (IsWellFormed(host) && IsInternationalizedName(host));
        }

        // The last label of a name, or nothing where there is none to take: a name of a single label.
        public static ReadOnlySpan<char> TopLevelLabelOf(ReadOnlySpan<char> host)
        {
            var lastDot = host.LastIndexOf('.');

            return lastDot < 0 ? default : host[(lastDot + 1)..];
        }

        // Whether a name's last label is all digits, which is what tells an address from a name: no
        // top-level domain is numeric, and RFC 3986 reads a host of that shape as an IPv4 address.
        public static bool EndsInDigits(ReadOnlySpan<char> host)
        {
            var lastDot = host.LastIndexOf('.');
            var label = lastDot < 0 ? host : host[(lastDot + 1)..];

            if (label.IsEmpty)
            {
                return false;
            }

            foreach (var character in label)
            {
                if (!char.IsAsciiDigit(character))
                {
                    return false;
                }
            }

            return true;
        }

        // Whether one of the entries is the text, compared without regard to case. A linear scan over a
        // handful of short strings, which is what such a list is, and nothing allocated.
        public static bool IsOneOf(ReadOnlySpan<char> text, string[] entries)
        {
            foreach (var entry in entries)
            {
                if (text.Equals(entry, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // Whether the host is one of the entries or a label under one. The boundary is checked at a dot,
        // because a suffix match without it would read evil-example.com as sitting under example.com.
        public static bool IsAtOrUnderOneOf(ReadOnlySpan<char> host, string[] entries)
        {
            foreach (var entry in entries)
            {
                if (host.Length == entry.Length)
                {
                    if (host.Equals(entry, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else if (host.Length > entry.Length
                    && host[host.Length - entry.Length - 1] == '.'
                    && host[(host.Length - entry.Length)..].Equals(entry, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // Whether every surrogate is one half of a pair, in its place. Asked only of text that carries
        // something beyond ASCII, which is why the scanners need not ask it of every character.
        public static bool IsWellFormed(ReadOnlySpan<char> text)
        {
            var expectLowSurrogate = false;

            foreach (var character in text)
            {
                if (expectLowSurrogate != char.IsLowSurrogate(character))
                {
                    return false;
                }

                expectLowSurrogate = char.IsHighSurrogate(character);
            }

            return !expectLowSurrogate;
        }

        // IPv4address = dec-octet 3("." dec-octet), RFC 3986 §3.2.2, which is also RFC 5321's Snum form:
        // four groups of one to three digits, each at most 255. No shorthand and no other radix, so the
        // forms a parser would expand — 1.2.3, 0x7f.1, 2130706433 — are refused rather than rewritten.
        public static bool IsIPv4Address(ReadOnlySpan<char> text)
        {
            var octets = 0;

            while (true)
            {
                var dot = text.IndexOf('.');
                var octet = dot < 0 ? text : text[..dot];

                if (octet.Length is 0 or > 3
                    || !int.TryParse(octet, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                    || number > 255)
                {
                    return false;
                }

                octets++;

                if (dot < 0)
                {
                    return octets == 4;
                }

                if (octets == 4)
                {
                    return false;
                }

                text = text[(dot + 1)..];
            }
        }

        // The IPv6 grammar is long and IPAddress implements it; what IPAddress accepts beyond the URI and
        // mail grammars — a zone index, an IPv4 address in the IPv6 slot — is refused around it. The one
        // place an accepted value allocates, and reached only where a value carries an address literal.
        public static bool TryParseIPv6(ReadOnlySpan<char> text, [NotNullWhen(true)] out IPAddress? address)
        {
            if (!text.IsEmpty
                && !text.Contains('%')
                && IPAddress.TryParse(text, out address)
                && address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return true;
            }

            address = null;

            return false;
        }

        // Ldh-str: a letter, a digit, or a hyphen anywhere but first.
        private static bool IsLabelCharacter(char character, int labelLength)
        {
            return character < 0x80 && (LabelBits[character >> 3] & (1 << (character & 7))) != 0 && (character != '-' || labelLength > 0);
        }

        // A label ends legally when it has something in it, does not end in a hyphen and — while the name
        // is ASCII, so that a label's octets are its characters — fits DNS. Once anything is beyond ASCII,
        // IdnMapping holds every label to the limit instead.
        private static bool EndsLabel(int labelLength, bool ascii, char last)
        {
            return labelLength > 0 && last != '-' && (!ascii || labelLength <= MaximumLabelOctets);
        }

        private static bool IsInternationalizedName(ReadOnlySpan<char> host)
        {
            // GetAscii answers "not a valid internationalized domain name" by throwing, and offers no Try
            // form on any target framework, so catching it is the only way to hear the answer; nothing is
            // being swallowed. What it returns is the name as DNS carries it, which is what the octet limit
            // is about.
            try
            {
                return Idn.GetAscii(host.ToString()).Length <= MaximumHostOctets;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
