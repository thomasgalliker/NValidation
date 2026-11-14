using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using NValidation.TestData;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What the address scanner costs on values it accepts and on values it refuses, set against what
    /// the same verdict costs three other ways: through the framework's mail parser plus the round-trip
    /// check, which is the implementation the scanner replaced; through the small pattern the .NET
    /// guidance recommends; and through the one-@ check that is the floor. Every row is one validator
    /// with one rule, so the differences are the rules and nothing else.
    /// </summary>
    [MemoryDiagnoser]
    public partial class EmailAddressBenchmark
    {
        private NValidation.IValidator<Manufacturer> scanner = null!;
        private NValidation.IValidator<Manufacturer> frameworkParser = null!;
        private NValidation.IValidator<Manufacturer> pattern = null!;
        private NValidation.IValidator<Manufacturer> oneAtSign = null!;

        private Manufacturer manufacturer = null!;

        public static IEnumerable<Input> Inputs =>
        [
            new("everyday", "info@aurora-motors.example"),
            new("tagged, two-label TLD", "sales+fleet@aurora-motors.co.uk"),
            new("internationalized domain", "verkauf@aurora-motörs.example"),
            new("quoted local part", "\"john doe\"@example.com"),
            new("no @", "not an email"),
            new("plausible until the end", "missing-at.example.com"),
            new("near miss on a label", "a@-b.example"),
            new("bad IDN", "verkauf@aurora-mot￿rs.example"),
            new("4 KB of junk", new string('x', 4096)),
            new("4 KB local part", new string('a', 4096) + "@example.com"),
        ];

        [ParamsSource(nameof(Inputs))]
        public Input Value { get; set; } = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();
            this.manufacturer.ContactEmail = this.Value.Text;

            // Every refinement on, so the quoted row has a chance; on the other rows the refinements are
            // branches never taken.
            this.scanner = new OneRuleValidator(builder => builder
                .EmailAddress()
                .AllowQuotedLocalPart()
                .AllowAddressLiteral()
                .AllowSingleLabelDomain());

            this.frameworkParser = new OneRuleValidator(builder => builder.Must(PreviousImplementation.IsBareAddress));
            this.pattern = new OneRuleValidator(builder => builder.Matches(SmallPattern()));
            this.oneAtSign = new OneRuleValidator(builder => builder.Must(HasOneAtSign));
        }

        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> Scanner()
        {
            return this.scanner.ValidateAsync(this.manufacturer);
        }

        /// <summary>
        /// The implementation this replaced, reproduced below as it was: a span fast path for the everyday
        /// shape, and the framework's parser plus a round-trip check for everything else.
        /// </summary>
        [Benchmark]
        public ValueTask<ValidationResult> FrameworkParser()
        {
            return this.frameworkParser.ValidateAsync(this.manufacturer);
        }

        /// <summary>
        /// The pattern the .NET guidance recommends. It accepts far more than the scanner, so where it
        /// wins, the margin is what the scanner's strictness costs.
        /// </summary>
        [Benchmark]
        public ValueTask<ValidationResult> Pattern()
        {
            return this.pattern.ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> OneAtSign()
        {
            return this.oneAtSign.ValidateAsync(this.manufacturer);
        }

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        private static partial Regex SmallPattern();

        private static bool HasOneAtSign(string? value)
        {
            if (value is null)
            {
                return true;
            }

            var at = value.IndexOf('@');

            return at > 0 && at < value.Length - 1 && at == value.LastIndexOf('@');
        }

        public sealed record Input(string Name, string Text)
        {
            public override string ToString()
            {
                return this.Name;
            }
        }

        private sealed class OneRuleValidator : Validator<Manufacturer>
        {
            public OneRuleValidator(Action<PropertyRuleBuilder<Manufacturer, string?>> declare)
            {
                declare(this.Property("ContactEmail", static m => m.ContactEmail));
            }
        }
    }

    /// <summary>
    /// The domain rule on top of the address rule, before and after they became one rule: the previous
    /// chain parsed the value twice, once per rule; the current one parses it once and reads the domain
    /// off that parse.
    /// </summary>
    [MemoryDiagnoser]
    public class EmailAddressChainBenchmark
    {
        private NValidation.IValidator<Manufacturer> oneParse = null!;
        private NValidation.IValidator<Manufacturer> twoParses = null!;

        private Manufacturer manufacturer = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();

            this.oneParse = new ChainValidator(builder => builder.EmailAddress().RequireTopLevelDomain("example"));

            var allowed = new[] { "example" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
            this.twoParses = new ChainValidator(builder => builder
                .Must(PreviousImplementation.IsBareAddress)
                .Must(value => PreviousImplementation.IsUnderTopLevelDomain(value, allowed)));
        }

        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> AddressAndDomainInOneRule()
        {
            return this.oneParse.ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> AddressAndDomainAsTwoRules()
        {
            return this.twoParses.ValidateAsync(this.manufacturer);
        }

        private sealed class ChainValidator : Validator<Manufacturer>
        {
            public ChainValidator(Action<PropertyRuleBuilder<Manufacturer, string?>> declare)
            {
                declare(this.Property("ContactEmail", static m => m.ContactEmail));
            }
        }
    }

    /// <summary>
    /// The address check as it was before the scanner, kept verbatim so the comparison is against what
    /// shipped rather than a sketch of it.
    /// </summary>
    internal static class PreviousImplementation
    {
        public static bool IsBareAddress(string? value)
        {
            return string.IsNullOrWhiteSpace(value) || IsOrdinaryAddress(value) || TryGetBareAddress(value, out _);
        }

        public static bool IsUnderTopLevelDomain(string? value, FrozenSet<string> allowed)
        {
            if (value is null || !TryGetBareAddress(value, out var address))
            {
                return true;
            }

            return TopLevelDomainOf(address) is { } topLevelDomain && allowed.Contains(topLevelDomain);
        }

        private static bool IsOrdinaryAddress(ReadOnlySpan<char> value)
        {
            var at = value.IndexOf('@');

            if (at <= 0 || at == value.Length - 1)
            {
                return false;
            }

            return IsDotAtom(value[..at]) && IsDottedHost(value[(at + 1)..]);
        }

        private static bool IsDotAtom(ReadOnlySpan<char> local)
        {
            if (local[0] == '.' || local[^1] == '.')
            {
                return false;
            }

            var previousWasDot = false;

            foreach (var character in local)
            {
                if (character == '.')
                {
                    if (previousWasDot)
                    {
                        return false;
                    }

                    previousWasDot = true;
                    continue;
                }

                if (!char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-' or '+'))
                {
                    return false;
                }

                previousWasDot = false;
            }

            return true;
        }

        private static bool IsDottedHost(ReadOnlySpan<char> host)
        {
            var labelLength = 0;
            var dots = 0;
            var previous = '\0';

            foreach (var character in host)
            {
                if (character == '.')
                {
                    if (labelLength == 0 || previous == '-')
                    {
                        return false;
                    }

                    dots++;
                    labelLength = 0;
                    previous = character;
                    continue;
                }

                if (labelLength == 0 && character == '-')
                {
                    return false;
                }

                if (!char.IsAsciiLetterOrDigit(character) && character != '-')
                {
                    return false;
                }

                labelLength++;
                previous = character;
            }

            return dots > 0 && labelLength > 0 && previous != '-';
        }

        private static bool TryGetBareAddress(string value, [NotNullWhen(true)] out MailAddress? address)
        {
            if (MailAddress.TryCreate(value, out var parsed) && string.Equals(parsed.Address, value, StringComparison.Ordinal))
            {
                address = parsed;
                return true;
            }

            address = null;
            return false;
        }

        private static string? TopLevelDomainOf(MailAddress address)
        {
            var host = address.Host;

            if (host.StartsWith('['))
            {
                return null;
            }

            var lastDot = host.LastIndexOf('.');

            return lastDot >= 0 && lastDot < host.Length - 1 ? host[(lastDot + 1)..] : null;
        }
    }
}
