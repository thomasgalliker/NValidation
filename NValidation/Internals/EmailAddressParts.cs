namespace NValidation.Internals
{
    // What the address rule reads off a parsed value: the domain, as a slice of the value rather than a
    // copy of it, and whether it is an address literal rather than a name.
    internal readonly ref struct EmailAddressParts
    {
        public EmailAddressParts(ReadOnlySpan<char> domain, bool isAddressLiteral)
        {
            this.Domain = domain;
            this.IsAddressLiteral = isAddressLiteral;
        }

        public ReadOnlySpan<char> Domain { get; }

        public bool IsAddressLiteral { get; }

        // The last label of the domain, or nothing where there is none to take: a domain of a single
        // label, or an address literal, which is not a domain name at all.
        public ReadOnlySpan<char> TopLevelDomain =>
            this.IsAddressLiteral ? default : HostNames.TopLevelLabelOf(this.Domain);
    }
}
