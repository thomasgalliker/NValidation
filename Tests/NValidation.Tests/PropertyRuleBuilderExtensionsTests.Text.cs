using System.Text.RegularExpressions;

namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Theory]
        [InlineData(null, true)] // absent is left to NotEmpty
        [InlineData("ab", false)]
        [InlineData("abc", true)] // exactly the limit
        [InlineData("abcd", true)]
        public async Task MinimumLength_RejectsOnlyShorterText(string? name, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).MinimumLength(3);

            var manufacturer = Cars.Manufacturer();
            manufacturer.Name = name;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)] // absent is not too long
        [InlineData("abc", true)]
        [InlineData("abcde", true)] // exactly the limit
        [InlineData("abcdef", false)]
        public async Task MaximumLength_RejectsOnlyLongerText(string? name, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).MaximumLength(5);

            var manufacturer = Cars.Manufacturer();
            manufacturer.Name = name;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotEmpty
        [InlineData("CHE", true)]
        [InlineData("CH", false)]
        [InlineData("CHEX", false)]
        public async Task Length_RequiresAnExactNumberOfCharacters(string? countryCode, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.CountryCode).Length(3);

            var manufacturer = Cars.Manufacturer();
            manufacturer.CountryCode = countryCode;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// The text is measured as it arrived. Surrounding whitespace is part of the value, and
        /// rejecting a blank one is <c>NotEmpty</c>'s job.
        /// </summary>
        [Theory]
        [InlineData(" CHE ")] // five characters, not three
        [InlineData("   ")] // blank, but the wrong length either way is what this rule reports
        public async Task Length_MeasuresWhitespace_AsPartOfTheValue(string countryCode)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.CountryCode).Length(3);

            var manufacturer = Cars.Manufacturer();
            manufacturer.CountryCode = countryCode;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(countryCode.Length == 3);
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotEmpty
        [InlineData("ab", false)]
        [InlineData("abc", true)] // the lower bound
        [InlineData("abcde", true)] // the upper bound
        [InlineData("abcdef", false)]
        public async Task Length_WithARange_RequiresBothBounds(string? name, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).Length(3, 5);

            var manufacturer = Cars.Manufacturer();
            manufacturer.Name = name;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public void Length_WithAMinimumAboveTheMaximum_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();

            // Act
            var act = () => validator.Property(m => m.Name).Length(5, 3);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotEmpty
        [InlineData("   ", true)]
        [InlineData("https://aurora-motors.example", true)]
        [InlineData("http://aurora-motors.example", true)]
        [InlineData("aurora-motors.example", false)]
        public async Task Matches_RequiresThePatternToMatch(string? website, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Matches("^https?://");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = website;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task Matches_AcceptsAPreparedRegex()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Matches(new Regex("^https://", RegexOptions.IgnoreCase));

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "HTTPS://aurora-motors.example";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task Matches_NamesThePatternInTheMessage()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Matches("^https?://");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "aurora-motors.example";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("Website", "Website has an invalid format.");
        }

        [Fact]
        public void Matches_WithoutAPattern_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();

            // Act
            var act = () => validator.Property(m => m.Website).Matches((string)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Matches_WithoutARegex_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();

            // Act
            var act = () => validator.Property(m => m.Website).Matches((Regex)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// A pattern which backtracks pathologically must not be able to fail the request as a server
        /// error: a value the pattern cannot decide in time does not match, and that is a validation
        /// failure like any other.
        /// </summary>
        [Fact]
        public async Task Matches_WhenThePatternTimesOut_ReportsAFailureInsteadOfThrowing()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Matches(new Regex("(a+)+$", RegexOptions.None, TimeSpan.FromMilliseconds(1)));

            var manufacturer = new Manufacturer { Website = new string('a', 5_000) + "!" };

            // Act
            var act = async () => await validator.ValidateAsync(manufacturer);

            // Assert
            await act.Should().NotThrowAsync<RegexMatchTimeoutException>();
        }

        [Fact]
        public async Task MinimumLength_ReportsMinimumLength()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Name).MinimumLength(10);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { Name = "AB" });

            // Assert
            result.ShouldReport("Name", "MinimumLength");
        }

        [Fact]
        public async Task MaximumLength_ReportsMaximumLength()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Name).MaximumLength(2);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { Name = "Aurora" });

            // Assert
            result.ShouldReport("Name", "MaximumLength");
        }

        [Fact]
        public async Task Length_ReportsLength()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.CountryCode).Length(3);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { CountryCode = "CH" });

            // Assert
            result.ShouldReport("CountryCode", "Length");
        }

        [Fact]
        public async Task LengthRange_ReportsLengthBetween()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Name).Length(5, 10);

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { Name = "AB" });

            // Assert
            result.ShouldReport("Name", "LengthBetween");
        }

        [Theory]
        [InlineData("Aurora Motors", true)]
        [InlineData("Aurora admin Motors", false)]
        [InlineData("Aurora ADMIN Motors", false)] // a blocklist which only matches one casing is no blocklist
        [InlineData(null, true)] // left to NotEmpty
        public async Task NotContaining_RefusesTheTermsItNames(string? name, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotContaining("admin", "support");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Name = name;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("Aurora ADMIN Motors", true)]
        [InlineData("Aurora admin Motors", false)]
        public async Task NotContaining_WithAComparison_ComparesTheWayItWasTold(string name, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Name).NotContaining(StringComparison.Ordinal, "admin");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Name = name;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// The message names none of the terms: a blocklist which reports its own entries is one the
        /// next value works around.
        /// </summary>
        [Fact]
        public async Task NotContaining_ReportsNotContaining_WithoutNamingTheTerm()
        {
            // Arrange
            var englishValidator = new TestValidator<Manufacturer>();
            englishValidator.Property(m => m.Name).NotContaining("admin");

            var keyedValidator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            keyedValidator.Property(m => m.Name).NotContaining("admin");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Name = "Aurora admin Motors";

            // Act
            var english = await englishValidator.ValidateAsync(manufacturer);
            var keyed = await keyedValidator.ValidateAsync(manufacturer);

            // Assert
            keyed.ShouldReport("Name", "NotContaining");

            // The one assertion wildcards cannot express: what the message must *not* carry.
            english.Errors.Should().ContainSingle().Which.Message.Should().NotContain("admin");
        }

        [Fact]
        public void NotContaining_WithNothingToLookFor_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();

            // Act
            var acts = new Action[]
            {
                () => validator.Property(m => m.Name).NotContaining(),
                () => validator.Property(m => m.Name).NotContaining("  "),
            };

            // Assert
            acts.Should().AllSatisfy(act => act.Should().Throw<ArgumentException>());
        }

        [Fact]
        public async Task Matches_ReportsMatches()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Website).Matches(@"^https://");

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { Website = "ftp://x" });

            // Assert
            result.ShouldReport("Website", "Matches");
        }
    }
}
