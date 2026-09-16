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
    /// names a placeholder the rule does not supply keeps it as written instead of throwing. A format
    /// a value cannot honour renders the value unformatted, for the same reason.
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

                return RenderValue(value, match.Groups["format"]);
            });
        }

        /// <summary>
        /// One argument, rendered the way the template asked for it where the value can honour that.
        /// </summary>
        /// <remarks>
        /// The format specifier belongs to the message, which a host writes and translates, while the
        /// value it lands on comes from whichever rule reported the key — and one key serves several CLR
        /// types, so <c>{OtherValue:d}</c> is a date over a <see cref="DateTime"/> and a
        /// <see cref="FormatException"/> over a <see cref="decimal"/>. A host cannot tell those apart by
        /// reading its own resource file, so a specifier the value cannot honour falls back to the plain
        /// rendering rather than turning a bad request into a server error.
        /// </remarks>
        private static string RenderValue(object? value, Group format)
        {
            if (format.Success && TrySelectPluralForm(value, format.ValueSpan, out var pluralForm))
            {
                return pluralForm;
            }

            if (format.Success && value is IFormattable formattable)
            {
                try
                {
                    return formattable.ToString(format.Value, CultureInfo.CurrentCulture);
                }
                catch (FormatException)
                {
                    // Falls through to the plain rendering below.
                }
                catch (ArgumentException)
                {
                    // Some types report a bad specifier this way instead.
                }
            }

            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        }

        /// <summary>
        /// A format holding a single bar is not a format at all: it names the two forms a count selects
        /// between, as in <c>{MinCount:entry|entries}</c>.
        /// </summary>
        /// <remarks>
        /// Two-form selection, which is enough for English and for most of the Germanic and Romance
        /// languages. It is deliberately not an implementation of the CLDR plural rules: a language with
        /// three or more forms — Polish, Russian, Arabic — supplies its own
        /// <see cref="IValidationMessageProvider"/> and decides there, which the seam already allows.
        /// <para>
        /// Only a whole number selects; a bar is a legal literal in a custom numeric or date format
        /// string, so anything else falls through to being formatted as written. A bar over a whole
        /// number naming more than two forms is malformed, and renders the number alone rather than
        /// echoing the forms into the message.
        /// </para>
        /// </remarks>
        private static bool TrySelectPluralForm(object? value, ReadOnlySpan<char> format, out string form)
        {
            form = string.Empty;

            var bar = format.IndexOf('|');

            if (bar < 0 || !TryGetCount(value, out var count))
            {
                return false;
            }

            // A bar over a whole number was meant as a selector, so it is never handed to the number's
            // own formatting: a custom numeric format echoes the characters it does not recognise, which
            // would put "one|few|many" verbatim into a message somebody reads. More forms than two is a
            // malformed selector — render the number and let the wording around it carry the meaning.
            form = format[(bar + 1)..].Contains('|')
                ? Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty
                : (count == 1 ? format[..bar] : format[(bar + 1)..]).ToString();

            return true;
        }

        /// <summary>
        /// The whole number a rule supplied, where it supplied one.
        /// </summary>
        private static bool TryGetCount(object? value, out long count)
        {
            switch (value)
            {
                case int number: count = number; return true;
                case long number: count = number; return true;
                case short number: count = number; return true;
                case byte number: count = number; return true;
                case sbyte number: count = number; return true;
                case ushort number: count = number; return true;
                case uint number: count = number; return true;
                case ulong number: count = number <= long.MaxValue ? (long)number : long.MaxValue; return true;
                default: count = 0; return false;
            }
        }

        [GeneratedRegex(@"\{(?<name>\w+)(?::(?<format>[^}]+))?\}")]
        private static partial Regex PlaceholderPattern();
    }
}
