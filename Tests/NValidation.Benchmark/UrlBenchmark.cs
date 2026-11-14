using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using NValidation.TestData;

namespace NValidation.Benchmark
{
    /// <summary>
    /// What the URL scanner costs on values it accepts and on values it refuses, set against what the same
    /// verdict costs three other ways: through <see cref="Uri"/> plus the scheme check, which is the
    /// obvious way to write this rule by hand; through a pattern of the shape people reach for; and
    /// through the one-"//" check that is the floor. Every row is one validator with one rule, so the
    /// differences are the rules and nothing else.
    /// </summary>
    [MemoryDiagnoser]
    public partial class UrlBenchmark
    {
        private NValidation.IValidator<Manufacturer> scanner = null!;
        private NValidation.IValidator<Manufacturer> uriParser = null!;
        private NValidation.IValidator<Manufacturer> pattern = null!;
        private NValidation.IValidator<Manufacturer> doubleSlash = null!;

        private Manufacturer manufacturer = null!;

        public static IEnumerable<Input> Inputs =>
        [
            new("everyday", "https://aurora-motors.example"),
            new("path, query and fragment", "https://aurora-motors.example/parts?id=7&sort=name#top"),
            new("internationalized host", "https://aurora-motörs.example/"),
            new("address host", "http://192.0.2.1:8080/x"),
            new("no scheme", "aurora-motors.example"),
            new("plausible until the host", "https://-aurora-motors.example/"),
            new("scheme a link field refuses", "javascript:alert(1)"),
            new("near miss on an escape", "https://aurora-motors.example/%zz"),
            new("4 KB of junk", new string('x', 4096)),
            new("4 KB path", "https://aurora-motors.example/" + new string('a', 4096)),
        ];

        [ParamsSource(nameof(Inputs))]
        public Input Value { get; set; } = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();
            this.manufacturer.Website = this.Value.Text;

            // Every refinement on, so the address row has a chance; on the other rows the refinements are
            // branches never taken.
            this.scanner = new OneRuleValidator(builder => builder
                .Url()
                .AllowUserInfo()
                .AllowIPAddress()
                .AllowLoopback()
                .AllowSingleLabelHost());

            this.uriParser = new OneRuleValidator(builder => builder.Must(IsUrlThroughUri));
            this.pattern = new OneRuleValidator(builder => builder.Matches(SmallPattern()));
            this.doubleSlash = new OneRuleValidator(builder => builder.Must(HasSchemeAndAuthority));
        }

        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> Scanner()
        {
            return this.scanner.ValidateAsync(this.manufacturer);
        }

        /// <summary>
        /// The obvious hand-written rule. It accepts a great deal the scanner refuses, so where it wins,
        /// the margin is what the scanner's strictness costs.
        /// </summary>
        [Benchmark]
        public ValueTask<ValidationResult> UriParser()
        {
            return this.uriParser.ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> Pattern()
        {
            return this.pattern.ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> DoubleSlash()
        {
            return this.doubleSlash.ValidateAsync(this.manufacturer);
        }

        [GeneratedRegex(@"^https?://[^\s/?#]+\.[^\s/?#]+(?:[/?#]\S*)?$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        private static partial Regex SmallPattern();

        private static bool IsUrlThroughUri(string? value)
        {
            return value is null
                || (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
        }

        private static bool HasSchemeAndAuthority(string? value)
        {
            return value is null || value.IndexOf("://", StringComparison.Ordinal) > 0;
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
                declare(this.Property("Website", static m => m.Website));
            }
        }
    }

    /// <summary>
    /// The host rule on top of the URL rule, as one rule and as two: the refinement reads the host off the
    /// parse the URL rule already made, where a second rule would have to parse the value again.
    /// </summary>
    [MemoryDiagnoser]
    public class UrlChainBenchmark
    {
        private NValidation.IValidator<Manufacturer> oneParse = null!;
        private NValidation.IValidator<Manufacturer> twoParses = null!;

        private Manufacturer manufacturer = null!;

        [GlobalSetup]
        public void Setup()
        {
            this.manufacturer = Cars.Manufacturer();

            this.oneParse = new ChainValidator(builder => builder.Url().RequireDomain("aurora-motors.example"));

            this.twoParses = new ChainValidator(builder => builder
                .Must(static value => value is null || Uri.TryCreate(value, UriKind.Absolute, out _))
                .Must(static value => value is null
                    || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    || uri.Host.EndsWith("aurora-motors.example", StringComparison.OrdinalIgnoreCase)));
        }

        [Benchmark(Baseline = true)]
        public ValueTask<ValidationResult> UrlAndHostInOneRule()
        {
            return this.oneParse.ValidateAsync(this.manufacturer);
        }

        [Benchmark]
        public ValueTask<ValidationResult> UrlAndHostAsTwoRules()
        {
            return this.twoParses.ValidateAsync(this.manufacturer);
        }

        private sealed class ChainValidator : Validator<Manufacturer>
        {
            public ChainValidator(Action<PropertyRuleBuilder<Manufacturer, string?>> declare)
            {
                declare(this.Property("Website", static m => m.Website));
            }
        }
    }
}
