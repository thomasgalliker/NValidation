using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NValidation.AspNetCore
{
    /// <summary>
    /// Maps a validation failure onto the RFC7807 shape: a 400 problem details whose top-level
    /// <c>errors</c> member holds the messages grouped by <see cref="ValidationError.PropertyName"/>.
    /// </summary>
    /// <remarks>
    /// Only <c>errors</c> is filled in. Title, Detail, Type and the trace identifier are deliberately
    /// left to the host, so a response built here is indistinguishable from the ones its own problem
    /// details pipeline produces.
    /// <para>
    /// The concrete type is <see cref="HttpValidationProblemDetails"/> rather than a
    /// <see cref="ProblemDetails"/> carrying a dictionary under <c>Extensions["errors"]</c>. The two
    /// serialize alike through the reflection-based serializer, but an extension member is typed
    /// <see cref="object"/>, so writing one needs the runtime type resolved at serialization time —
    /// which a published-ahead-of-time host, serializing through a source-generated context, cannot do.
    /// <see cref="HttpValidationProblemDetails"/> is a framework type its own context already knows.
    /// </para>
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
        public static HttpValidationProblemDetails ToProblemDetails(this ValidationResult validationResult)
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
        public static HttpValidationProblemDetails ToProblemDetails(this ValidationException validationException)
        {
            ArgumentNullException.ThrowIfNull(validationException);

            return Create(validationException.Errors);
        }

        private static HttpValidationProblemDetails Create(IReadOnlyDictionary<string, string[]> errors)
        {
            var byProperty = new Dictionary<string, string[]>(errors.Count, StringComparer.Ordinal);

            foreach (var (propertyName, messages) in errors)
            {
                byProperty[propertyName] = messages;
            }

            return new HttpValidationProblemDetails(byProperty)
            {
                Status = StatusCodes.Status400BadRequest,

                // The constructor fills one in. Leaving it would make this the one error response in the
                // application whose title the host did not choose.
                Title = null,
            };
        }
    }
}
