namespace NValidation.Testing.Internals
{
    /// <summary>
    /// Pairs what a test expected with what a validator reported, one expectation to one error.
    /// </summary>
    /// <remarks>
    /// An expectation whose message is a wildcard can fit several of the errors, so pairing greedily
    /// would report a failure where a different pairing fits — <c>["Vin", ("Vin", "*required*")]</c>
    /// against a required-VIN failure and a too-long-VIN failure is the case that breaks. This searches
    /// for a complete pairing instead, by the standard augmenting-path walk: whenever an expectation
    /// finds only errors that are taken, it asks each of those errors' holders to move, and moves in if
    /// one of them can.
    /// </remarks>
    internal static class ExpectedErrorMatcher
    {
        /// <summary>
        /// Pairs as many expectations as possible with the errors that satisfy them, and returns what
        /// was left over on each side.
        /// </summary>
        public static MatchResult Match(IReadOnlyList<ExpectedError> expected, IReadOnlyList<ValidationError> actual)
        {
            // errorForExpectation[e] is the error paired with expectation e, and -1 when it has none.
            var errorForExpectation = new int[expected.Count];
            var expectationForError = new int[actual.Count];

            Array.Fill(errorForExpectation, -1);
            Array.Fill(expectationForError, -1);

            for (var expectationIndex = 0; expectationIndex < expected.Count; expectationIndex++)
            {
                var visited = new bool[actual.Count];

                TryPair(expectationIndex, expected, actual, errorForExpectation, expectationForError, visited);
            }

            var unmatchedExpectations = new List<ExpectedError>();
            var nearMisses = new List<bool>();

            for (var expectationIndex = 0; expectationIndex < expected.Count; expectationIndex++)
            {
                if (errorForExpectation[expectationIndex] >= 0)
                {
                    continue;
                }

                var expectation = expected[expectationIndex];

                unmatchedExpectations.Add(expectation);
                nearMisses.Add(actual.Any(error => string.Equals(error.PropertyName, expectation.PropertyName, StringComparison.Ordinal)));
            }

            var unmatchedErrors = new List<ValidationError>();

            for (var errorIndex = 0; errorIndex < actual.Count; errorIndex++)
            {
                if (expectationForError[errorIndex] < 0)
                {
                    unmatchedErrors.Add(actual[errorIndex]);
                }
            }

            return new MatchResult(unmatchedExpectations, nearMisses, unmatchedErrors);
        }

        /// <summary>
        /// Finds a place for <paramref name="expectationIndex"/>, moving the expectations already in the
        /// way if they have somewhere else to go.
        /// </summary>
        private static bool TryPair(
            int expectationIndex,
            IReadOnlyList<ExpectedError> expected,
            IReadOnlyList<ValidationError> actual,
            int[] errorForExpectation,
            int[] expectationForError,
            bool[] visited)
        {
            for (var errorIndex = 0; errorIndex < actual.Count; errorIndex++)
            {
                if (visited[errorIndex] || !Satisfies(expected[expectationIndex], actual[errorIndex]))
                {
                    continue;
                }

                visited[errorIndex] = true;

                var holder = expectationForError[errorIndex];

                if (holder < 0 || TryPair(holder, expected, actual, errorForExpectation, expectationForError, visited))
                {
                    expectationForError[errorIndex] = expectationIndex;
                    errorForExpectation[expectationIndex] = errorIndex;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <c>true</c> when <paramref name="error"/> is one the <paramref name="expectation"/> asked for:
        /// the same property name, and a message the expectation's pattern matches.
        /// </summary>
        private static bool Satisfies(ExpectedError expectation, ValidationError error)
        {
            if (!string.Equals(expectation.PropertyName, error.PropertyName, StringComparison.Ordinal))
            {
                return false;
            }

            return expectation.Match switch
            {
                ExpectedMessage.Any => true,
                ExpectedMessage.Pattern => WildcardPattern.IsMatch(expectation.Message!, error.Message),
                _ => string.Equals(expectation.Message, error.Message, StringComparison.Ordinal),
            };
        }
    }
}
