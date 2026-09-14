namespace NValidation.Testing.Internals
{
    /// <summary>
    /// What <see cref="ExpectedErrorMatcher"/> could not pair: the expectations nothing satisfied and the
    /// errors nothing asked for. Both empty means the result is exactly what the test expected.
    /// </summary>
    internal sealed class MatchResult
    {
        public MatchResult(
            IReadOnlyList<ExpectedError> unmatchedExpectations,
            IReadOnlyList<bool> nearMisses,
            IReadOnlyList<ValidationError> unmatchedErrors)
        {
            this.UnmatchedExpectations = unmatchedExpectations;
            this.NearMisses = nearMisses;
            this.UnmatchedErrors = unmatchedErrors;
        }

        /// <summary>
        /// The expectations no error satisfied.
        /// </summary>
        public IReadOnlyList<ExpectedError> UnmatchedExpectations { get; }

        /// <summary>
        /// Parallel to <see cref="UnmatchedExpectations"/>: <c>true</c> where an error was reported under
        /// that code but carried a different message. That is the near miss a reader wants named, since
        /// it separates "the wrong rule fired" from "no rule fired".
        /// </summary>
        public IReadOnlyList<bool> NearMisses { get; }

        /// <summary>
        /// The errors no expectation asked for.
        /// </summary>
        public IReadOnlyList<ValidationError> UnmatchedErrors { get; }

        /// <summary>
        /// <c>true</c> when every expectation was paired with an error and no error was left over.
        /// </summary>
        public bool Succeeded => this.UnmatchedExpectations.Count == 0 && this.UnmatchedErrors.Count == 0;
    }
}
