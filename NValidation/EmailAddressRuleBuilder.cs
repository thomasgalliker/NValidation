using NValidation.Internals;

namespace NValidation
{
    /// <summary>
    /// The chain <see cref="PropertyRuleBuilderExtensions.EmailAddress{T}"/> returns: the
    /// <see cref="PropertyRuleBuilder{T, TProperty}"/> of the same property, plus the refinements of the
    /// address rule itself. Write the refinements first — every other rule and decoration returns the
    /// plain builder, which does not have them.
    /// </summary>
    /// <remarks>
    /// The refinements change the one rule <c>EmailAddress()</c> declared rather than adding rules of their
    /// own, so a value is parsed once and reports at most one failure, whichever refinement objected first.
    /// </remarks>
    public sealed class EmailAddressRuleBuilder<T> : PropertyRuleBuilder<T, string?>
    {
        private readonly EmailAddressOptions options;

        internal EmailAddressRuleBuilder(PropertyRuleBuilder<T, string?> source, EmailAddressOptions options)
            : base(source)
        {
            this.options = options;
        }

        /// <summary>
        /// Also accepts a quoted local part, e.g. <c>"john doe"@example.com</c> — legal, and rare enough that
        /// the plain rule refuses it.
        /// </summary>
        public EmailAddressRuleBuilder<T> AllowQuotedLocalPart()
        {
            this.ThrowIfFrozen();

            this.options.AllowQuotedLocalPart = true;

            return this;
        }

        /// <summary>
        /// Also accepts an address literal in place of the domain, e.g. <c>parts@[192.0.2.1]</c> or
        /// <c>parts@[IPv6:2001:db8::1]</c>, checked to be an IPv4 or IPv6 address and not merely bracketed.
        /// </summary>
        public EmailAddressRuleBuilder<T> AllowAddressLiteral()
        {
            this.ThrowIfFrozen();

            this.options.AllowAddressLiteral = true;

            return this;
        }

        /// <summary>
        /// Also accepts a domain of a single label, e.g. <c>root@localhost</c>, which the plain rule refuses
        /// because a public address always carries a dot.
        /// </summary>
        public EmailAddressRuleBuilder<T> AllowSingleLabelDomain()
        {
            this.ThrowIfFrozen();

            this.options.AllowSingleLabelDomain = true;

            return this;
        }

        /// <summary>
        /// Requires the address to sit under one of <paramref name="topLevelDomains"/>, e.g. the domains a
        /// tenant is allowed to invite from. Entries are compared without regard to case, and may be
        /// written with or without their leading dot.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.EmailTopLevelDomain"/> with
        /// <see cref="ValidationMessagePlaceholders.TopLevelDomains"/>. An address whose domain carries no
        /// top-level domain — a single label, or an address literal — is under none of them and is reported.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="topLevelDomains"/> is empty, or names a blank entry.</exception>
        public EmailAddressRuleBuilder<T> RequireTopLevelDomain(params string[] topLevelDomains)
        {
            this.ThrowIfFrozen();

            var entries = Terms.ToDomains(topLevelDomains, nameof(topLevelDomains));

            this.options.RequiredTopLevelDomains = entries;
            this.options.RequiredTopLevelDomainsText = string.Join(", ", entries);

            return this;
        }

        /// <summary>
        /// Refuses an address under any of <paramref name="topLevelDomains"/> — the throwaway domains a
        /// signup form will not take, typically. Entries are compared without regard to case, and may be
        /// written with or without their leading dot.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.EmailTopLevelDomainNotAllowed"/> with
        /// <see cref="ValidationMessagePlaceholders.TopLevelDomain"/>. An address whose domain carries no
        /// top-level domain has nothing on the list and passes.
        /// </remarks>
        /// <inheritdoc cref="RequireTopLevelDomain(string[])" path="/exception"/>
        public EmailAddressRuleBuilder<T> RefuseTopLevelDomain(params string[] topLevelDomains)
        {
            this.ThrowIfFrozen();

            this.options.RefusedTopLevelDomains = Terms.ToDomains(topLevelDomains, nameof(topLevelDomains));

            return this;
        }
    }
}
