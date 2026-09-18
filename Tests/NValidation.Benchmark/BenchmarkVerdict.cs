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
            var reportedByNValidation = nValidator.ValidateAsync(payload).GetAwaiter().GetResult()
                .Errors.Select(error => error.PropertyName).Order().ToArray();

            var reportedByFluentValidation = fluentValidator.Validate(payload)
                .Errors.Select(failure => failure.PropertyName).Order().ToArray();

            if (!reportedByNValidation.SequenceEqual(reportedByFluentValidation, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The two validators of {typeof(T).Name} do not agree, so comparing them would be " +
                    $"meaningless. NValidation reported [{string.Join(", ", reportedByNValidation)}] and " +
                    $"FluentValidation reported [{string.Join(", ", reportedByFluentValidation)}].");
            }
        }
    }
}
