using System.Diagnostics;
using NValidation.Testing.Internals;

namespace NValidation.Testing
{
    /// <summary>
    /// Asserts what a validator reported — the properties it blamed and the messages it chose, not merely
    /// that it failed. Depends on no test framework and no assertion library: a failure is a
    /// <see cref="ValidationAssertionException"/>, which every runner reports.
    /// </summary>
    /// <remarks>
    /// Every overload states the <em>whole</em> expected result: nothing else may be present and the count
    /// is implied, so a repeated entry asks for a repeated failure. Order is ignored, because the order
    /// errors arrive in is not part of what a validator promises.
    /// </remarks>
    [DebuggerNonUserCode]
    public static class ValidationAssertions
    {
        /// <summary>
        /// Asserts that the only failure is <paramref name="message"/>, reported under
        /// <paramref name="propertyName"/>. The message is matched exactly; for a pattern, pass
        /// <see cref="ExpectedError.Matching"/>.
        /// </summary>
        public static void ShouldReport(this ValidationResult result, string propertyName, string message)
        {
            result.ShouldReport([new ExpectedError(propertyName, message)]);
        }

        /// <summary>
        /// Asserts that <paramref name="result"/> reports exactly <paramref name="expected"/> — no more
        /// and no fewer, in any order.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="expected"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">The result is not what was expected.</exception>
        public static void ShouldReport(this ValidationResult result, IEnumerable<ExpectedError> expected)
        {
            ArgumentNullException.ThrowIfNull(result);

            Assert("validation result", result.Errors, expected);
        }

        /// <summary>
        /// Asserts that the only failure <paramref name="exception"/> carries is
        /// <paramref name="message"/>, reported under <paramref name="propertyName"/>.
        /// </summary>
        public static void ShouldReport(this ValidationException exception, string propertyName, string message)
        {
            exception.ShouldReport([new ExpectedError(propertyName, message)]);
        }

        /// <summary>
        /// Asserts that <paramref name="exception"/> carries exactly <paramref name="expected"/> — no more
        /// and no fewer, in any order.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> or <paramref name="expected"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">The exception does not carry what was expected.</exception>
        public static void ShouldReport(this ValidationException exception, IEnumerable<ExpectedError> expected)
        {
            ArgumentNullException.ThrowIfNull(exception);

            Assert("validation exception", Flatten(exception.Errors), expected);
        }

        /// <summary>
        /// Asserts that the only failure in <paramref name="errors"/> is <paramref name="message"/>,
        /// reported under <paramref name="propertyName"/>.
        /// </summary>
        public static void ShouldReport<TMessages>(
            this IEnumerable<KeyValuePair<string, TMessages>> errors,
            string propertyName,
            string message)
            where TMessages : IEnumerable<string>
        {
            errors.ShouldReport([new ExpectedError(propertyName, message)]);
        }

        /// <summary>
        /// Asserts that a <c>{ propertyName: [messages] }</c> shape — <see cref="ValidationResult.ToErrorsDictionary"/>,
        /// <see cref="ValidationException.Errors"/>, or the <c>errors</c> of a problem-details response —
        /// holds exactly <paramref name="expected"/>, in any order.
        /// </summary>
        /// <remarks>
        /// Taken as a sequence of pairs rather than as a dictionary, so it accepts every shape one of
        /// these arrives in: <see cref="Dictionary{TKey, TValue}"/>, <see cref="IDictionary{TKey, TValue}"/>
        /// (which is how the framework's own <c>HttpValidationProblemDetails.Errors</c> is typed) and
        /// <see cref="IReadOnlyDictionary{TKey, TValue}"/> alike. Overloads for two of those would make
        /// the third ambiguous. A property carrying several messages counts as several failures, so the
        /// grouping makes no difference to what has to be expected.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="errors"/> or <paramref name="expected"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">The errors are not what was expected.</exception>
        public static void ShouldReport<TMessages>(
            this IEnumerable<KeyValuePair<string, TMessages>> errors,
            IEnumerable<ExpectedError> expected)
            where TMessages : IEnumerable<string>
        {
            ArgumentNullException.ThrowIfNull(errors);

            Assert("validation errors", Flatten(errors), expected);
        }

        /// <summary>
        /// Asserts that the only failure is the one <paramref name="errorCode"/> names, reported under
        /// <paramref name="propertyName"/> — which rule fired, without depending on any wording.
        /// </summary>
        /// <remarks>
        /// The counterpart of <see cref="ShouldReport(ValidationResult, string, string)"/> for a test
        /// about the rule rather than the message. Every rule this core ships reports its own code, so
        /// this needs neither a second validator nor a second run.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Any argument is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">The result is not what was expected.</exception>
        public static void ShouldReportErrorCode(this ValidationResult result, string propertyName, string errorCode)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(propertyName);
            ArgumentNullException.ThrowIfNull(errorCode);

            var reported = result.Errors
                .Select(error => $"{error.PropertyName} -> {error.ErrorCode ?? "(no code)"}")
                .ToArray();

            if (result.Errors.Count == 1 &&
                string.Equals(result.Errors[0].PropertyName, propertyName, StringComparison.Ordinal) &&
                string.Equals(result.Errors[0].ErrorCode, errorCode, StringComparison.Ordinal))
            {
                return;
            }

            throw new ValidationAssertionException(
                $"Expected exactly one failure, \"{propertyName}\" reporting \"{errorCode}\", but the result " +
                (reported.Length == 0
                    ? "succeeded."
                    : $"reported {reported.Length}:{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", reported)));
        }

        private static void Assert(string subject, IReadOnlyList<ValidationError> actual, IEnumerable<ExpectedError> expected)
        {
            ArgumentNullException.ThrowIfNull(expected);

            var expectations = expected as IReadOnlyList<ExpectedError> ?? expected.ToArray();

            foreach (var expectation in expectations)
            {
                ArgumentNullException.ThrowIfNull(expectation);
            }

            var match = ExpectedErrorMatcher.Match(expectations, actual);

            if (match.Succeeded)
            {
                return;
            }

            throw new ValidationAssertionException(
                ValidationAssertionMessage.Build(subject, expectations, actual, match));
        }

        private static IReadOnlyList<ValidationError> Flatten<TMessages>(IEnumerable<KeyValuePair<string, TMessages>> errors)
            where TMessages : IEnumerable<string>
        {
            return errors
                .SelectMany(entry => entry.Value.Select(message => new ValidationError(entry.Key, message)))
                .ToArray();
        }
    }
}
