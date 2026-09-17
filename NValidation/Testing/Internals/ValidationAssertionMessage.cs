using System.Text;

namespace NValidation.Testing.Internals
{
    internal static class ValidationAssertionMessage
    {
        private const string AnyMessage = "(any message)";
        private const string NoErrors = "(no errors)";

        /// <summary>
        /// Describes <paramref name="match"/>, using <paramref name="subject"/> to name what was
        /// validated (e.g. <c>"validation result"</c>).
        /// </summary>
        public static string Build(
            string subject,
            IReadOnlyList<ExpectedError> expected,
            IReadOnlyList<ValidationError> actual,
            MatchResult match)
        {
            var message = new StringBuilder();

            if (expected.Count == 0)
            {
                // Nothing was expected, so every error is unexpected and a diff would only repeat the list.
                message
                    .Append("Expected the ").Append(subject).Append(" to succeed, but it reported ")
                    .Append(Count(actual.Count)).AppendLine(":");
                AppendRows(message, actual.Select(Row).ToArray());

                return message.ToString().TrimEnd();
            }

            if (actual.Count == 0)
            {
                // The mirror image: nothing came back, so naming each expectation as missing adds nothing.
                message
                    .Append("Expected the ").Append(subject).Append(" to report ")
                    .Append(Count(expected.Count)).AppendLine(":");
                AppendRows(message, expected.Select(Row).ToArray());
                message.Append("but it succeeded.");

                return message.ToString();
            }

            if (expected.Count == 1 && actual.Count == 1)
            {
                // The commonest shape by far. Two one-element lists read better side by side than as a diff.
                message.Append("Expected the ").Append(subject).AppendLine(" to report one error:");
                AppendRows(message, [Row(expected[0])]);
                message.AppendLine("but it reported:");
                AppendRows(message, [Row(actual[0])]);

                return message.ToString().TrimEnd();
            }

            message
                .Append("Expected the ").Append(subject).Append(" to report exactly ")
                .Append(Count(expected.Count)).AppendLine(":");
            AppendRows(message, expected.Select(Row).ToArray());

            message.Append("but it reported ").Append(Count(actual.Count)).AppendLine(":");
            AppendRows(message, actual.Select(Row).ToArray());

            AppendNotReported(message, match);
            AppendNotExpected(message, match);

            return message.ToString().TrimEnd();
        }

        private static void AppendNotReported(StringBuilder message, MatchResult match)
        {
            if (match.UnmatchedExpectations.Count == 0)
            {
                return;
            }

            var rows = new (string PropertyName, string Message, string Reason)[match.UnmatchedExpectations.Count];

            for (var index = 0; index < rows.Length; index++)
            {
                var expectation = match.UnmatchedExpectations[index];
                var reason = match.NearMisses[index]
                    ? $"(an error was reported under \"{expectation.PropertyName}\", but its message differs)"
                    : $"(nothing was reported under \"{expectation.PropertyName}\")";

                var (propertyName, text) = Row(expectation);

                rows[index] = (propertyName, text, reason);
            }

            message.AppendLine().AppendLine("Not reported:");

            var propertyNameWidth = rows.Max(row => row.PropertyName.Length);
            var messageWidth = rows.Max(row => row.Message.Length);

            foreach (var row in rows)
            {
                message
                    .Append("  ").Append(row.PropertyName.PadRight(propertyNameWidth))
                    .Append("  ").Append(row.Message.PadRight(messageWidth))
                    .Append("  ").AppendLine(row.Reason);
            }
        }

        private static void AppendNotExpected(StringBuilder message, MatchResult match)
        {
            if (match.UnmatchedErrors.Count == 0)
            {
                return;
            }

            message.AppendLine().AppendLine("Not expected:");
            AppendRows(message, match.UnmatchedErrors.Select(Row).ToArray());
        }

        private static void AppendRows(StringBuilder message, IReadOnlyList<(string PropertyName, string Message)> rows)
        {
            if (rows.Count == 0)
            {
                message.Append("  ").AppendLine(NoErrors);

                return;
            }

            var propertyNameWidth = rows.Max(row => row.PropertyName.Length);

            foreach (var row in rows)
            {
                message.Append("  ").Append(row.PropertyName.PadRight(propertyNameWidth)).Append("  ").AppendLine(row.Message);
            }
        }

        private static (string PropertyName, string Message) Row(ExpectedError expected)
        {
            return (expected.PropertyName, expected.Match == ExpectedMessage.Any ? AnyMessage : Quote(expected.Message!));
        }

        private static (string PropertyName, string Message) Row(ValidationError error)
        {
            return (error.PropertyName, Quote(error.Message));
        }

        private static string Count(int count)
        {
            return count == 1 ? "one error" : $"{count} errors";
        }

        private static string Quote(string value)
        {
            return $"\"{value}\"";
        }
    }
}
