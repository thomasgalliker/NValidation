using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// The throwing path, for a host with no exception-to-problem-details pipeline of its own: a service
    /// validates by throwing and never mentions HTTP, and the response still comes out as RFC7807.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationExceptionHandlerTests
    {
        [Fact]
        public async Task TryHandleAsync_WritesABadRequestProblemDetails()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var handler = CreateHandler(httpContext);
            var validationException = new ValidationException(
                ValidationResult.FromValidationErrors(new ValidationError("Vin", "The VIN is required.")));

            // Act
            var handled = await handler.TryHandleAsync(httpContext, validationException, CancellationToken.None);
            var body = await ReadBodyAsync(httpContext);

            // Assert
            handled.Should().BeTrue();
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            var errors = body.GetProperty("errors");
            errors.GetProperty("Vin").EnumerateArray().Select(message => message.GetString())
                .Should().ContainSingle().Which.Should().Be("The VIN is required.");
        }

        /// <summary>
        /// The error codes are the C# property names, and they must survive serialization verbatim —
        /// a client binds its inputs to them.
        /// </summary>
        [Fact]
        public async Task TryHandleAsync_WritesTheErrorCodesVerbatim()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var handler = CreateHandler(httpContext);
            var validationException = new ValidationException(
                ValidationResult.FromValidationErrors(new ValidationError("Model.Manufacturer.Name", "The name is required.")));

            // Act
            await handler.TryHandleAsync(httpContext, validationException, CancellationToken.None);
            var body = await ReadBodyAsync(httpContext);

            // Assert
            body.GetProperty("errors").TryGetProperty("Model.Manufacturer.Name", out _).Should().BeTrue();
        }

        [Fact]
        public async Task TryHandleAsync_LeavesAnyOtherExceptionToTheNextHandler()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var handler = CreateHandler(httpContext);

            // Act
            var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("something else"), CancellationToken.None);

            // Assert
            handled.Should().BeFalse();
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK, "the response must be left untouched");
        }

        /// <summary>
        /// A problem details service can decline to write — a host may have configured it away. Leaving
        /// the 400 behind would hand the next handler a response already blaming the caller for a
        /// failure nobody has reported yet.
        /// </summary>
        [Fact]
        public async Task TryHandleAsync_WhenNothingIsWritten_LeavesTheStatusCodeAlone()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var handler = new ValidationExceptionHandler(new DecliningProblemDetailsService());
            var validationException = new ValidationException(
                ValidationResult.FromValidationErrors(new ValidationError("Vin", "The VIN is required.")));

            // Act
            var handled = await handler.TryHandleAsync(httpContext, validationException, CancellationToken.None);

            // Assert
            handled.Should().BeFalse();
            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        private sealed class DecliningProblemDetailsService : IProblemDetailsService
        {
            public ValueTask WriteAsync(ProblemDetailsContext context)
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
            {
                return ValueTask.FromResult(false);
            }
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProblemDetails();

            return new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider(),
                Response = { Body = new MemoryStream() },
            };
        }

        private static ValidationExceptionHandler CreateHandler(HttpContext httpContext)
        {
            return new ValidationExceptionHandler(httpContext.RequestServices.GetRequiredService<IProblemDetailsService>());
        }

        private static async Task<JsonElement> ReadBodyAsync(HttpContext httpContext)
        {
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

            var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
            return document.RootElement.Clone();
        }
    }
}
