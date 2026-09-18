using System.Net;

namespace NValidation.Internals
{
    // The grammar Url() reads: RFC 3986, with the non-ASCII RFC 3987 admits, narrowed to a URL that names
    // a host unless the options widen it. One pass over the span, allocating nothing — except where the
    // .NET APIs consulted for an internationalized host and an IPv6 literal offer no allocation-free way
    // in. What a host is lives in HostNames, shared with EmailAddress().
    internal static class Urls
    {
        // pchar / "/", RFC 3986 §3.3, as one bit per ASCII code so that asking is one load rather than a
        // run of comparisons. '%' is not here: a percent-escape is read as a unit.
        private static ReadOnlySpan<byte> PathBits =>
            [0x00, 0x00, 0x00, 0x00, 0xD2, 0xFF, 0xFF, 0x2F, 0xFF, 0xFF, 0xFF, 0x87, 0xFE, 0xFF, 0xFF, 0x47];

        // The same, plus '?', which a query and a fragment may both carry. It also admits '[' and ']',
        // which RFC 3986 reserves for the host: a query of the form ?ids[]=1 is everywhere, every browser
        // sends it, and refusing what the rest of the stack accepts would only be a false alarm.
        private static ReadOnlySpan<byte> QueryBits =>
            [0x00, 0x00, 0x00, 0x00, 0xD2, 0xFF, 0xFF, 0xAF, 0xFF, 0xFF, 0xFF, 0xAF, 0xFE, 0xFF, 0xFF, 0x47];

        // userinfo = *( unreserved / pct-encoded / sub-delims / ":" ), RFC 3986 §3.2.1.
        private static ReadOnlySpan<byte> UserInfoBits =>
            [0x00, 0x00, 0x00, 0x00, 0xD2, 0x7F, 0xFF, 0x2F, 0xFE, 0xFF, 0xFF, 0x87, 0xFE, 0xFF, 0xFF, 0x47];

        // scheme = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." ), RFC 3986 §3.1.
        private static ReadOnlySpan<byte> SchemeBits =>
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x68, 0xFF, 0x03, 0xFE, 0xFF, 0xFF, 0x07, 0xFE, 0xFF, 0xFF, 0x07];

        // A port is at most five digits, and 65535 at that.
        private const int MaximumPort = 65535;

        public static bool TryParse(ReadOnlySpan<char> value, UrlOptions options, out UrlParts parts)
        {
            parts = default;

            if (value.IsEmpty)
            {
                return false;
            }

            var schemeEnd = SchemeEnd(value);

            if (schemeEnd < 0)
            {
                return options.AllowRelative && IsRelativeReference(value);
            }

            // hier-part = "//" authority path-abempty. The authority is required, which is what keeps
            // javascript:, data: and mailto: out: they carry no host, so there is nothing to point at.
            var afterScheme = value[(schemeEnd + 1)..];

            if (!afterScheme.StartsWith("//", StringComparison.Ordinal))
            {
                return false;
            }

            var rest = afterScheme[2..];
            var authorityEnd = rest.IndexOfAny('/', '?', '#');
            var authority = authorityEnd < 0 ? rest : rest[..authorityEnd];

            if (!TryReadAuthority(authority, options, out var host) || !IsTail(authorityEnd < 0 ? default : rest[authorityEnd..]))
            {
                return false;
            }

            parts = new UrlParts(value[..schemeEnd], host);

            return true;
        }

        // The index of the ':' that ends the scheme, or -1 where the value does not begin with one. A
        // value with no scheme is the only thing that can still be a relative reference.
        private static int SchemeEnd(ReadOnlySpan<char> value)
        {
            if (!char.IsAsciiLetter(value[0]))
            {
                return -1;
            }

            for (var i = 1; i < value.Length; i++)
            {
                var character = value[i];

                if (character == ':')
                {
                    return i;
                }

                if (!Allows(SchemeBits, character))
                {
                    return -1;
                }
            }

            return -1;
        }

        // authority = [ userinfo "@" ] host [ ":" port ]. The host comes out as a slice; which hosts are
        // admitted is the options' business, because every one of them is an opt-in.
        private static bool TryReadAuthority(ReadOnlySpan<char> authority, UrlOptions options, out ReadOnlySpan<char> host)
        {
            host = default;

            // userinfo carries no '@' of its own, so a second one lands inside it and is refused there.
            var at = authority.LastIndexOf('@');

            if (at >= 0)
            {
                if (!options.AllowUserInfo || !IsEncoded(authority[..at], UserInfoBits, allowNonAscii: false))
                {
                    return false;
                }

                authority = authority[(at + 1)..];
            }

            if (authority.IsEmpty)
            {
                return false;
            }

            if (authority[0] == '[')
            {
                var close = authority.IndexOf(']');

                if (close < 0 || !HostNames.TryParseIPv6(authority[1..close], out var address) || !IsPort(authority[(close + 1)..]))
                {
                    return false;
                }

                if (!options.AllowIPAddress && !(options.AllowLoopback && IPAddress.IsLoopback(address)))
                {
                    return false;
                }

                host = authority[1..close];

                return true;
            }

            // ':' is not part of a host name, so the first one starts the port.
            var colon = authority.IndexOf(':');
            var text = colon < 0 ? authority : authority[..colon];

            if (text.IsEmpty || (colon >= 0 && !IsPort(authority[colon..])))
            {
                return false;
            }

            if (HostNames.EndsInDigits(text))
            {
                // A host whose last label is all digits is an address and never a name: no top-level
                // domain is numeric. Reading it here is what refuses the shorthands a parser would
                // otherwise expand into something else — 1.2.3, 0x7f.1, 2130706433.
                if (!HostNames.IsIPv4Address(text)
                    || (!options.AllowIPAddress && !(options.AllowLoopback && text.StartsWith("127.", StringComparison.Ordinal))))
                {
                    return false;
                }

                host = text;

                return true;
            }

            if (!text.Contains('.')
                && !options.AllowSingleLabelHost
                && !(options.AllowLoopback && text.Equals("localhost", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (!HostNames.IsName(text, allowSingleLabel: true))
            {
                return false;
            }

            host = text;

            return true;
        }

        // Either nothing, or ":" followed by a port that a socket could be opened on.
        private static bool IsPort(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                return true;
            }

            if (text[0] != ':' || text.Length is 1 or > 6)
            {
                return false;
            }

            var port = 0;

            foreach (var character in text[1..])
            {
                if (!char.IsAsciiDigit(character))
                {
                    return false;
                }

                port = (port * 10) + (character - '0');
            }

            return port is >= 1 and <= MaximumPort;
        }

        // path-abempty [ "?" query ] [ "#" fragment ]. A fragment may carry '?' of its own, so the
        // fragment is taken first and the query is what is left before it.
        private static bool IsTail(ReadOnlySpan<char> tail)
        {
            var hash = tail.IndexOf('#');
            var beforeFragment = hash < 0 ? tail : tail[..hash];
            var fragment = hash < 0 ? default : tail[(hash + 1)..];

            var question = beforeFragment.IndexOf('?');
            var path = question < 0 ? beforeFragment : beforeFragment[..question];
            var query = question < 0 ? default : beforeFragment[(question + 1)..];

            return IsEncoded(path, PathBits, allowNonAscii: true)
                && IsEncoded(query, QueryBits, allowNonAscii: true)
                && IsEncoded(fragment, QueryBits, allowNonAscii: true);
        }

        // relative-ref = relative-part [ "?" query ] [ "#" fragment ], RFC 3986 §4.2, without the
        // network-path reference: "//host/x" names a host, and a field that means to hold a path must not
        // accept one that sends the reader somewhere else.
        private static bool IsRelativeReference(ReadOnlySpan<char> value)
        {
            if (value.StartsWith("//", StringComparison.Ordinal) || !IsTail(value))
            {
                return false;
            }

            var end = value.IndexOfAny('?', '#');
            var path = end < 0 ? value : value[..end];

            if (path.IsEmpty || path[0] == '/')
            {
                return true;
            }

            // path-noscheme: the first segment of a relative path carries no ':', or the reference would
            // read as a scheme instead.
            var slash = path.IndexOf('/');

            return !(slash < 0 ? path : path[..slash]).Contains(':');
        }

        // *( allowed / pct-encoded ), with anything beyond ASCII taken as RFC 3987 text and held to being
        // well-formed afterwards, so that the loop over the everyday URL, which is all ASCII, does nothing
        // but the structure.
        private static bool IsEncoded(ReadOnlySpan<char> text, ReadOnlySpan<byte> allowed, bool allowNonAscii)
        {
            var ascii = true;

            for (var i = 0; i < text.Length; i++)
            {
                var character = text[i];

                if (character == '%')
                {
                    // pct-encoded = "%" HEXDIG HEXDIG. A stray '%' is what a parser quietly rewrites to
                    // "%25", leaving the application storing something other than what was validated.
                    if (i + 2 >= text.Length || !char.IsAsciiHexDigit(text[i + 1]) || !char.IsAsciiHexDigit(text[i + 2]))
                    {
                        return false;
                    }

                    i += 2;
                }
                else if (character < 0x80)
                {
                    if (!Allows(allowed, character))
                    {
                        return false;
                    }
                }
                else if (!allowNonAscii)
                {
                    return false;
                }
                else
                {
                    ascii = false;
                }
            }

            return ascii || HostNames.IsWellFormed(text);
        }

        private static bool Allows(ReadOnlySpan<byte> bits, char character)
        {
            return character < 0x80 && (bits[character >> 3] & (1 << (character & 7))) != 0;
        }
    }
}
