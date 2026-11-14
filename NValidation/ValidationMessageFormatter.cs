using System.Globalization;
using System.Text;

namespace NValidation
{
    /// <summary>
    /// Substitutes the named placeholders of a message template — <c>{MaxLength}</c>, and
    /// <c>{Step:0.00}</c> when a format is given.
    /// </summary>
    /// <remarks>
    /// A template which names fewer placeholders than the rule supplies drops the rest, and one which
    /// names a placeholder the rule does not supply keeps it as written instead of throwing. A format
    /// a value cannot honour renders the value unformatted, for the same reason. Scanned by hand rather
    /// than with a regular expression, because this runs for every failure and a match evaluator costs
    /// a closure and a match object per placeholder.
    /// </remarks>
    public static class ValidationMessageFormatter
    {
        /// <summary>
        /// Substitutes the placeholders <paramref name="template"/> names from
        /// <paramref name="arguments"/>. A placeholder neither side knows is left as written.
        /// </summary>
        /// <exception cref="ArgumentNullException">Either argument is <c>null</c>.</exception>
        public static string Format(string template, IReadOnlyDictionary<string, object?> arguments)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(arguments);

            // A template naming nothing is returned as it stands: a translation is free to leave every
            // placeholder out, and scanning a template that cannot match is pure cost.
            var open = template.IndexOf('{');

            if (open < 0)
            {
                return template;
            }

            var builder = new StringBuilder(template.Length + 32);
            var rest = template.AsSpan();

            while (open >= 0)
            {
                builder.Append(rest[..open]);
                rest = rest[open..];

                if (TryReadPlaceholder(rest, out var name, out var format, out var length))
                {
                    var key = name.ToString();

                    if (arguments.TryGetValue(key, out var value))
                    {
                        builder.Append(RenderValue(value, format));
                    }
                    else
                    {
                        // A placeholder the rule did not supply is kept as written: visible in a test,
                        // harmless in production.
                        builder.Append(rest[..length]);
                    }

                    rest = rest[length..];
                }
                else
                {
                    // Not a placeholder — a lone brace, or one with nothing a name could be made of.
                    builder.Append('{');
                    rest = rest[1..];
                }

                open = rest.IndexOf('{');
            }

            builder.Append(rest);

            return builder.ToString();
        }

        private static bool TryReadPlaceholder(ReadOnlySpan<char> text, out ReadOnlySpan<char> name, out ReadOnlySpan<char> format, out int length)
        {
            name = default;
            format = default;
            length = 0;

            var i = 1;

            while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
            {
                i++;
            }

            if (i == 1 || i == text.Length)
            {
                return false;
            }

            name = text[1..i];

            if (text[i] == '}')
            {
                length = i + 1;

                return true;
            }

            if (text[i] != ':')
            {
                return false;
            }

            var close = text[(i + 1)..].IndexOf('}');

            if (close <= 0)
            {
                return false;
            }

            format = text.Slice(i + 1, close);
            length = i + 1 + close + 1;

            return true;
        }

        /// <summary>
        /// One argument, rendered the way the template asked. A specifier the value cannot honour renders it
        /// unformatted rather than throwing.
        /// </summary>
        private static string RenderValue(object? value, ReadOnlySpan<char> format)
        {
            if (!format.IsEmpty && TrySelectPluralForm(value, format, out var pluralForm))
            {
                return pluralForm;
            }

            if (!format.IsEmpty && value is IFormattable formattable)
            {
                try
                {
                    return formattable.ToString(format.ToString(), CultureInfo.CurrentCulture);
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
        /// Selects between the two forms of <c>{Count:entry|entries}</c>. Two forms only; a language with more
        /// supplies its own provider.
        /// </summary>
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
    }
}
