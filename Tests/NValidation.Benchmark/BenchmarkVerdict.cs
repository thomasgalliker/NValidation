using FV = FluentValidation;

namespace NValidation.Benchmark
{
    /// <summary>
    /// Holds a comparison honest: both validators must blame the same properties before anything is
    /// measured, or the numbers describe different work.
    /// </summary>
    /// <remarks>
    /// Property names are compared rather than messages: the wording is each library's own, and holding
    /// them to identical text would be a maintenance tax that says nothing about speed.
    /// </remarks>
    internal static class BenchmarkVerdict
    {
        public static void RequireTheSame<T>(
            NValidation.IValidator<T> nValidator, FV.IValidator<T> fluentValidator, T payload)
        {
            RequireTheSame(nValidator.ValidateAsync(payload), fluentValidator.Validate(payload), typeof(T).Name);
        }

        /// <summary>
        /// The same for a comparison whose calls pass something of their own — a selection, data the
        /// rules read — so the two sides are handed over as what they returned.
        /// </summary>
        public static void RequireTheSame(
            ValueTask<ValidationResult> nValidationResult, FV.Results.ValidationResult fluentValidationResult, string scenario)
        {
            var reportedByNValidation = nValidationResult.GetAwaiter().GetResult()
                .Errors.Select(error => error.PropertyName).Order().ToArray();

            var reportedByFluentValidation = fluentValidationResult
                .Errors.Select(failure => failure.PropertyName).Order().ToArray();

            if (!reportedByNValidation.SequenceEqual(reportedByFluentValidation, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The two validators do not agree on {scenario}, so comparing them would be " +
                    $"meaningless. NValidation reported [{string.Join(", ", reportedByNValidation)}] and " +
                    $"FluentValidation reported [{string.Join(", ", reportedByFluentValidation)}].");
            }
        }
    }
}
