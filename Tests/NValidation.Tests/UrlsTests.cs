using System.Text;
using NValidation.Internals;

namespace NValidation.Tests
{
    /// <summary>
    /// The scanner behind <c>Url()</c>, asked directly about what a rule-level test cannot reach cheaply:
    /// that no input makes it throw or point outside the value, and that it never accepts what
    /// <see cref="Uri"/> refuses — the property that lets an application parse a value the rule approved.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class UrlsTests
    {
        // Every class of character the scanner tells apart, so that the strings over this alphabet reach
        // every branch: a letter, a digit, the scheme separator, the path separator, the label dot and
        // hyphen, the userinfo separator, a percent-escape, the query and fragment marks, a bracket,
        // whitespace, a letter beyond ASCII and a stray surrogate.
        private static readonly char[] Alphabet = ['a', '1', ':', '/', '.', '-', '@', '%', '?', '#', '[', ' ', 'ö', '\uD800'];

        // The shortest absolute URL the scanner accepts is five characters, so a shorter sweep would
        // exercise the refusal paths alone.
        private const int SweepLength = 5;

        [Fact]
        public void TryParse_OverEveryShortString_NeverThrowsAndSlicesOnlyTheValue()
        {
            // Arrange
            var options = Everything();
            var misplaced = new List<string>();
            var accepted = 0;

            // Act
            foreach (var value in EveryString(SweepLength))
            {
                if (!Urls.TryParse(value, options, out var parts))
                {
                    continue;
                }

                accepted++;

                var span = value.AsSpan();
                var slicesTheValue =
                    (parts.Scheme.IsEmpty || (span.Overlaps(parts.Scheme) && span.StartsWith(parts.Scheme) && span[parts.Scheme.Length] == ':'))
                    && (parts.Host.IsEmpty || span.Overlaps(parts.Host));

                if (!slicesTheValue)
                {
                    misplaced.Add(value);
                }
            }

            // Assert
            misplaced.Should().BeEmpty();
            accepted.Should().BePositive("a sweep that accepts nothing has not exercised the accept path");
        }

        /// <summary>
        /// The scanner reads a subset of what <see cref="Uri"/> reads — it refuses more, never less — so a
        /// value it accepts is one the application can hand to <see cref="Uri"/> afterwards.
        /// </summary>
        [Fact]
        public void TryParse_AcceptsNothingUriRefuses()
        {
            // Arrange
            var options = Everything();
            var disagreements = new List<string>();

            // Act
            foreach (var value in EveryString(SweepLength))
            {
                if (Urls.TryParse(value, options, out var parts)
                    && !parts.IsRelative
                    && !Uri.TryCreate(value, UriKind.Absolute, out _))
                {
                    disagreements.Add(value);
                }
            }

            // Assert
            disagreements.Should().BeEmpty();
        }

        /// <summary>
        /// And for the schemes the rule allows by default, the two agree on what they read: a value the
        /// rule approved names the same host to the application that will fetch it.
        /// </summary>
        [Theory]
        [InlineData("https://aurora-motors.example", "https", "aurora-motors.example")]
        [InlineData("HTTP://AURORA-MOTORS.EXAMPLE/x", "http", "aurora-motors.example")]
        [InlineData("https://shop.aurora-motors.co.uk:8443/a?b=1#c", "https", "shop.aurora-motors.co.uk")]
        [InlineData("https://aurora-motörs.example/", "https", "aurora-motörs.example")]
        [InlineData("http://192.0.2.1:8080/x", "http", "192.0.2.1")]
        [InlineData("https://user:pass@aurora-motors.example/", "https", "aurora-motors.example")]
        public void TryParse_ReadsTheSameSchemeAndHostAsUri(string value, string expectedScheme, string expectedHost)
        {
            // Arrange
            var options = Everything();

            // Act
            var parsed = Urls.TryParse(value, options, out var parts);
            var uriParsed = Uri.TryCreate(value, UriKind.Absolute, out var uri);

            // Assert
            parsed.Should().BeTrue();
            uriParsed.Should().BeTrue();
            parts.Scheme.ToString().Should().BeEquivalentTo(expectedScheme, "a scheme and a host are compared without regard to case, and both are reported as written");
            parts.Host.ToString().Should().BeEquivalentTo(expectedHost);
            uri!.Scheme.Should().Be(expectedScheme);
            uri.Host.Should().BeEquivalentTo(expectedHost);
        }

        /// <summary>
        /// The same guarantee on the shapes the sweep is too short to reach: a port, an address literal,
        /// a userinfo, a long path, and the characters the scanner admits beyond bare ASCII letters.
        /// </summary>
        [Theory]
        [InlineData("http://a.example:1/")]
        [InlineData("http://a.example:65535/")]
        [InlineData("http://a.example:00001/")]
        [InlineData("http://[2001:db8::1]:8080/x")]
        [InlineData("http://[::1]/")]
        [InlineData("http://192.0.2.1/")]
        [InlineData("http://127.0.0.1:5001/")]
        [InlineData("http://localhost:5001/api")]
        [InlineData("https://user:pass@a.example/")]
        [InlineData("https://a.example/?ids[]=1")]
        [InlineData("https://a.example/#a?b/c")]
        [InlineData("https://a.example/caf\u00e9?q=caf\u00e9#caf\u00e9")]
        [InlineData("https://a.example/a!$&'()*+,;=b")]
        [InlineData("https://a.example/a:b@c")]
        [InlineData("https://a.example/%41%2F")]
        [InlineData("https://aurora-mot\u00f6rs.example/")]
        [InlineData("https://xn--aurora-motrs-jhb.example/")]
        public void TryParse_AcceptsNothingUriRefuses_OnTheShapesTheSweepCannotReach(string value)
        {
            // Act
            var parsed = Urls.TryParse(value, Everything(), out _);
            var uriParsed = Uri.TryCreate(value, UriKind.Absolute, out _);

            // Assert
            parsed.Should().BeTrue("the scanner is meant to accept this shape");
            uriParsed.Should().BeTrue("a value the rule accepts has to be one Uri can parse afterwards");
        }

        [Fact]
        public void TryParse_AcceptsALongPath_WhichUriAlsoParses()
        {
            // Arrange
            var value = "https://aurora-motors.example/" + new string('a', 100_000);

            // Act
            var parsed = Urls.TryParse(value, Everything(), out _);
            var uriParsed = Uri.TryCreate(value, UriKind.Absolute, out _);

            // Assert
            parsed.Should().BeTrue();
            uriParsed.Should().BeTrue();
        }

        /// <summary>
        /// An IPv6 host comes out without its brackets, so a host list is written the way an address is.
        /// </summary>
        [Fact]
        public void TryParse_ReadsAnIPv6HostWithoutItsBrackets()
        {
            // Act
            var parsed = Urls.TryParse("http://[2001:db8::1]:8080/x", Everything(), out var parts);

            // Assert
            parsed.Should().BeTrue();
            parts.Host.ToString().Should().Be("2001:db8::1");
        }

        /// <summary>
        /// Both rules read a host through the same grammar, so a name one of them accepts is one the other
        /// accepts too. The day they disagree is the day a value passes one rule and fails the other.
        /// </summary>
        [Theory]
        [InlineData("aurora-motors.example", true)]
        [InlineData("shop.aurora-motors.co.uk", true)]
        [InlineData("aurora-motörs.example", true)]
        [InlineData("xn--aurora-motrs-jhb.example", true)]
        [InlineData("localhost", true)]
        [InlineData("-aurora-motors.example", false)]
        [InlineData("aurora-motors-.example", false)]
        [InlineData("aurora..motors.example", false)]
        [InlineData("aurora-motors.example.", false)]
        [InlineData("my_host.example", false)]
        [InlineData("aurora motors.example", false)]
        public async Task AHost_ReadsTheSameWayForBothRules(string host, bool expectedToSucceed)
        {
            // Arrange
            var email = new TestValidator<Manufacturer>();
            email.Property(m => m.ContactEmail).EmailAddress().AllowSingleLabelDomain();

            var url = new TestValidator<Manufacturer>();
            url.Property(m => m.Website).Url().AllowSingleLabelHost();

            // Act
            var emailResult = await email.ValidateAsync(new Manufacturer { ContactEmail = $"info@{host}" });
            var urlResult = await url.ValidateAsync(new Manufacturer { Website = $"https://{host}/" });

            // Assert
            emailResult.Succeeded.Should().Be(expectedToSucceed);
            urlResult.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("aurora-motors.example", true)]
        [InlineData("shop.aurora-motors.example", true)]
        [InlineData("a.b.aurora-motors.example", true)]
        [InlineData("AURORA-MOTORS.EXAMPLE", true)]
        [InlineData("evil-aurora-motors.example", false)] // the boundary is a label, not a suffix
        [InlineData("aurora-motors.example.evil.example", false)]
        [InlineData("urora-motors.example", false)]
        [InlineData("example", false)]
        public void IsAtOrUnderOneOf_CutsAtALabelBoundary(string host, bool expected)
        {
            // Act
            var atOrUnder = HostNames.IsAtOrUnderOneOf(host, ["aurora-motors.example"]);

            // Assert
            atOrUnder.Should().Be(expected);
        }

        [Theory]
        [InlineData("aurora-motors.example", false)]
        [InlineData("42.example", false)]
        [InlineData("a.b.42", true)]
        [InlineData("192.0.2.1", true)]
        [InlineData("2130706433", true)]
        [InlineData("0x7f.1", true)]
        public void EndsInDigits_TellsAnAddressFromAName(string host, bool expected)
        {
            // Act
            var endsInDigits = HostNames.EndsInDigits(host);

            // Assert
            endsInDigits.Should().Be(expected);
        }

        private static UrlOptions Everything()
        {
            return new UrlOptions
            {
                AllowUserInfo = true,
                AllowIPAddress = true,
                AllowLoopback = true,
                AllowSingleLabelHost = true,
                AllowRelative = true,
            };
        }

        // Every string of one to maxLength characters over the alphabet, counted up like an odometer.
        private static IEnumerable<string> EveryString(int maxLength)
        {
            var builder = new StringBuilder(maxLength);

            for (var length = 1; length <= maxLength; length++)
            {
                var digits = new int[length];

                while (true)
                {
                    builder.Clear();

                    foreach (var digit in digits)
                    {
                        builder.Append(Alphabet[digit]);
                    }

                    yield return builder.ToString();

                    var position = length - 1;

                    while (position >= 0 && ++digits[position] == Alphabet.Length)
                    {
                        digits[position] = 0;
                        position--;
                    }

                    if (position < 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}
