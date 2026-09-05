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
            var validator = new NameMinimumLengthValidator(3);
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
            var validator = new NameMaximumLengthValidator(5);
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
            var validator = new CountryCodeLengthValidator(3);
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
            var validator = new CountryCodeLengthValidator(3);
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
            var validator = new NameLengthRangeValidator(3, 5);
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
            // Act
            var act = () => new NameLengthRangeValidator(5, 3);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(null, true)] // absent is left to NotEmpty
        [InlineData("   ", true)]
        [InlineData("info@aurora-motors.example", true)]
        [InlineData("sales+fleet@aurora-motors.co.uk", true)]
        [InlineData("not an email", false)]
        [InlineData("missing-at.example.com", false)]
        public async Task EmailAddress_AcceptsWhatCanBeParsedAsAMailAddress(string? email, bool expectedToSucceed)
        {
            // Arrange
            var validator = new ContactEmailValidator();
            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
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
            var validator = new WebsitePatternValidator("^https?://");
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
            var validator = new WebsiteRegexValidator(new Regex("^https://", RegexOptions.IgnoreCase));
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
            var validator = new WebsitePatternValidator("^https?://");
            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "aurora-motors.example";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Errors.Should().ContainSingle().Which.Code.Should().Be(nameof(Manufacturer.Website));
        }

        [Fact]
        public void Matches_WithoutAPattern_Throws()
        {
            // Act
            var act = () => new WebsitePatternValidator(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Matches_WithoutARegex_Throws()
        {
            // Act
            var act = () => new WebsiteRegexValidator(null!);

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
            var regex = new Regex("(a+)+$", RegexOptions.None, TimeSpan.FromMilliseconds(1));
            var validator = new WebsiteRegexValidator(regex);
            var manufacturer = new Manufacturer { Website = new string('a', 5_000) + "!" };

            // Act
            var act = async () => await validator.ValidateAsync(manufacturer);

            // Assert
            await act.Should().NotThrowAsync<RegexMatchTimeoutException>();
        }

        [Fact]
        public async Task MinimumLength_ReportsMinimumLength()
        {
            // Act
            var result = await new NameMinimumLengthValidator(10).ValidateForKeysAsync(new Manufacturer { Name = "AB" });

            // Assert
            result.ShouldReport(nameof(Manufacturer.Name), ValidationMessageKeys.MinimumLength);
        }

        [Fact]
        public async Task MaximumLength_ReportsMaximumLength()
        {
            // Act
            var result = await new NameMaximumLengthValidator(2).ValidateForKeysAsync(new Manufacturer { Name = "Aurora" });

            // Assert
            result.ShouldReport(nameof(Manufacturer.Name), ValidationMessageKeys.MaximumLength);
        }

        [Fact]
        public async Task Length_ReportsLength()
        {
            // Act
            var result = await new CountryCodeLengthValidator(3).ValidateForKeysAsync(new Manufacturer { CountryCode = "CH" });

            // Assert
            result.ShouldReport(nameof(Manufacturer.CountryCode), ValidationMessageKeys.Length);
        }

        [Fact]
        public async Task LengthRange_ReportsLengthBetween()
        {
            // Act
            var result = await new NameLengthRangeValidator(5, 10).ValidateForKeysAsync(new Manufacturer { Name = "AB" });

            // Assert
            result.ShouldReport(nameof(Manufacturer.Name), ValidationMessageKeys.LengthBetween);
        }

        [Fact]
        public async Task EmailAddress_ReportsEmailAddress()
        {
            // Act
            var result = await new ContactEmailValidator().ValidateForKeysAsync(new Manufacturer { ContactEmail = "not an email" });

            // Assert
            result.ShouldReport(nameof(Manufacturer.ContactEmail), ValidationMessageKeys.EmailAddress);
        }

        [Fact]
        public async Task Matches_ReportsMatches()
        {
            // Act
            var result = await new WebsitePatternValidator(@"^https://").ValidateForKeysAsync(new Manufacturer { Website = "ftp://x" });

            // Assert
            result.ShouldReport(nameof(Manufacturer.Website), ValidationMessageKeys.Matches);
        }

    }
}
