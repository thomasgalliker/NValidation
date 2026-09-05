using Microsoft.AspNetCore.Mvc;
using NValidation.AspNetCore;
using NValidation.TestData;

namespace NValidation.SampleApi.Controllers
{
    /// <summary>
    /// The same domain as the minimal API endpoints in <c>Program.cs</c>, validated the other way round:
    /// there, each handler calls its validator; here, <see cref="ValidationActionFilter"/> has already
    /// done it by the time an action runs.
    /// </summary>
    [ApiController]
    [Route("api/cars")]
    public sealed class CarsController : ControllerBase
    {
        private readonly IValidator<CarValuation> carValuationValidator;

        public CarsController(IValidator<CarValuation> carValuationValidator)
        {
            this.carValuationValidator = carValuationValidator;
        }

        /// <summary>
        /// Holds no validation code at all: the filter resolved <c>IValidator&lt;Car&gt;</c> and ran it,
        /// and an invalid car never reached here.
        /// </summary>
        /// <remarks>
        /// The payload nests a model and a manufacturer, so a failure further down is reported under
        /// <c>Model.Manufacturer.Name</c> — the code a form binds its message to.
        /// </remarks>
        [HttpPost("")]
        public ActionResult<string> Create(Car car)
        {
            return this.Ok(car.Vin);
        }

        /// <summary>
        /// The car id is bound from the route, which is where the request was addressed rather than what
        /// it carried, so the filter validates only the body.
        /// </summary>
        [HttpPut("{carId:int}")]
        public IActionResult Update(int carId, Car car)
        {
            return this.Ok(new { carId, car.Vin });
        }

        /// <summary>
        /// A legacy endpoint which answers in its own error shape, because clients in the field parse
        /// that shape. It is excluded from the filter and runs the very same validator itself, so the
        /// rules stay in one place while the response does not change.
        /// </summary>
        [HttpPost("valuations")]
        public async Task<IActionResult> ValuateAsync(
            [SkipValidation("Answers in a legacy error shape which deployed clients parse; validated explicitly below.")]
            CarValuation carValuation,
            CancellationToken cancellationToken)
        {
            var validationResult = await this.carValuationValidator.ValidateAsync(carValuation, cancellationToken);
            if (!validationResult.Succeeded)
            {
                return this.BadRequest(new
                {
                    error = "invalid_request",
                    error_description = string.Join(" ", validationResult.Errors.Select(e => e.Message)),
                });
            }

            return this.Ok(new { estimate = 12_500 });
        }

        /// <summary>
        /// Its payload has no validator, which is what
        /// <see cref="MissingValidatorBehavior"/> decides about: ignore it, log it, or refuse to serve the
        /// request until somebody either writes the validator or says why there is none.
        /// </summary>
        [HttpPost("imports")]
        public IActionResult Import(CarImport carImport)
        {
            return this.Accepted(new { carImport.SourceSystem });
        }
    }
}
