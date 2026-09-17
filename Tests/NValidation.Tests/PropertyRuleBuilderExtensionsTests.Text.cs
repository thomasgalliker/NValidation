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

        /// <summary>
        /// The rule decides the everyday shape over the characters themselves and only asks
        /// <see cref="System.Net.Mail.MailAddress"/> about the rest, so the two have to agree on every
        /// value — otherwise the shortcut would have changed a verdict rather than only its cost.
        /// </summary>
        [Theory]
        [InlineData("info@aurora-motors.example")]
        [InlineData("sales+fleet@aurora-motors.co.uk")]
        [InlineData("first.last@sub.domain.example")]
        [InlineData("under_score@example.com")]
        [InlineData("dash-name@a-b.example")]
        [InlineData("UPPER@CASE.EXAMPLE")]
        [InlineData("digits123@456example.com")]
        [InlineData("x@y.z")]
        [InlineData("a@localhost")] // a host without a dot is left to the parser
        [InlineData("\"quoted local\"@aurora-motors.example")]
        [InlineData("parts@[192.168.0.1]")]
        [InlineData("verkauf@aurora-motörs.example")]
        [InlineData("o'brien@example.com")]
        [InlineData("percent%sign@example.com")]
        [InlineData("equals=sign@example.com")]
        [InlineData("")]
        [InlineData("@example.com")]
        [InlineData("local@")]
        [InlineData(".leading@example.com")]
        [InlineData("trailing.@example.com")]
        [InlineData("double..dot@example.com")]
        [InlineData("two@@ats.example")]
        [InlineData("a@b..c")]
        [InlineData("a@-leading.example")]
        [InlineData("a@trailing-.example")]
        [InlineData("a@.example")]
        [InlineData("a@example.")]
        [InlineData("not an email")]
        [InlineData("missing-at.example.com")]
        [InlineData("info@aurora-motors.example ")]
        [InlineData(" info@aurora-motors.example")]
        [InlineData("Aurora Motors <info@aurora-motors.example>")]
        [InlineData("info@aurora-motors.example, sales@aurora-motors.example")]
        public async Task EmailAddress_AgreesWithTheParserOnEveryValue(string email)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress();

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // A blank value is the rule's own business and never reaches either path.
            var expectedToSucceed = string.IsNullOrWhiteSpace(email) ||
                (System.Net.Mail.MailAddress.TryCreate(email, out var parsed) &&
                 string.Equals(parsed.Address, email, StringComparison.Ordinal));

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(
                expectedToSucceed,
                "the rule and System.Net.Mail.MailAddress must reach the same verdict for '{0}'",
                email);
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
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress();

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
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress();

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("ContactEmail", "ContactEmail is not a valid email address.");
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
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailTopLevelDomainIn(topLevelDomain);

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
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailTopLevelDomainNotIn("test", "invalid");

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
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.ContactEmail).EmailTopLevelDomainIn("example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "info@aurora-motors.com";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("ContactEmail", "EmailTopLevelDomain");
        }

        [Fact]
        public async Task EmailTopLevelDomainNotIn_ReportsEmailTopLevelDomainNotAllowed()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.ContactEmail).EmailTopLevelDomainNotIn("test");

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "info@aurora-motors.test";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("ContactEmail", "EmailTopLevelDomainNotAllowed");
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
                () => validator.Property(m => m.ContactEmail).EmailTopLevelDomainIn(),
            };

            // Assert
            acts.Should().AllSatisfy(act => act.Should().Throw<ArgumentException>());
        }

        [Fact]
        public async Task EmailAddress_ReportsEmailAddress()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.ContactEmail).EmailAddress();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { ContactEmail = "not an email" });

            // Assert
            result.ShouldReport("ContactEmail", "EmailAddress");
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
