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
        /// <paramref name="code"/>. The message is matched with wildcards — see
        /// <see cref="ExpectedError"/>.
        /// </summary>
        public static void ShouldReport(this ValidationResult result, string code, string message)
        {
            result.ShouldReport([new ExpectedError(code, message)]);
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
        /// <paramref name="message"/>, reported under <paramref name="code"/>.
        /// </summary>
        public static void ShouldReport(this ValidationException exception, string code, string message)
        {
            exception.ShouldReport([new ExpectedError(code, message)]);
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
        /// reported under <paramref name="code"/>.
        /// </summary>
        public static void ShouldReport<TMessages>(
            this IReadOnlyDictionary<string, TMessages> errors,
            string code,
            string message)
            where TMessages : IEnumerable<string>
        {
            errors.ShouldReport([new ExpectedError(code, message)]);
        }

        /// <summary>
        /// Asserts that a <c>{ code: [messages] }</c> shape — <see cref="ValidationResult.ToErrorsDictionary"/>,
        /// <see cref="ValidationException.Errors"/>, or the <c>errors</c> member of a problem-details
        /// response — holds exactly <paramref name="expected"/>, in any order.
        /// </summary>
        /// <remarks>
        /// A code carrying several messages counts as several failures, so the grouping makes no
        /// difference to what has to be expected.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="errors"/> or <paramref name="expected"/> is <c>null</c>.</exception>
        /// <exception cref="ValidationAssertionException">The errors are not what was expected.</exception>
        public static void ShouldReport<TMessages>(
            this IReadOnlyDictionary<string, TMessages> errors,
            IEnumerable<ExpectedError> expected)
            where TMessages : IEnumerable<string>
        {
            ArgumentNullException.ThrowIfNull(errors);

            Assert("validation errors", Flatten(errors), expected);
        }

        private static void Assert(
            string subject,
            IReadOnlyList<ValidationError> actual,
            IEnumerable<ExpectedError> expected)
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

        /// <summary>
        /// Turns a <c>{ code: [messages] }</c> shape back into the flat list of failures it was grouped
        /// from, so every overload is matched the same way.
        /// </summary>
        private static IReadOnlyList<ValidationError> Flatten<TMessages>(IReadOnlyDictionary<string, TMessages> errors)
            where TMessages : IEnumerable<string>
        {
            return errors
                .SelectMany(entry => entry.Value.Select(message => new ValidationError(entry.Key, message)))
                .ToArray();
        }
    }
}
