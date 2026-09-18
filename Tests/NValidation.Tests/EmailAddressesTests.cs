using System.Net.Mail;
using System.Text;
using NValidation.Internals;

namespace NValidation.Tests
{
    /// <summary>
    /// The scanner behind <c>EmailAddress()</c>, asked directly about what a rule-level test cannot reach
    /// cheaply: that no input makes it throw or point outside the value, and that it never accepts what
    /// the framework's own mail parser refuses.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class EmailAddressesTests
    {
        // Every class of character the scanner tells apart, so that the strings over this alphabet reach
        // every branch: an atom character, a digit, the dot, the hyphen, the separator, the quote, the
        // brackets and colon of an address literal, whitespace, a letter beyond ASCII and a stray surrogate.
        private static readonly char[] Alphabet = ['a', '1', '.', '-', '@', '"', '[', ']', ':', ' ', 'ö', '\uD800'];

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
                if (!EmailAddresses.TryParse(value, options, out var parts))
                {
                    continue;
                }

                accepted++;

                // The domain is the tail of the value, just past the separator.
                var span = value.AsSpan();
                var separator = span.Length - parts.Domain.Length - 1;
                var slicesTheValue =
                    separator > 0
                    && span[separator] == '@'
                    && parts.Domain.SequenceEqual(span[(separator + 1)..]);

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
        /// The scanner reads a subset of what the framework's parser reads as one bare address — it refuses
        /// more, never less — so a value it accepts is one that parser accepts too. The one thing the two
        /// cannot agree on is decomposed non-ASCII, which that parser normalizes to composed form and so
        /// fails its own round-trip; the sweep's alphabet is composed, and the case is pinned on its own.
        /// </summary>
        [Fact]
        public void TryParse_AcceptsNothingTheFrameworkParserRefuses()
        {
            // Arrange
            var options = Everything();
            var disagreements = new List<string>();

            // Act
            foreach (var value in EveryString(SweepLength))
            {
                if (EmailAddresses.TryParse(value, options, out _) && !IsBareAddressToTheFramework(value))
                {
                    disagreements.Add(value);
                }
            }

            // Assert
            disagreements.Should().BeEmpty();
        }

        [Fact]
        public void TryParse_AcceptsADecomposedDomainTheFrameworkParserRefuses()
        {
            // Arrange
            var options = Everything();
            var decomposed = "verkauf@aurora-motörs.example";

            // Act
            var accepted = EmailAddresses.TryParse(decomposed, options, out _);
            var acceptedByTheFramework = IsBareAddressToTheFramework(decomposed);

            // Assert
            accepted.Should().BeTrue();
            acceptedByTheFramework.Should().BeFalse("this is the one known disagreement, and the guard above relies on it staying the only one");
        }

        [Theory]
        [InlineData("info@aurora-motors.example", "example")]
        [InlineData("info@aurora-motors.co.uk", "uk")]
        [InlineData("verkauf@aurora-motörs.example", "example")]
        [InlineData("root@localhost", "")] // a single label is under no top-level domain
        [InlineData("parts@[192.0.2.1]", "")] // nor is an address literal
        public void TopLevelDomain_IsTheLastLabelOrNothing(string value, string expected)
        {
            // Arrange
            var options = Everything();

            // Act
            var parsed = EmailAddresses.TryParse(value, options, out var parts);

            // Assert
            parsed.Should().BeTrue();
            parts.TopLevelDomain.ToString().Should().Be(expected);
        }

        private static EmailAddressOptions Everything()
        {
            return new EmailAddressOptions
            {
                AllowQuotedLocalPart = true,
                AllowAddressLiteral = true,
                AllowSingleLabelDomain = true,
            };
        }

        // What the framework reads as one address and nothing else: parsed, and parsed back to itself.
        private static bool IsBareAddressToTheFramework(string value)
        {
            return MailAddress.TryCreate(value, out var parsed) && string.Equals(parsed.Address, value, StringComparison.Ordinal);
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
