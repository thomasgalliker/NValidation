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
        [InlineData("\"quoted local\"@aurora-motors.example", true)] // legal, and no hand-written pattern allows it
        [InlineData("parts@[192.168.0.1]", true)] // an address literal is an address
        [InlineData("verkauf@aurora-motörs.example", true)] // an internationalized domain is an address
        [InlineData("not an email", false)]
        [InlineData("missing-at.example.com", false)]
        [InlineData("two@@ats.example", false)]
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

        /// <summary>
        /// <see cref="System.Net.Mail.MailAddress"/> parses the header forms too, and each of them is
        /// something other than the single address the field asked for. A value carrying a display name,
        /// a second address, or surrounding whitespace is not one address, and what a host would later
        /// do with it — put it in a header, hand it to a recipient list — is not this rule's to assume.
        /// </summary>
        [Theory]
        [InlineData("Aurora Motors <info@aurora-motors.example>")]
        [InlineData("\"Aurora Motors\" <info@aurora-motors.example>")]
        [InlineData("info@aurora-motors.example, sales@aurora-motors.example")]
        [InlineData("info@aurora-motors.example ")]
        [InlineData(" info@aurora-motors.example")]
        [InlineData("Aurora info@aurora-motors.example")]
        public async Task EmailAddress_RefusesAValueWhichIsMoreThanTheAddress(string email)
        {
            // Arrange
            var validator = new ContactEmailValidator();
            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Theory]
        [InlineData("info@aurora-motors.example", "example", true)]
        [InlineData("info@aurora-motors.example", ".example", true)] // written with or without its dot
        [InlineData("info@aurora-motors.EXAMPLE", "example", true)] // a domain does not care about case
        [InlineData("info@aurora-motors.com", "example", false)]
        [InlineData("info@localhost", "example", false)] // no top-level domain is not one of them
        [InlineData("parts@[192.168.0.1]", "example", false)] // nor is an address literal
        [InlineData("not an email", "example", true)] // left to EmailAddress
        [InlineData(null, "example", true)] // left to NotEmpty
        public async Task EmailTopLevelDomainIn_AcceptsOnlyTheDomainsItNames(string? email, string topLevelDomain, bool expectedToSucceed)
        {
            // Arrange
            var validator = new ContactEmailTopLevelDomainInValidator(topLevelDomain);
            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("info@aurora-motors.test", false)]
        [InlineData("info@aurora-motors.TEST", false)]
        [InlineData("info@aurora-motors.example", true)]
        [InlineData("info@localhost", true)] // nothing to refuse
        public async Task EmailTopLevelDomainNotIn_RefusesTheDomainsItNames(string email, bool expectedToSucceed)
        {
            // Arrange
            var validator = new ContactEmailTopLevelDomainNotInValidator("test", "invalid");
            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Fact]
        public async Task EmailTopLevelDomainIn_ReportsEmailTopLevelDomain()
        {
            // Arrange
            var validator = new ContactEmailTopLevelDomainInValidator("example");
            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "info@aurora-motors.com";

            // Act
            var result = await validator.ValidateForKeysAsync(manufacturer);

            // Assert
            result.ShouldReport(nameof(Manufacturer.ContactEmail), ValidationMessageKeys.EmailTopLevelDomain);
        }

        [Fact]
        public async Task EmailTopLevelDomainNotIn_ReportsEmailTopLevelDomainNotAllowed()
        {
            // Arrange
            var validator = new ContactEmailTopLevelDomainNotInValidator("test");
            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "info@aurora-motors.test";

            // Act
            var result = await validator.ValidateForKeysAsync(manufacturer);

            // Assert
            result.ShouldReport(nameof(Manufacturer.ContactEmail), ValidationMessageKeys.EmailTopLevelDomainNotAllowed);
        }

        [Theory]
        [InlineData("Aurora Motors", true)]
        [InlineData("Aurora admin Motors", false)]
        [InlineData("Aurora ADMIN Motors", false)] // a blocklist which only matches one casing is no blocklist
        [InlineData(null, true)] // left to NotEmpty
        public async Task NotContaining_RefusesTheTermsItNames(string? name, bool expectedToSucceed)
        {
            // Arrange
            var validator = new NameNotContainingValidator("admin", "support");
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
            var validator = new NameNotContainingOrdinalValidator("admin");
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
            var validator = new NameNotContainingValidator("admin");
            var manufacturer = Cars.Manufacturer();
            manufacturer.Name = "Aurora admin Motors";

            // Act
            var keyed = await validator.ValidateForKeysAsync(manufacturer);
            var english = await new NameNotContainingValidator("admin").ValidateAsync(manufacturer);

            // Assert
            keyed.ShouldReport(nameof(Manufacturer.Name), ValidationMessageKeys.NotContaining);
            english.Errors.Should().ContainSingle().Which.Message.Should().NotContain("admin");
        }

        [Fact]
        public void NotContaining_WithNothingToLookFor_Throws()
        {
            // Act
            var acts = new Action[]
            {
                () => new NameNotContainingValidator(),
                () => new NameNotContainingValidator("  "),
                () => new ContactEmailTopLevelDomainInValidator(),
            };

            // Assert
            acts.Should().AllSatisfy(act => act.Should().Throw<ArgumentException>());
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
