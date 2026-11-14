namespace NValidation.Internals
{
    // What one Url() rule was refined to admit and to require. Written while the chain is declared and
    // read while it runs; the validator freezes its rules before the first validation, which is what makes
    // a plain mutable object safe here.
    internal sealed class UrlOptions
    {
        // What a link field carries. A scheme outside this set is reported as a scheme rather than as a
        // malformed URL, so the message can say which ones are allowed even where nobody wrote RequireScheme.
        public static readonly string[] DefaultSchemes = ["http", "https"];

        public bool AllowUserInfo { get; set; }

        public bool AllowIPAddress { get; set; }

        public bool AllowLoopback { get; set; }

        public bool AllowSingleLabelHost { get; set; }

        public bool AllowRelative { get; set; }

        public string[] Schemes { get; set; } = DefaultSchemes;

        // The allowed entries joined once, for the message, rather than on every failure.
        public string SchemesText { get; set; } = "http, https";

        public string[]? RequiredHosts { get; set; }

        public string[]? RequiredDomains { get; set; }

        // Null until a host rule is declared, which is also how the rule knows to skip the check.
        public string? RequiredHostsText { get; set; }
    }
}
