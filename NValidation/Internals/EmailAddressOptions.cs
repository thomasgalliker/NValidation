namespace NValidation.Internals
{
    // What one EmailAddress() rule was refined to admit and to require. Written while the chain is
    // declared and read while it runs; the validator freezes its rules before the first validation, which
    // is what makes a plain mutable object safe here.
    internal sealed class EmailAddressOptions
    {
        public bool AllowQuotedLocalPart { get; set; }

        public bool AllowAddressLiteral { get; set; }

        public bool AllowSingleLabelDomain { get; set; }

        public string[]? RequiredTopLevelDomains { get; set; }

        // The required entries joined once, for the message, rather than on every failure.
        public string? RequiredTopLevelDomainsText { get; set; }

        public string[]? RefusedTopLevelDomains { get; set; }
    }
}
