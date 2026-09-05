using System.Globalization;
using System.Text.RegularExpressions;

namespace NValidation
{
    /// <summary>
    /// Substitutes the named placeholders of a message template — <c>{MaxLength}</c>, and
    /// <c>{Step:0.00}</c> when a format is given.
    /// </summary>
    /// <remarks>
    /// A template which names fewer placeholders than the rule supplies drops the rest, and one which
    /// names a placeholder the rule does not supply keeps it as written instead of throwing.
    /// </remarks>
    public static partial class ValidationMessageFormatter
    {
        public static string Format(string template, IReadOnlyDictionary<string, object?> arguments)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(arguments);

            // A template naming nothing is returned as it stands: most messages carry a placeholder,
            // but a translation is free to leave every one of them out, and matching against a
            // template that cannot match is pure cost.
            if (!template.Contains('{'))
            {
                return template;
            }

            return PlaceholderPattern().Replace(template, match =>
            {
                if (!arguments.TryGetValue(match.Groups["name"].Value, out var value))
                {
                    return match.Value;
                }

                var format = match.Groups["format"];

                return format.Success && value is IFormattable formattable
                    ? formattable.ToString(format.Value, CultureInfo.CurrentCulture)
                    : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
            });
        }

        [GeneratedRegex(@"\{(?<name>\w+)(?::(?<format>[^}]+))?\}")]
        private static partial Regex PlaceholderPattern();
    }
}
