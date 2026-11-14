using NValidation.Internals;

namespace NValidation
{
    public static partial class PropertyRuleBuilderExtensions
    {
        /// <summary>
        /// Requires the value to be one URL that names a host and nothing else: a scheme of <c>http</c> or
        /// <c>https</c>, then a host of two or more labels, then whatever path, query and fragment it
        /// carries. A missing or blank value passes; use <c>NotEmpty()</c> to require one.
        /// </summary>
        /// <remarks>
        /// Reports <see cref="ValidationErrorCodes.Url"/>, or <see cref="ValidationErrorCodes.UrlScheme"/>
        /// where the value is a URL whose scheme is not allowed. The URL is read against RFC 3986, with an
        /// internationalized host checked against IDNA, and never matched to a pattern. The forms this
        /// refuses — credentials before the host, an address in place of a name, a host of a single label, a
        /// relative reference — are admitted by the refinements on the <see cref="UrlRuleBuilder{T}"/> it
        /// returns, which also carries the scheme and host rules; write those before any other rule or
        /// decoration. A syntax rule says a URL is well-formed, never that it resolves, and never that it is
        /// safe to fetch: the host is resolved when the request is made, so no syntax rule can decide where
        /// it will point.
        /// </remarks>
        public static UrlRuleBuilder<T> Url<T>(this PropertyRuleBuilder<T, string?> builder)
        {
            var options = new UrlOptions();

            builder.Add(context =>
            {
                if (string.IsNullOrWhiteSpace(context.Value))
                {
                    return;
                }

                if (!Urls.TryParse(context.Value, options, out var parts))
                {
                    context.AddError(ValidationErrorCodes.Url);
                    return;
                }

                // A relative reference names neither a scheme nor a host, so the rules about them have
                // nothing to judge.
                if (parts.IsRelative)
                {
                    return;
                }

                // The scheme and host rules refine this rule rather than being rules of their own, so they
                // are asked in turn over the one parse and the first to object is the failure reported.
                if (!HostNames.IsOneOf(parts.Scheme, options.Schemes))
                {
                    context.AddError(ValidationErrorCodes.UrlScheme, (ValidationMessagePlaceholders.Schemes, options.SchemesText));
                    return;
                }

                if (options.RequiredHostsText is not { } allowed)
                {
                    return;
                }

                if (!(options.RequiredHosts is { } hosts && HostNames.IsOneOf(parts.Host, hosts))
                    && !(options.RequiredDomains is { } domains && HostNames.IsAtOrUnderOneOf(parts.Host, domains)))
                {
                    context.AddError(ValidationErrorCodes.UrlHost, (ValidationMessagePlaceholders.Hosts, allowed));
                }
            });

            return new UrlRuleBuilder<T>(builder, options);
        }
    }
}
