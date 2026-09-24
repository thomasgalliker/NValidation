using NValidation;
using NValidation.AspNetCore;
using NValidation.TestData;
using NValidation.TestData.Validators;

var builder = WebApplication.CreateBuilder(args);

// Everything this library needs is configured in one place. How long a validator lives and how much
// it reports come from appsettings.json, so they can be changed without a rebuild; what is registered
// stays in code, because naming an assembly in a settings file turns a typo into a payload that is
// silently never validated.
builder.Services.AddNValidation(builder.Configuration.GetSection("NValidation"), o =>
{
    // Every validator in this assembly is found and registered, along with whatever each one depends
    // on — CarValidator takes an IValidator<CarModel>, which in turn takes an IValidator<Manufacturer>.
    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);

    // Controllers validate their payloads without saying so: the filter resolves the validator for
    // every body- and form-bound parameter and runs it before the action.
    //
    // Ignore | Log | Throw — what a payload with neither a validator nor [SkipNValidation] means — is
    // bound from configuration so it can be changed without a rebuild; see appsettings.json.
    o.AddValidationFilter(builder.Configuration.GetSection("Validation"));
});

builder.Services.AddControllers();

// A ValidationException becomes a 400 problem details response, so the endpoint below never mentions
// HTTP status property names.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapControllers();

// A minimal API endpoint validates by calling its validator: the filter above is an MVC filter, and
// there is no action for it to run in front of here.
//
// The throwing path: validate, and let the handler turn a failure into the response. The options select
// the Create group, which is what [ValidationGroups] does for the controller action.
app.MapPost("/cars", async (Car car, IValidator<Car> validator, CancellationToken cancellationToken) =>
{
    var options = new NValidationOptions { ValidationGroups = CarValidator.CreateGroup };

    await validator.ValidateAndThrowAsync(car, options, cancellationToken);

    return Results.Ok(new { car.Vin });
});

// A listing check: the Listing group alone, and the policy of the market the car is offered in, which the
// car cannot answer for itself. The options are built once and shared by every request.
var listingCheck = new NValidationOptions
{
    ValidationGroups = ValidationGroups.Only(CarValidator.ListingGroup),
    ValidationData = [new ListingPolicy(MaximumMileage: 200_000, RequiresServiceHistory: true)],
};

app.MapPost("/cars/listing-check", async (Car car, IValidator<Car> validator, CancellationToken cancellationToken) =>
{
    var result = await validator.ValidateAsync(car, listingCheck, cancellationToken);

    return result.Succeeded
        ? Results.NoContent()
        : Results.Problem(result.ToProblemDetails());
});

// The returning path, for an endpoint that would rather decide for itself what a failure means.
app.MapPost("/cars/checked", async (Car car, IValidator<Car> validator, CancellationToken cancellationToken) =>
{
    var result = await validator.ValidateAsync(car, cancellationToken);

    return result.Succeeded
        ? Results.Ok(new { car.Vin })
        : Results.Problem(result.ToProblemDetails());
});

app.Run();

/// <summary>
/// Exposed so the sample can be started from a test host.
/// </summary>
public partial class Program;
