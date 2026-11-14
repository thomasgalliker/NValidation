namespace NValidation.Tests
{
    public partial class PropertyRuleBuilderExtensionsTests
    {
        [Theory]
        [InlineData(null)] // absent is left to NotEmpty
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("https://aurora-motors.example")]
        [InlineData("https://aurora-motors.example/")]
        [InlineData("http://aurora-motors.example/parts/brakes")]
        [InlineData("https://sub.domain.aurora-motors.co.uk/x")]
        [InlineData("https://aurora-motors.example:8443/x")]
        [InlineData("https://aurora-motors.example/parts?id=7&sort=name#top")]
        [InlineData("HTTPS://AURORA-MOTORS.EXAMPLE/")] // a scheme and a host do not care about case
        [InlineData("https://aurora-motors.example/a!$&'()*+,;=b")] // sub-delims are legal in a path
        [InlineData("https://aurora-motors.example/a:b@c")] // and so are ':' and '@'
        [InlineData("https://aurora-motors.example/~user/-_.")]
        [InlineData("https://aurora-motors.example/%41%2F")] // a well-formed percent-escape
        [InlineData("https://aurora-motors.example/?ids[]=1")] // what every browser sends
        [InlineData("https://aurora-motors.example/#a/b?c")] // '/' and '?' are legal in a fragment
        [InlineData("https://aurora-motörs.example/")] // an internationalized host, checked against IDNA
        [InlineData("https://xn--aurora-motrs-jhb.example/")] // and a name in the form DNS carries it
        [InlineData("https://aurora-motors.example/caf\u00e9")] // RFC 3987 text in a path
        public async Task Url_AcceptsTheEverydayShape(string? url)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("not a url")]
        [InlineData("aurora-motors.example")] // a host alone is not a URL
        [InlineData("www.aurora-motors.example")]
        [InlineData("javascript:alert(1)")] // no authority, so there is nothing to point at
        [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
        [InlineData("about:blank")]
        [InlineData("mailto:info@aurora-motors.example")]
        [InlineData("tel:+41791234567")]
        [InlineData("//aurora-motors.example/path")] // a network-path reference names a host
        [InlineData("/parts/brakes")] // a relative reference is left to AllowRelative
        [InlineData("http://")]
        [InlineData("http://:8080/")]
        [InlineData("http:/aurora-motors.example")]
        [InlineData("https://aurora-motors.example ")] // the value has to be the URL alone
        [InlineData(" https://aurora-motors.example")]
        [InlineData("\thttps://aurora-motors.example\n")]
        [InlineData("https://aurora motors.example/")]
        [InlineData("https://aurora-motors.example/a b")]
        [InlineData("https://aurora-motors.example/a\nb")] // a control character is not text a URL carries
        [InlineData("https://aurora-motors.example/%zz")] // a percent-escape that is not one
        [InlineData("https://aurora-motors.example/%2")]
        [InlineData("https://aurora-motors.example/%")]
        [InlineData("https://.aurora-motors.example/")]
        [InlineData("https://aurora..motors.example/")]
        [InlineData("https://-aurora-motors.example/")] // a label begins with a letter or digit
        [InlineData("https://aurora-motors-.example/")] // and ends with one
        [InlineData("https://aurora-motors.example./")] // a trailing dot is still an empty label
        [InlineData("http://my_host.example/")] // an underscore is not a host-name character
        [InlineData("https://aurora-motors.example:0/")]
        [InlineData("https://aurora-motors.example:99999/")]
        [InlineData("https://aurora-motors.example:/")]
        [InlineData("https://aurora-motors.example:80a/")]
        [InlineData("https://user:pass@aurora-motors.example/")] // left to AllowUserInfo
        [InlineData("http://aurora-motors.example@evil.example/")]
        [InlineData("http://localhost/")] // left to AllowSingleLabelHost or AllowLoopback
        [InlineData("http://192.0.2.1/")] // left to AllowIPAddress
        [InlineData("http://[2001:db8::1]/")]
        [InlineData("http://1.2.3/")] // the shorthands a parser would expand into an address
        [InlineData("http://123.456.789.0/")]
        [InlineData("http://0x7f.1/")]
        [InlineData("http://2130706433/")]
        [InlineData("https://aurora-motors.example/#a#b")] // a fragment carries no '#' of its own
        public async Task Url_RefusesWhatIsNotOneUrl(string url)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("Website", "Website is not a valid URL.");
        }

        /// <summary>
        /// A stray surrogate is not text at all, in the host or in the path. The values are built here
        /// rather than carried as theory data: that data is serialized, which turns a lone surrogate into
        /// U+FFFD, a character refused for a different reason.
        /// </summary>
        [Fact]
        public async Task Url_RefusesAStraySurrogate()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url();

            var inTheHost = new Manufacturer { Website = "https://aurora-mot\uD800rs.example/" };
            var inThePath = new Manufacturer { Website = "https://aurora-motors.example/\uD800" };

            // Act
            var host = await validator.ValidateAsync(inTheHost);
            var path = await validator.ValidateAsync(inThePath);

            // Assert
            host.ShouldReport("Website", "Website is not a valid URL.");
            path.ShouldReport("Website", "Website is not a valid URL.");
        }

        [Fact]
        public async Task Url_HoldsTheHostToItsOctetLimits()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url();

            var atTheLabelLimit = new Manufacturer { Website = $"https://{new string('a', MaximumLabelOctets)}.example/" };
            var pastTheLabelLimit = new Manufacturer { Website = $"https://{new string('a', MaximumLabelOctets + 1)}.example/" };
            var atTheHostLimit = new Manufacturer { Website = $"https://{Domain(63, 63, 63, 63)}/" };
            var pastTheHostLimit = new Manufacturer { Website = $"https://{Domain(63, 63, 63, 62, 1)}/" };

            // Act
            var accepted = await validator.ValidateAsync(atTheLabelLimit);
            var wholeHost = await validator.ValidateAsync(atTheHostLimit);
            var label = await validator.ValidateAsync(pastTheLabelLimit);
            var host = await validator.ValidateAsync(pastTheHostLimit);

            // Assert
            Domain(63, 63, 63, 63).Length.Should().Be(MaximumDomainOctets, "the fixture must sit exactly on the limit");
            Domain(63, 63, 63, 62, 1).Length.Should().Be(MaximumDomainOctets + 1, "the fixture must sit exactly past it");
            accepted.Errors.Should().BeEmpty();
            wholeHost.Errors.Should().BeEmpty();
            label.ShouldReportErrorCode("Website", "Url");
            host.ShouldReportErrorCode("Website", "Url");
        }

        [Theory]
        [InlineData("https://user:pass@aurora-motors.example/", false, true)]
        [InlineData("https://user@aurora-motors.example/", false, true)]
        [InlineData("http://aurora-motors.example@evil.example/", false, true)] // admitted, and it points at evil.example
        [InlineData("https://us er@aurora-motors.example/", false, false)]
        [InlineData("https://a@b@aurora-motors.example/", false, false)] // userinfo carries no '@' of its own
        [InlineData("http://localhost/", false, false)] // it admits nothing else
        [InlineData("http://192.0.2.1/", false, false)]
        [InlineData("https://aurora-motors.example/", true, true)]
        public async Task AllowUserInfo_AdmitsCredentialsAndNothingElse(string url, bool plainRuleAccepts, bool refinedRuleAccepts)
        {
            // Arrange
            var plain = new TestValidator<Manufacturer>();
            plain.Property(m => m.Website).Url();

            var refined = new TestValidator<Manufacturer>();
            refined.Property(m => m.Website).Url().AllowUserInfo();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var plainResult = await plain.ValidateAsync(manufacturer);
            var refinedResult = await refined.ValidateAsync(manufacturer);

            // Assert
            plainResult.Succeeded.Should().Be(plainRuleAccepts);
            refinedResult.Succeeded.Should().Be(refinedRuleAccepts);
        }

        [Theory]
        [InlineData("http://192.0.2.1/", false, true)]
        [InlineData("http://192.0.2.1:8080/x", false, true)]
        [InlineData("http://[2001:db8::1]/", false, true)]
        [InlineData("http://[::1]:5001/", false, true)]
        [InlineData("http://127.0.0.1/", false, true)]
        [InlineData("http://256.0.2.1/", false, false)] // shaped like an address, and not one
        [InlineData("http://1.2.3/", false, false)]
        [InlineData("http://0x7f.1/", false, false)]
        [InlineData("http://2130706433/", false, false)]
        [InlineData("http://[2001:db8::1%25eth0]/", false, false)] // a zone index is not part of an address
        [InlineData("http://[not-an-ip]/", false, false)]
        [InlineData("http://[2001:db8::1/", false, false)]
        [InlineData("http://localhost/", false, false)] // it admits nothing else
        [InlineData("https://user@aurora-motors.example/", false, false)]
        [InlineData("https://aurora-motors.example/", true, true)]
        public async Task AllowIPAddress_AdmitsAnAddressAndNothingElse(string url, bool plainRuleAccepts, bool refinedRuleAccepts)
        {
            // Arrange
            var plain = new TestValidator<Manufacturer>();
            plain.Property(m => m.Website).Url();

            var refined = new TestValidator<Manufacturer>();
            refined.Property(m => m.Website).Url().AllowIPAddress();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var plainResult = await plain.ValidateAsync(manufacturer);
            var refinedResult = await refined.ValidateAsync(manufacturer);

            // Assert
            plainResult.Succeeded.Should().Be(plainRuleAccepts);
            refinedResult.Succeeded.Should().Be(refinedRuleAccepts);
        }

        [Theory]
        [InlineData("http://localhost/", false, true)]
        [InlineData("http://localhost:5001/api", false, true)]
        [InlineData("http://LOCALHOST/", false, true)]
        [InlineData("http://127.0.0.1/", false, true)]
        [InlineData("http://127.10.20.30:5001/", false, true)] // the whole of 127.0.0.0/8
        [InlineData("http://[::1]:5001/", false, true)]
        [InlineData("http://192.0.2.1/", false, false)] // an address that is not loopback
        [InlineData("http://[2001:db8::1]/", false, false)]
        [InlineData("http://intranet/", false, false)] // a bare name that is not localhost
        [InlineData("http://localhost.localdomain/", true, true)] // two labels, so the plain rule already takes it
        [InlineData("https://aurora-motors.example/", true, true)]
        public async Task AllowLoopback_AdmitsThisMachineAndNothingElse(string url, bool plainRuleAccepts, bool refinedRuleAccepts)
        {
            // Arrange
            var plain = new TestValidator<Manufacturer>();
            plain.Property(m => m.Website).Url();

            var refined = new TestValidator<Manufacturer>();
            refined.Property(m => m.Website).Url().AllowLoopback();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var plainResult = await plain.ValidateAsync(manufacturer);
            var refinedResult = await refined.ValidateAsync(manufacturer);

            // Assert
            plainResult.Succeeded.Should().Be(plainRuleAccepts);
            refinedResult.Succeeded.Should().Be(refinedRuleAccepts);
        }

        [Theory]
        [InlineData("http://intranet/", false, true)]
        [InlineData("http://localhost:5001/", false, true)]
        [InlineData("http://intranet./", false, false)] // a trailing dot is still an empty label
        [InlineData("http://-intranet/", false, false)]
        [InlineData("http://192.0.2.1/", false, false)] // it admits nothing else
        [InlineData("https://user@aurora-motors.example/", false, false)]
        [InlineData("https://aurora-motors.example/", true, true)]
        public async Task AllowSingleLabelHost_AdmitsABareNameAndNothingElse(string url, bool plainRuleAccepts, bool refinedRuleAccepts)
        {
            // Arrange
            var plain = new TestValidator<Manufacturer>();
            plain.Property(m => m.Website).Url();

            var refined = new TestValidator<Manufacturer>();
            refined.Property(m => m.Website).Url().AllowSingleLabelHost();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var plainResult = await plain.ValidateAsync(manufacturer);
            var refinedResult = await refined.ValidateAsync(manufacturer);

            // Assert
            plainResult.Succeeded.Should().Be(plainRuleAccepts);
            refinedResult.Succeeded.Should().Be(refinedRuleAccepts);
        }

        [Theory]
        [InlineData("/parts/brakes", false, true)]
        [InlineData("parts/brakes", false, true)]
        [InlineData("../parts", false, true)]
        [InlineData("./parts", false, true)]
        [InlineData("?page=2", false, true)]
        [InlineData("#top", false, true)]
        [InlineData("/parts?page=2#top", false, true)]
        [InlineData("./a:b", false, true)] // only the first segment may carry no colon
        [InlineData("//evil.example/parts", false, false)] // a network-path reference names a host
        [InlineData("not a url", false, false)]
        [InlineData("a:b/c", false, false)] // the first segment would read as a scheme
        [InlineData("C:\\temp\\x.txt", false, false)]
        [InlineData("/parts\\brakes", false, false)]
        [InlineData("/parts/%zz", false, false)]
        [InlineData("javascript:alert(1)", false, false)]
        [InlineData("https://aurora-motors.example/", true, true)]
        public async Task AllowRelative_AdmitsAReferenceWithoutAHostAndNothingElse(string url, bool plainRuleAccepts, bool refinedRuleAccepts)
        {
            // Arrange
            var plain = new TestValidator<Manufacturer>();
            plain.Property(m => m.Website).Url();

            var refined = new TestValidator<Manufacturer>();
            refined.Property(m => m.Website).Url().AllowRelative();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var plainResult = await plain.ValidateAsync(manufacturer);
            var refinedResult = await refined.ValidateAsync(manufacturer);

            // Assert
            plainResult.Succeeded.Should().Be(plainRuleAccepts);
            refinedResult.Succeeded.Should().Be(refinedRuleAccepts);
        }

        /// <summary>
        /// A relative reference names neither a scheme nor a host, so the rules about them have nothing to
        /// judge and pass it. An absolute value in the same chain is still held to both.
        /// </summary>
        [Theory]
        [InlineData("/parts/brakes", true)]
        [InlineData("https://aurora-motors.example/parts", true)]
        [InlineData("http://aurora-motors.example/parts", false)]
        [InlineData("https://evil.example/parts", false)]
        public async Task AllowRelative_LeavesTheSchemeAndHostRulesNothingToJudge(string url, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website)
                .Url()
                .AllowRelative()
                .RequireScheme("https")
                .RequireHost("aurora-motors.example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("https://aurora-motors.example/", "https", true)]
        [InlineData("https://aurora-motors.example/", "https://", true)] // written with or without its separator
        [InlineData("https://aurora-motors.example/", "https:", true)]
        [InlineData("HTTPS://aurora-motors.example/", "https", true)] // a scheme does not care about case
        [InlineData("http://aurora-motors.example/", "https", false)]
        [InlineData("ftp://files.aurora-motors.example/a", "ftp", true)] // it replaces the default pair
        [InlineData("https://aurora-motors.example/", "ftp", false)]
        [InlineData(null, "https", true)] // left to NotEmpty
        public async Task RequireScheme_AcceptsOnlyTheSchemesItNames(string? url, string scheme, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url().RequireScheme(scheme);

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("https://aurora-motors.example/", true)]
        [InlineData("https://AURORA-MOTORS.EXAMPLE/", true)] // a host does not care about case
        [InlineData("https://shop.aurora-motors.example/", false)] // exact, so a host under it does not count
        [InlineData("https://evil.example/", false)]
        [InlineData("https://evil-aurora-motors.example/", false)]
        public async Task RequireHost_AcceptsOnlyTheHostsItNames(string url, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url().RequireHost("aurora-motors.example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("https://aurora-motors.example/", true)] // the domain itself
        [InlineData("https://shop.aurora-motors.example/", true)]
        [InlineData("https://a.b.aurora-motors.example/", true)]
        [InlineData("https://AURORA-MOTORS.EXAMPLE/", true)]
        [InlineData("https://evil-aurora-motors.example/", false)] // the boundary is a label, not a suffix
        [InlineData("https://aurora-motors.example.evil.example/", false)]
        [InlineData("https://evil.example/", false)]
        public async Task RequireDomain_AcceptsTheDomainAndWhatSitsUnderIt(string url, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url().RequireDomain(".aurora-motors.example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        [Theory]
        [InlineData("https://api.partner.example/", true)] // named exactly
        [InlineData("https://aurora-motors.example/", true)] // or under the domain
        [InlineData("https://shop.aurora-motors.example/", true)]
        [InlineData("https://partner.example/", false)] // the exact entry does not take what sits under it
        [InlineData("https://evil.example/", false)]
        public async Task RequireHostAndRequireDomain_AcceptAHostSatisfyingEither(string url, bool expectedToSucceed)
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website)
                .Url()
                .RequireHost("api.partner.example")
                .RequireDomain("aurora-motors.example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = url;

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.Succeeded.Should().Be(expectedToSucceed);
        }

        /// <summary>
        /// The scheme and host rules refine the one URL rule rather than adding rules of their own, so a
        /// value that is not a URL reports that and nothing about its scheme or its host.
        /// </summary>
        [Fact]
        public async Task Url_ReportsOneFailureForOneValue()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Website)
                .Url()
                .RequireScheme("https")
                .RequireHost("aurora-motors.example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "not a url";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("Website", "Url");
        }

        [Fact]
        public async Task Url_ReportsUrl()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Website).Url();

            // Act
            var result = await validator.ValidateAsync(new Manufacturer { Website = "not a url" });

            // Assert
            result.ShouldReport("Website", "Url");
        }

        /// <summary>
        /// A well-formed URL whose scheme is not allowed is told apart from text that is not a URL, so the
        /// message can say which schemes are allowed even where nobody wrote RequireScheme.
        /// </summary>
        [Fact]
        public async Task Url_WithASchemeItDoesNotAllow_ReportsUrlScheme()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Website).Url();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "ftp://files.aurora-motors.example/a";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("Website", "UrlScheme");
        }

        [Fact]
        public async Task Url_WithNoRequireScheme_NamesTheSchemesItAllows()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url();

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "ftp://files.aurora-motors.example/a";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("Website", "Website must use one of the following schemes: http, https.");
        }

        /// <summary>
        /// The message lists the entries as they were written, without their separators, without repeats
        /// and in the caller's order.
        /// </summary>
        [Fact]
        public async Task RequireScheme_NamesTheSchemesAsTheyWereWritten()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website).Url().RequireScheme("https://", "WS", "https", "wss");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "http://aurora-motors.example/";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("Website", "Website must use one of the following schemes: https, WS, wss.");
        }

        [Fact]
        public async Task RequireHost_ReportsUrlHost()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>(ErrorCodeProvider.Instance);
            validator.Property(m => m.Website).Url().RequireHost("aurora-motors.example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "https://evil.example/";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("Website", "UrlHost");
        }

        [Fact]
        public async Task RequireHostAndRequireDomain_NameEveryHostTheyAllow()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            validator.Property(m => m.Website)
                .Url()
                .RequireHost("api.partner.example")
                .RequireDomain(".aurora-motors.example");

            var manufacturer = Cars.Manufacturer();
            manufacturer.Website = "https://evil.example/";

            // Act
            var result = await validator.ValidateAsync(manufacturer);

            // Assert
            result.ShouldReport("Website", "Website must point at one of the following hosts: api.partner.example, aurora-motors.example.");
        }

        [Fact]
        public void Url_WithNothingToLookFor_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();

            // Act
            var acts = new Action[]
            {
                () => validator.Property(m => m.Website).Url().RequireScheme(),
                () => validator.Property(m => m.Website).Url().RequireScheme("  "),
                () => validator.Property(m => m.Website).Url().RequireScheme("1nvalid"),
                () => validator.Property(m => m.Website).Url().RequireScheme("has space"),
                () => validator.Property(m => m.Website).Url().RequireHost(),
                () => validator.Property(m => m.Website).Url().RequireHost("  "),
                () => validator.Property(m => m.Website).Url().RequireDomain(),
                () => validator.Property(m => m.Website).Url().RequireDomain("  "),
                () => validator.Property(m => m.Website).Url().RequireDomain("."),
            };

            // Assert
            acts.Should().AllSatisfy(act => act.Should().Throw<ArgumentException>());
        }

        /// <summary>
        /// A refinement changes the rule after it was declared, exactly as WithMessage does, and is refused
        /// after the first validation for the same reason.
        /// </summary>
        [Fact]
        public async Task Url_ARefinementAfterTheFirstValidation_Throws()
        {
            // Arrange
            var validator = new TestValidator<Manufacturer>();
            var chain = validator.Property(m => m.Website).Url();

            await validator.ValidateAsync(Cars.Manufacturer());

            // Act
            var act = () => chain.AllowRelative();

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*'Website'*already been used*");
        }
    }
}
