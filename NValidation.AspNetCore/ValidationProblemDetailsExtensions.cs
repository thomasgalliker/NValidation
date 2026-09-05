using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NValidation.AspNetCore
{
    /// <summary>
    /// Maps a validation failure onto the RFC7807 shape: a 400 problem details whose top-level
    /// <c>errors</c> member holds the messages grouped by <see cref="ValidationError.Code"/>.
    /// </summary>
    /// <remarks>
    /// Only <c>errors</c> is filled in. Title, Detail, Type and the trace identifier are deliberately
    /// left to the host, so a response built here is indistinguishable from the ones its own problem
    /// details pipeline produces.
    /// </remarks>
    public static class ValidationProblemDetailsExtensions
    {
        /// <summary>
        /// The problem details for a failed <paramref name="validationResult"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="validationResult"/> succeeded. A problem details response always reports at
        /// least one error, so a successful result has nothing to report.
        /// </exception>
        public static ProblemDetails ToProblemDetails(this ValidationResult validationResult)
        {
            ArgumentNullException.ThrowIfNull(validationResult);

            if (validationResult.Succeeded)
            {
                throw new ArgumentException("A validation problem result requires at least one error.", nameof(validationResult));
            }

            return Create(validationResult.ToErrorsDictionary());
        }

        /// <summary>
        /// The problem details for a <paramref name="validationException"/>, for the path where the
        /// failure was thrown rather than returned.
        /// </summary>
        public static ProblemDetails ToProblemDetails(this ValidationException validationException)
        {
            ArgumentNullException.ThrowIfNull(validationException);

            return Create(validationException.Errors);
        }

        private static ProblemDetails Create<TMessages>(IReadOnlyDictionary<string, TMessages> errors)
            where TMessages : IEnumerable<string>
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Extensions =
                {
                    ["errors"] = errors
                }
            };

            return problemDetails;
        }
    }
}
