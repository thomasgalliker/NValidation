using NValidation.Testing.Internals;

namespace NValidation.Tests.Testing
{
    /// <summary>
    /// The message matcher behind <see cref="ExpectedError.Message"/>.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class WildcardPatternTests
    {
        [Theory]
        [InlineData("Vin is required.", "Vin is required.")] // no wildcard: an exact match
        [InlineData("*", "Vin is required.")]
        [InlineData("*", "")]
        [InlineData("Vin*", "Vin is required.")]
        [InlineData("*required.", "Vin is required.")]
        [InlineData("*is required*", "Vin is required.")]
        [InlineData("**is**", "Vin is required.")]
        [InlineData("Vin is required?", "Vin is required.")]
        [InlineData("?in is required.", "Vin is required.")]
        [InlineData("*a*b*c*", "xxaxxbxxcxx")] // several wildcards, each greedy on its own
        [InlineData(@"5 \* 3", "5 * 3")] // an escaped wildcard stands for itself
        [InlineData(@"why\?", "why?")]
        [InlineData(@"a\\b", @"a\b")]
        [InlineData(@"a\b", @"a\b")] // a backslash before anything else is a plain backslash
        public void IsMatch_Matches(string pattern, string value)
        {
            // Act
            var matched = WildcardPattern.IsMatch(pattern, value);

            // Assert
            matched.Should().BeTrue();
        }

        [Theory]
        [InlineData("Vin is required.", "Vin is required")] // one character short
        [InlineData("Vin is required.", "vin is required.")] // case matters
        [InlineData("*optional*", "Vin is required.")]
        [InlineData("Vin?", "Vin")] // ? demands exactly one character
        [InlineData("?", "")]
        [InlineData("*a*b*c*", "xxaxxcxxbxx")] // the right pieces, the wrong order
        [InlineData(@"5 \* 3", "5 x 3")] // an escaped wildcard no longer swallows anything
        [InlineData(@"why\?", "whyx")]
        public void IsMatch_DoesNotMatch(string pattern, string value)
        {
            // Act
            var matched = WildcardPattern.IsMatch(pattern, value);

            // Assert
            matched.Should().BeFalse();
        }

        /// <summary>
        /// A pattern of alternating wildcards and single characters is where a matcher which never gives
        /// a wildcard back its characters loops or stalls.
        /// </summary>
        [Fact]
        public void IsMatch_WithManyWildcards_Terminates()
        {
            // Arrange
            var pattern = string.Concat(Enumerable.Repeat("*a", 20)) + "*";
            var value = string.Concat(Enumerable.Repeat("aa", 40));

            // Act
            var matched = WildcardPattern.IsMatch(pattern, value);

            // Assert
            matched.Should().BeTrue();
        }
    }
}
