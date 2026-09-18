using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NValidation.AspNetCore
{
    /// <summary>
    /// Lets an action return a validation failure directly as an <c>ActionResult</c> instead of throwing
    /// (the counterpart to <see cref="ValidationResult.ThrowIfInvalid"/>).
    /// </summary>
    public static class ValidationProblemControllerExtensions
    {
        /// <summary>
        /// Returns a 400 Bad Request problem details response carrying <paramref name="validationResult"/>'s
        /// per-field errors.
        /// </summary>
        /// <remarks>
        /// The result is a plain <see cref="ObjectResult"/>, so a host which post-processes error results —
        /// to add a localized Title/Detail or a trace identifier, say — sees this one exactly as it sees
        /// <c>this.Problem(...)</c> or <c>this.BadRequest(...)</c>.
        /// </remarks>
        public static ObjectResult ValidationProblem(this ControllerBase controller, ValidationResult validationResult)
        {
            ArgumentNullException.ThrowIfNull(controller);

            return new ObjectResult(validationResult.ToProblemDetails())
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }
    }
}
