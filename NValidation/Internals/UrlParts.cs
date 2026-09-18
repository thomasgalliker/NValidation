namespace NValidation.Internals
{
    // What the URL rule reads off a parsed value: the scheme and the host, as slices of the value rather
    // than copies of it. Both are empty for a relative reference, which names neither.
    internal readonly ref struct UrlParts
    {
        public UrlParts(ReadOnlySpan<char> scheme, ReadOnlySpan<char> host)
        {
            this.Scheme = scheme;
            this.Host = host;
        }

        public ReadOnlySpan<char> Scheme { get; }

        // An IPv6 host is the literal without its brackets, so that a host list is written the way an
        // address is written.
        public ReadOnlySpan<char> Host { get; }

        public bool IsRelative => this.Scheme.IsEmpty;
    }
}
