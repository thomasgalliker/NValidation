namespace NValidation.Testing.Internals
{
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

        public IReadOnlyList<ExpectedError> UnmatchedExpectations { get; }

        /// <summary>
        /// Parallel to <see cref="UnmatchedExpectations"/>: <c>true</c> where an error was reported under
        /// that property name but carried a different message. That is the near miss a reader wants named, since
        /// it separates "the wrong rule fired" from "no rule fired".
        /// </summary>
        public IReadOnlyList<bool> NearMisses { get; }

        public IReadOnlyList<ValidationError> UnmatchedErrors { get; }

        public bool Succeeded => this.UnmatchedExpectations.Count == 0 && this.UnmatchedErrors.Count == 0;
    }
}
