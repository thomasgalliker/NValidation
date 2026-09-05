using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace NValidation.AspNetCore
{
    /// <summary>
    /// Turns an unhandled <see cref="ValidationException"/> into a 400 problem details response, so a
    /// service can validate by throwing and never mention HTTP in its own code.
    /// </summary>
    /// <remarks>
    /// Register it alongside the problem details service, which writes the response:
    /// <code>
    /// services.AddProblemDetails();
    /// services.AddExceptionHandler&lt;ValidationExceptionHandler&gt;();
    /// </code>
    /// Any other exception is left untouched for the next handler in the chain. A host which already has
    /// its own exception-to-problem-details handler should read
    /// <see cref="ValidationException.Errors"/> there instead of registering this one, so every error
    /// response keeps going through one place.
    /// </remarks>
    public sealed class ValidationExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService problemDetailsService;

        public ValidationExceptionHandler(IProblemDetailsService problemDetailsService)
        {
            ArgumentNullException.ThrowIfNull(problemDetailsService);

            this.problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            if (exception is not ValidationException validationException)
            {
                return false;
            }

            // The status code is set on the response as well as in the body: the problem details service
            // writes the body, but the response code is what the client actually reacts to.
            var statusCodeBefore = httpContext.Response.StatusCode;
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            var handled = await this.problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = validationException,
                ProblemDetails = validationException.ToProblemDetails(),
            });

            if (!handled)
            {
                // Nothing was written, so this handler did not handle it. Leaving a 400 behind would
                // hand the next handler — or the host's own fallback — a response already claiming the
                // failure was the caller's.
                httpContext.Response.StatusCode = statusCodeBefore;
            }

            return handled;
        }
    }
}
