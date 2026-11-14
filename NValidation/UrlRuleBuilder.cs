using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// The chain <see cref="PropertyRuleBuilderExtensions.Url{T}"/> returns: the
    /// <see cref="PropertyRuleBuilder{T, TProperty}"/> of the same property, plus the refinements of the
    /// URL rule itself. Write the refinements first — every other rule and decoration returns the plain
    /// builder, which does not have them.
    /// </summary>
    /// <remarks>
    /// The refinements change the one rule <c>Url()</c> declared rather than adding rules of their own, so
    /// a value is parsed once and reports at most one failure, whichever refinement objected first.
    /// </remarks>
    public sealed class UrlRuleBuilder<T> : PropertyRuleBuilder<T, string?>
    {
        private readonly UrlOptions options;

        internal UrlRuleBuilder(PropertyRuleBuilder<T, string?> source, UrlOptions options)
            : base(source)
        {
            this.options = options;
        }

        /// <summary>
        /// Also accepts credentials before the host, e.g. <c>https://user:pass@example.com/</c>, which the
        /// plain rule refuses.
        /// </summary>
        /// <remarks>
        /// The plain rule refuses them because <c>https://example.com@evil.example/</c> reads to a person as
        /// a URL for <c>example.com</c> and points at <c>evil.example</c>. Opt in only where the field is
        /// known to carry credentials.
        /// </remarks>
        public UrlRuleBuilder<T> AllowUserInfo()
        {
            this.ThrowIfFrozen();

            this.options.AllowUserInfo = true;

            return this;
        }

        /// <summary>
        /// Also accepts an address in place of a host name, e.g. <c>http://192.0.2.1/</c> or
        /// <c>http://[2001:db8::1]/</c>, checked to be an IPv4 or IPv6 address and not merely shaped like one.
        /// </summary>
        /// <remarks>
        /// Only the dotted-quad and bracketed forms are read, so the shorthands another parser would expand
        /// — <c>1.2.3</c>, <c>0x7f.1</c>, <c>2130706433</c> — stay refused rather than being rewritten into
        /// an address nobody wrote.
        /// </remarks>
        public UrlRuleBuilder<T> AllowIPAddress()
        {
            this.ThrowIfFrozen();

            this.options.AllowIPAddress = true;

            return this;
        }

        /// <summary>
        /// Also accepts a URL that points at this machine: <c>localhost</c>, an address in
        /// <c>127.0.0.0/8</c>, or <c>[::1]</c>, which a development or test configuration carries.
        /// </summary>
        /// <remarks>
        /// Narrower than <see cref="AllowSingleLabelHost"/> and <see cref="AllowIPAddress"/> on purpose: it
        /// admits the loopback host and nothing else, so a configuration field can take
        /// <c>http://localhost:5001</c> without also taking every bare name and every address.
        /// </remarks>
        public UrlRuleBuilder<T> AllowLoopback()
        {
            this.ThrowIfFrozen();

            this.options.AllowLoopback = true;

            return this;
        }

        /// <summary>
        /// Also accepts a host of a single label, e.g. <c>http://intranet/</c>, which the plain rule refuses
        /// because a public URL always carries a dot.
        /// </summary>
        public UrlRuleBuilder<T> AllowSingleLabelHost()
        {
            this.ThrowIfFrozen();

            this.options.AllowSingleLabelHost = true;

            return this;
        }

        /// <summary>
        /// Also accepts a relative reference, e.g. <c>/orders/42</c>, <c>../a</c> or <c>?page=2</c>, for a
        /// field that may hold either a link or a path within the application.
        /// </summary>
        /// <remarks>
        /// <c>//example.com/x</c> stays refused although it is a legal relative reference: it names a host,
        /// which is how a field meant to hold a path sends a reader somewhere else. A relative reference has
        /// neither scheme nor host, so <see cref="RequireScheme"/>, <see cref="RequireHost"/> and
        /// <see cref="RequireDomain"/> have nothing to judge and pass it.
        /// </remarks>
        public UrlRuleBuilder<T> AllowRelative()
        {
            this.ThrowIfFrozen();

            this.options.AllowRelative = true;

            return this;
        }

        /// <summary>
        /// Requires one of <paramref name="schemes"/> in place of the <c>http</c> and <c>https</c> the plain
        /// rule allows, e.g. <c>RequireScheme("https")</c> for a field that must not carry a plaintext link.
        /// </summary>
        /// <remarks>
        /// Replaces the default pair rather than adding to it. Entries are compared without regard to case,
        /// and may be written with or without their trailing <c>:</c> or <c>://</c>. Reports
        /// <see cref="ValidationErrorCodes.UrlScheme"/> with <see cref="ValidationMessagePlaceholders.Schemes"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="schemes"/> is empty, names a blank entry, or names something that is not a scheme.
        /// </exception>
        public UrlRuleBuilder<T> RequireScheme(params string[] schemes)
        {
            this.ThrowIfFrozen();

            var entries = ToSchemes(schemes, nameof(schemes));

            this.options.Schemes = entries;
            this.options.SchemesText = string.Join(", ", entries);

            return this;
        }

        /// <summary>
        /// Requires the URL to point at one of <paramref name="hosts"/> exactly, e.g. the one endpoint a
        /// tenant may register a callback on.
        /// </summary>
        /// <remarks>
        /// Exact: <c>api.example.com</c> does not satisfy <c>example.com</c>; <see cref="RequireDomain"/> is
        /// the form that takes a host under a domain. Entries are compared without regard to case. Reports
        /// <see cref="ValidationErrorCodes.UrlHost"/> with <see cref="ValidationMessagePlaceholders.Hosts"/>.
        /// Where both are declared, a host satisfying either one passes.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="hosts"/> is empty, or names a blank entry.</exception>
        public UrlRuleBuilder<T> RequireHost(params string[] hosts)
        {
            this.ThrowIfFrozen();

            this.options.RequiredHosts = Terms.ToDomains(hosts, nameof(hosts));

            return this.WithHostsText();
        }

        /// <summary>
        /// Requires the URL to point at one of <paramref name="domains"/> or at a host under one, e.g.
        /// <c>RequireDomain("example.com")</c> for anything on that domain.
        /// </summary>
        /// <remarks>
        /// The boundary is a label: <c>api.example.com</c> is under <c>example.com</c> and
        /// <c>evil-example.com</c> is not. Entries are compared without regard to case, and may be written
        /// with or without their leading dot. Reports <see cref="ValidationErrorCodes.UrlHost"/> with
        /// <see cref="ValidationMessagePlaceholders.Hosts"/>. Where both are declared, a host satisfying
        /// either one passes.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="domains"/> is empty, or names a blank entry.</exception>
        public UrlRuleBuilder<T> RequireDomain(params string[] domains)
        {
            this.ThrowIfFrozen();

            this.options.RequiredDomains = Terms.ToDomains(domains, nameof(domains));

            return this.WithHostsText();
        }

        // The two host lists joined once, for the message, rather than on every failure. It is also what
        // tells the rule that a host list was declared at all.
        private UrlRuleBuilder<T> WithHostsText()
        {
            this.options.RequiredHostsText = string.Join(
                ", ",
                [.. this.options.RequiredHosts ?? [], .. this.options.RequiredDomains ?? []]);

            return this;
        }

        // The schemes without their separators and without repeats, in the order they were written.
        private static string[] ToSchemes(string[] schemes, string parameterName)
        {
            var terms = Terms.Require(schemes, parameterName);
            var normalized = new List<string>(terms.Length);

            foreach (var term in terms)
            {
                var entry = term.EndsWith("://", StringComparison.Ordinal) ? term[..^3]
                    : term.EndsWith(':') ? term[..^1]
                    : term;

                if (!Uri.CheckSchemeName(entry))
                {
                    throw new ArgumentException($"'{term}' is not a scheme name; a scheme is a letter followed by letters, digits, '+', '-' or '.'.", parameterName);
                }

                if (!normalized.Contains(entry, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(entry);
                }
            }

            return [.. normalized];
        }
    }
}
