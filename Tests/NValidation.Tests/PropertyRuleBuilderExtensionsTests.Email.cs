namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        // RFC 5321 §4.5.3.1 and RFC 1035 §2.3.4, the limits the rule holds a value to.
        private const int MaximumLocalPartOctets = 64;
        private const int MaximumDomainOctets = 255;
        private const int MaximumLabelOctets = 63;

        [Theory]
        [InlineData(null)] // absent is left to NotEmpty
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("info@aurora-motors.example")]
        [InlineData("sales+fleet@aurora-motors.co.uk")]
        [InlineData("first.last@sub.domain.example")]
        [InlineData("under_score@example.com")]
        [InlineData("dash-name@a-b.example")]
        [InlineData("UPPER@CASE.EXAMPLE")]
        [InlineData("digits123@456example.com")]
        [InlineData("x@y.z")]
        [InlineData("o'brien@example.com")] // every atext character, not only the common ones
        [InlineData("percent%sign@example.com")]
        [InlineData("equals=sign@example.com")]
        [InlineData("verkauf@aurora-motörs.example")] // an internationalized domain, checked against IDNA
        [InlineData("verkauf@xn--mnchen-3ya.example")] // and a name in the form DNS carries it
        public async Task EmailAddress_AcceptsTheEverydayShape(string? email)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress();

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("not an email")]
        [InlineData("missing-at.example.com")]
        [InlineData("@example.com")]
        [InlineData("local@")]
        [InlineData("two@@ats.example")]
        [InlineData(".leading@example.com")]
        [InlineData("trailing.@example.com")] // Dot-string = Atom *("." Atom): no dot at either end
        [InlineData("double..dot@example.com")]
        [InlineData("a@b..c")]
        [InlineData("a@-leading.example")] // sub-domain = Let-dig [Ldh-str]: a label begins with a letter or digit
        [InlineData("a@trailing-.example")] // and ends with one
        [InlineData("a@.example")]
        [InlineData("a@example.")]
        [InlineData("a@exa mple.com")]
        [InlineData("Aurora Motors <info@aurora-motors.example>")] // the header forms are something other than one address
        [InlineData("\"Aurora Motors\" <info@aurora-motors.example>")]
        [InlineData("info@aurora-motors.example, sales@aurora-motors.example")]
        [InlineData("info@aurora-motors.example ")]
        [InlineData(" info@aurora-motors.example")]
        [InlineData("Aurora info@aurora-motors.example")]
        [InlineData("verkauf@aurora-mot￿rs.example")] // a scalar IDNA does not allow in a name
        public async Task EmailAddress_RefusesWhatIsNotOneAddress(string email)
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

        /// <summary>
        /// A stray surrogate is not text at all, in either half of the address. The values are built here
        /// rather than carried as theory data: that data is serialized, which turns a lone surrogate into
        /// U+FFFD — a character refused too, but for being disallowed by IDNA, which is a different case.
        /// </summary>
        [Fact]
        public async Task EmailAddress_RefusesAStraySurrogate()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress();

            var inTheLocalPart = new Manufacturer { ContactEmail = "\uD800@example.com" };
            var inTheDomain = new Manufacturer { ContactEmail = "verkauf@aurora-mot\uD800rs.example" };

            // Act
            var localPart = await validator.ValidateAsync(inTheLocalPart);
            var domain = await validator.ValidateAsync(inTheDomain);

            // Assert
            localPart.ShouldReport("ContactEmail", "ContactEmail is not a valid email address.");
            domain.ShouldReport("ContactEmail", "ContactEmail is not a valid email address.");
        }

        [Fact]
        public async Task EmailAddress_HoldsTheLocalPartToItsOctetLimit()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress();

            var atTheLimit = new Manufacturer { ContactEmail = new string('a', MaximumLocalPartOctets) + "@example.com" };
            var pastTheLimit = new Manufacturer { ContactEmail = new string('a', MaximumLocalPartOctets + 1) + "@example.com" };

            // Act
            var accepted = await validator.ValidateAsync(atTheLimit);
            var refused = await validator.ValidateAsync(pastTheLimit);

            // Assert
            accepted.Errors.Should().BeEmpty();
            refused.ShouldReportErrorCode("ContactEmail", "EmailAddress");
        }

        [Fact]
        public async Task EmailAddress_HoldsTheDomainToItsOctetLimit()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress();

            var atTheLimit = new Manufacturer { ContactEmail = "a@" + Domain(MaximumLabelOctets, MaximumLabelOctets, MaximumLabelOctets, MaximumLabelOctets) };
            var pastTheLimit = new Manufacturer { ContactEmail = "a@" + Domain(MaximumLabelOctets, MaximumLabelOctets, MaximumLabelOctets, MaximumLabelOctets - 1, 1) };

            // Act
            var accepted = await validator.ValidateAsync(atTheLimit);
            var refused = await validator.ValidateAsync(pastTheLimit);

            // Assert
            atTheLimit.ContactEmail!.Length.Should().Be(2 + MaximumDomainOctets, "the fixture must sit exactly on the limit");
            pastTheLimit.ContactEmail!.Length.Should().Be(2 + MaximumDomainOctets + 1, "the fixture must sit exactly past it");
            accepted.Errors.Should().BeEmpty();
            refused.ShouldReportErrorCode("ContactEmail", "EmailAddress");
        }

        [Fact]
        public async Task EmailAddress_HoldsALabelToItsOctetLimit()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress();

            var atTheLimit = new Manufacturer { ContactEmail = "a@" + Domain(MaximumLabelOctets, 7) };
            var pastTheLimit = new Manufacturer { ContactEmail = "a@" + Domain(MaximumLabelOctets + 1, 7) };

            // Act
            var accepted = await validator.ValidateAsync(atTheLimit);
            var refused = await validator.ValidateAsync(pastTheLimit);

            // Assert
            accepted.Errors.Should().BeEmpty();
            refused.ShouldReportErrorCode("ContactEmail", "EmailAddress");
        }

        [Theory]
        [InlineData("\"john doe\"@example.com", false, true)]
        [InlineData("\"a@b\"@example.com", false, true)] // the quoted part may carry the separator itself
        [InlineData("\"back\\\\slash\"@example.com", false, true)] // and any printable character behind a backslash
        [InlineData("\"tab\there\"@example.com", false, false)] // but only printable text
        [InlineData("\"unterminated@example.com", false, false)]
        [InlineData("\"a\"b@example.com", false, false)] // a quoted string is the whole local part or none of it
        [InlineData("parts@[192.0.2.1]", false, false)] // it admits nothing else
        [InlineData("root@localhost", false, false)]
        [InlineData("info@aurora-motors.example", true, true)]
        public async Task AllowQuotedLocalPart_AdmitsAQuotedLocalPartAndNothingElse(string email, bool plainRuleAccepts, bool refinedRuleAccepts)
        {
            // Arrange
            var plain = new TestValidator<Manufacturer>();
            plain.Property(m => m.ContactEmail).EmailAddress();

            var refined = new TestValidator<Manufacturer>();
            refined.Property(m => m.ContactEmail).EmailAddress().AllowQuotedLocalPart();

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var plainResult = await plain.ValidateAsync(manufacturer);
            var refinedResult = await refined.ValidateAsync(manufacturer);

            // Assert
            plainResult.Succeeded.Should().Be(plainRuleAccepts);
            refinedResult.Succeeded.Should().Be(refinedRuleAccepts);
        }

        [Theory]
        [InlineData("parts@[192.0.2.1]", false, true)]
        [InlineData("parts@[IPv6:2001:db8::1]", false, true)]
        [InlineData("parts@[ipv6:2001:db8::1]", false, true)] // the tag is a grammar literal, so its case does not matter
        [InlineData("parts@[not-an-ip]", false, false)] // bracketed is not the same as an address
        [InlineData("parts@[256.0.2.1]", false, false)]
        [InlineData("parts@[192.0.2]", false, false)]
        [InlineData("parts@[192.0.2.1.5]", false, false)]
        [InlineData("parts@[IPv6:fe80::1%eth0]", false, false)] // a zone index is not part of an address
        [InlineData("parts@[IPv6:192.0.2.1]", false, false)] // an IPv4 address in the IPv6 slot
        [InlineData("parts@[192.0.2.1", false, false)]
        [InlineData("\"john doe\"@example.com", false, false)] // it admits nothing else
        [InlineData("root@localhost", false, false)]
        [InlineData("info@aurora-motors.example", true, true)]
        public async Task AllowAddressLiteral_AdmitsAnAddressLiteralAndNothingElse(string email, bool plainRuleAccepts, bool refinedRuleAccepts)
        {
            // Arrange
            var plain = new TestValidator<Manufacturer>();
            plain.Property(m => m.ContactEmail).EmailAddress();

            var refined = new TestValidator<Manufacturer>();
            refined.Property(m => m.ContactEmail).EmailAddress().AllowAddressLiteral();

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var plainResult = await plain.ValidateAsync(manufacturer);
            var refinedResult = await refined.ValidateAsync(manufacturer);

            // Assert
            plainResult.Succeeded.Should().Be(plainRuleAccepts);
            refinedResult.Succeeded.Should().Be(refinedRuleAccepts);
        }

        [Theory]
        [InlineData("root@localhost", false, true)]
        [InlineData("a@b", false, true)]
        [InlineData("a@localhost.", false, false)] // a trailing dot is still an empty label
        [InlineData("a@-localhost", false, false)]
        [InlineData("\"john doe\"@example.com", false, false)] // it admits nothing else
        [InlineData("parts@[192.0.2.1]", false, false)]
        [InlineData("info@aurora-motors.example", true, true)]
        public async Task AllowSingleLabelDomain_AdmitsASingleLabelAndNothingElse(string email, bool plainRuleAccepts, bool refinedRuleAccepts)
        {
            // Arrange
            var plain = new TestValidator<Manufacturer>();
            plain.Property(m => m.ContactEmail).EmailAddress();

            var refined = new TestValidator<Manufacturer>();
            refined.Property(m => m.ContactEmail).EmailAddress().AllowSingleLabelDomain();

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var plainResult = await plain.ValidateAsync(manufacturer);
            var refinedResult = await refined.ValidateAsync(manufacturer);

            // Assert
            plainResult.Succeeded.Should().Be(plainRuleAccepts);
            refinedResult.Succeeded.Should().Be(refinedRuleAccepts);
        }

        /// <summary>
        /// The three refinements together are RFC 5321's Mailbox: every conformant form passes, and the
        /// forms the framework's parser used to let through — none of which any refinement admits — stay
        /// refused.
        /// </summary>
        [Theory]
        [InlineData("info@aurora-motors.example", true)]
        [InlineData("\"john doe\"@example.com", true)]
        [InlineData("parts@[192.0.2.1]", true)]
        [InlineData("parts@[IPv6:2001:db8::1]", true)]
        [InlineData("root@localhost", true)]
        [InlineData("verkauf@aurora-motörs.example", true)]
        [InlineData("trailing.@example.com", false)]
        [InlineData("a@-b.example", false)]
        [InlineData("parts@[not-an-ip]", false)]
        [InlineData("Aurora Motors <info@aurora-motors.example>", false)]
        public async Task EmailAddress_WithEveryRefinement_ReadsAMailbox(string email, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail)
                .EmailAddress()
                .AllowQuotedLocalPart()
                .AllowAddressLiteral()
                .AllowSingleLabelDomain();

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("info@aurora-motors.example", "example", true)]
        [InlineData("info@aurora-motors.example", ".example", true)] // written with or without its dot
        [InlineData("info@aurora-motors.EXAMPLE", "example", true)] // a domain does not care about case
        [InlineData("info@aurora-motors.com", "example", false)]
        [InlineData("root@localhost", "example", false)] // no top-level domain is not one of them
        [InlineData("parts@[192.0.2.1]", "example", false)] // nor is an address literal
        [InlineData(null, "example", true)] // left to NotEmpty
        public async Task RequireTopLevelDomain_AcceptsOnlyTheDomainsItNames(string? email, string topLevelDomain, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail)
                .EmailAddress()
                .AllowAddressLiteral()
                .AllowSingleLabelDomain()
                .RequireTopLevelDomain(topLevelDomain);

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
        [InlineData("root@localhost", true)] // nothing to refuse
        [InlineData("parts@[192.0.2.1]", true)]
        public async Task RefuseTopLevelDomain_RefusesTheDomainsItNames(string email, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail)
                .EmailAddress()
                .AllowAddressLiteral()
                .AllowSingleLabelDomain()
                .RefuseTopLevelDomain("test", "invalid");

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = email;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// The domain rules refine the one address rule rather than adding rules of their own, so a value
        /// that is not an address reports that and nothing about its domain.
        /// </summary>
        [Fact]
        public async Task EmailAddress_ReportsOneFailureForOneValue()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.ContactEmail)
                .EmailAddress()
                .RequireTopLevelDomain("com")
                .RefuseTopLevelDomain("test");

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "not an email";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("ContactEmail", "EmailAddress");
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
        public async Task RequireTopLevelDomain_ReportsEmailTopLevelDomain()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.ContactEmail).EmailAddress().RequireTopLevelDomain("example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "info@aurora-motors.com";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("ContactEmail", "EmailTopLevelDomain");
        }

        /// <summary>
        /// The message lists the entries as they were written — without their dots, without repeats, in
        /// the caller's order — so a reader of the failure sees the list the rule was given.
        /// </summary>
        [Fact]
        public async Task RequireTopLevelDomain_NamesTheDomainsAsTheyWereWritten()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress().RequireTopLevelDomain("example", ".CH", "Example", "de");

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "info@aurora-motors.com";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("ContactEmail", "ContactEmail must use one of the following top-level domains: example, CH, de.");
        }

        [Fact]
        public async Task RefuseTopLevelDomain_ReportsEmailTopLevelDomainNotAllowed()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.ContactEmail).EmailAddress().RefuseTopLevelDomain("test");

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "info@aurora-motors.test";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("ContactEmail", "EmailTopLevelDomainNotAllowed");
        }

        [Fact]
        public async Task RefuseTopLevelDomain_NamesTheDomainAsTheAddressWroteIt()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.ContactEmail).EmailAddress().RefuseTopLevelDomain("test");

            var manufacturer = Cars.Manufacturer();
            manufacturer.ContactEmail = "info@aurora-motors.TEST";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("ContactEmail", "ContactEmail must not use the top-level domain TEST.");
        }

        [Fact]
        public void EmailAddress_WithNoDomainToLookFor_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();

            // Act
            var acts = new Action[]
            {
                () => validator.Property(m => m.ContactEmail).EmailAddress().RequireTopLevelDomain(),
                () => validator.Property(m => m.ContactEmail).EmailAddress().RequireTopLevelDomain("  "),
                () => validator.Property(m => m.ContactEmail).EmailAddress().RefuseTopLevelDomain(),
                () => validator.Property(m => m.ContactEmail).EmailAddress().RefuseTopLevelDomain("  "),
            };

            // Assert
            acts.Should().AllSatisfy(act => act.Should().Throw<ArgumentException>());
        }

        /// <summary>
        /// A refinement changes the rule after it was declared, exactly as WithMessage does, and is refused
        /// after the first validation for the same reason.
        /// </summary>
        [Fact]
        public async Task EmailAddress_ARefinementAfterTheFirstValidation_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            var chain = validator.Property(m => m.ContactEmail).EmailAddress();

            await validator.ValidateAsync(Cars.Manufacturer());

            // Act
            var act = () => chain.AllowQuotedLocalPart();

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*'ContactEmail'*already been used*");
        }

        private static string Domain(params int[] labelLengths)
        {
            return string.Join('.', labelLengths.Select(length => new string('a', length)));
        }
    }
}
