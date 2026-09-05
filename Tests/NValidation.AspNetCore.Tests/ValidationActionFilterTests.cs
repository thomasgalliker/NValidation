using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NValidation.AspNetCore.Tests.TestData;

namespace NValidation.AspNetCore.Tests
{
    /// <summary>
    /// Which of an action's payloads the filter validates, and what it does with one it cannot.
    /// </summary>
    [Trait(Traits.Category, Traits.UnitTests)]
    public class ValidationActionFilterTests
    {
        private static Car ValidCar => Cars.Car();

        private static Car InvalidCar
        {
            get
            {
                var car = Cars.Car();
                car.Vin = "TOO-SHORT";

                return car;
            }
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAValidPayload_RunsTheAction()
        {
            // Arrange
            var context = CreateContext(nameof(TestActions.Create), new Dictionary<string, object?> { ["car"] = ValidCar });
            var filter = CreateFilter();
            var actionWasRun = false;

            // Act
            await filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            actionWasRun.Should().BeTrue();
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAnInvalidPayload_ThrowsBeforeTheAction()
        {
            // Arrange
            var context = CreateContext(nameof(TestActions.Create), new Dictionary<string, object?> { ["car"] = InvalidCar });
            var filter = CreateFilter();
            var actionWasRun = false;

            // Act
            var act = () => filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            var validationException = (await act.Should().ThrowAsync<ValidationException>()).Which;
            validationException.Errors.Should().ContainKey(nameof(Car.Vin));
            actionWasRun.Should().BeFalse();
        }

        /// <summary>
        /// The filter iterates every payload of the action, so one request reports everything that is
        /// wrong with it rather than only the first failure.
        /// </summary>
        [Fact]
        public async Task OnActionExecutionAsync_WithSeveralInvalidPayloads_ReportsThemTogether()
        {
            // Arrange
            var arguments = new Dictionary<string, object?>
            {
                ["car"] = InvalidCar,
                ["manufacturer"] = new Manufacturer { Name = null },
            };
            var context = CreateContext(nameof(TestActions.CreateWithManufacturer), arguments);
            var filter = CreateFilter();

            // Act
            var act = () => filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

            // Assert
            var validationException = (await act.Should().ThrowAsync<ValidationException>()).Which;
            validationException.Errors.Should().ContainKeys(nameof(Car.Vin), nameof(Manufacturer.Name));
        }

        /// <summary>
        /// An absent body binds as null. Validating it would report a missing payload as a server error,
        /// because a validator rejects a null instance rather than failing it.
        /// </summary>
        [Fact]
        public async Task OnActionExecutionAsync_WithANullPayload_RunsTheAction()
        {
            // Arrange
            var context = CreateContext(nameof(TestActions.Create), new Dictionary<string, object?> { ["car"] = null });
            var filter = CreateFilter();
            var actionWasRun = false;

            // Act
            await filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            actionWasRun.Should().BeTrue();
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAnUnboundPayload_RunsTheAction()
        {
            // Arrange
            var context = CreateContext(nameof(TestActions.Create), new Dictionary<string, object?>());
            var filter = CreateFilter();
            var actionWasRun = false;

            // Act
            await filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            actionWasRun.Should().BeTrue();
        }

        /// <summary>
        /// A route value is where the request was addressed, not what it carried, so it is not a payload
        /// even when a validator happens to be registered for its type.
        /// </summary>
        [Fact]
        public async Task OnActionExecutionAsync_WithARouteBoundParameter_DoesNotValidateIt()
        {
            // Arrange
            var context = CreateContext(
                nameof(TestActions.Create),
                new Dictionary<string, object?> { ["car"] = InvalidCar },
                BindingSource.Path);
            var filter = CreateFilter();

            // Act
            await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

            // Assert
            // Reaching here is the assertion: an invalid payload did not fail the request.
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAFormBoundPayload_ValidatesIt()
        {
            // Arrange
            var context = CreateContext(
                nameof(TestActions.Create),
                new Dictionary<string, object?> { ["car"] = InvalidCar },
                BindingSource.Form);
            var filter = CreateFilter();

            // Act
            var act = () => filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithoutAValidator_AndIgnore_RunsTheAction()
        {
            // Arrange
            var context = CreateContext(nameof(TestActions.Import), new Dictionary<string, object?> { ["carImport"] = new CarImport() });
            var filter = CreateFilter(MissingValidatorBehavior.Ignore);
            var actionWasRun = false;

            // Act
            await filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            actionWasRun.Should().BeTrue();
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithoutAValidator_AndLog_WarnsAndRunsTheAction()
        {
            // Arrange
            var logger = new CapturingLogger();
            var context = CreateContext(nameof(TestActions.Import), new Dictionary<string, object?> { ["carImport"] = new CarImport() });
            var filter = CreateFilter(MissingValidatorBehavior.Log, logger);
            var actionWasRun = false;

            // Act
            await filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            actionWasRun.Should().BeTrue();
            var warning = logger.Entries.Should().ContainSingle().Which;
            warning.LogLevel.Should().Be(LogLevel.Warning);
            warning.Message.Should().Contain(nameof(CarImport)).And.Contain("carImport");
        }

        /// <summary>
        /// Whether a payload has a validator is a property of the action, not of the request, so a
        /// second request to the same action adds nothing but noise to the log.
        /// </summary>
        [Fact]
        public async Task OnActionExecutionAsync_WithoutAValidator_AndLog_WarnsOncePerAction()
        {
            // Arrange
            var logger = new CapturingLogger();
            var context = CreateContext(nameof(TestActions.Import), new Dictionary<string, object?> { ["carImport"] = new CarImport() });
            var filter = CreateFilter(MissingValidatorBehavior.Log, logger);

            // Act
            await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));
            await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

            // Assert
            logger.Entries.Should().ContainSingle("the second request has nothing new to report");
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithoutAValidator_AndThrow_Throws()
        {
            // Arrange
            var context = CreateContext(nameof(TestActions.Import), new Dictionary<string, object?> { ["carImport"] = new CarImport() });
            var filter = CreateFilter(MissingValidatorBehavior.Throw);

            // Act
            var act = () => filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

            // Assert
            var exception = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;
            exception.Message.Should().Contain(nameof(CarImport)).And.Contain("[SkipValidation]");
        }

        /// <summary>
        /// The behaviour reports payloads nobody thought about. One which is marked as deliberately
        /// unvalidated was thought about, so it stays silent even at the strictest setting.
        /// </summary>
        [Fact]
        public async Task OnActionExecutionAsync_WithoutAValidator_ButSkipped_DoesNotApplyTheBehaviour()
        {
            // Arrange
            var context = CreateContext(
                nameof(TestActions.ImportSkippedParameter),
                new Dictionary<string, object?> { ["carImport"] = new CarImport() });
            var filter = CreateFilter(MissingValidatorBehavior.Throw);
            var actionWasRun = false;

            // Act
            await filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            actionWasRun.Should().BeTrue();
        }

        [Theory]
        [InlineData(nameof(TestActions.CreateSkippedParameter))]
        [InlineData(nameof(TestActions.CreateSkippedAction))]
        [InlineData(nameof(TestActions.CreateSkippedParameterWithoutAReason))]
        public async Task OnActionExecutionAsync_WithSkipValidation_DoesNotValidate(string actionName)
        {
            // Arrange
            var context = CreateContext(actionName, new Dictionary<string, object?> { ["car"] = InvalidCar });
            var filter = CreateFilter();
            var actionWasRun = false;

            // Act
            await filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            actionWasRun.Should().BeTrue();
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithSkipValidationOnTheController_DoesNotValidate()
        {
            // Arrange
            var context = CreateContext(
                nameof(SkippedTestActions.Create),
                new Dictionary<string, object?> { ["car"] = InvalidCar },
                controllerType: typeof(SkippedTestActions));
            var filter = CreateFilter();
            var actionWasRun = false;

            // Act
            await filter.OnActionExecutionAsync(context, () =>
            {
                actionWasRun = true;
                return Task.FromResult(CreateExecutedContext(context));
            });

            // Assert
            actionWasRun.Should().BeTrue();
        }

        private static ValidationActionFilter CreateFilter(
            MissingValidatorBehavior missingValidatorBehavior = MissingValidatorBehavior.Ignore,
            ILogger<ValidationActionFilter>? logger = null)
        {
            var options = Options.Create(new ValidationFilterOptions { MissingValidatorBehavior = missingValidatorBehavior });

            return new ValidationActionFilter(
                options, new EmptyModelMetadataProvider(), logger ?? NullLogger<ValidationActionFilter>.Instance);
        }

        private static ActionExecutingContext CreateContext(
            string actionName,
            IDictionary<string, object?> actionArguments,
            BindingSource? bindingSource = null,
            Type? controllerType = null)
        {
            controllerType ??= typeof(TestActions);
            bindingSource ??= BindingSource.Body;

            var actionMethod = controllerType.GetMethod(actionName)!;

            var serviceProvider = new ServiceCollection()
                .AddNValidation(o => o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly))
                .BuildServiceProvider();

            var actionDescriptor = new ControllerActionDescriptor
            {
                DisplayName = $"{controllerType.Name}.{actionName}",
                MethodInfo = actionMethod,
                ControllerTypeInfo = controllerType.GetTypeInfo(),
                Parameters = actionMethod.GetParameters()
                    .Select(parameter => (ParameterDescriptor)new ControllerParameterDescriptor
                    {
                        Name = parameter.Name!,
                        ParameterType = parameter.ParameterType,
                        ParameterInfo = parameter,
                        // A primitive is addressed from the route in a real application; the payload
                        // carries whichever source the test is exercising.
                        BindingInfo = new BindingInfo
                        {
                            BindingSource = parameter.ParameterType.IsPrimitive ? BindingSource.Path : bindingSource,
                        },
                    })
                    .ToList(),
                EndpointMetadata =
                [
                    .. actionMethod.GetCustomAttributes(inherit: true),
                    .. controllerType.GetCustomAttributes(inherit: true),
                ],
            };

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            return new ActionExecutingContext(
                new ActionContext(httpContext, new RouteData(), actionDescriptor),
                new List<IFilterMetadata>(),
                actionArguments,
                controller: null!);
        }

        private static ActionExecutedContext CreateExecutedContext(ActionExecutingContext context)
        {
            return new ActionExecutedContext(context, context.Filters, context.Controller);
        }

        private sealed class CapturingLogger : ILogger<ValidationActionFilter>
        {
            public List<(LogLevel LogLevel, string Message)> Entries { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                this.Entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
