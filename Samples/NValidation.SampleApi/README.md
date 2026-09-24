# NValidation.SampleApi

One cars domain, validated two ways: a minimal API where each handler calls its validator, and controllers
where `ValidationActionFilter` has already done it by the time an action runs. The handlers and actions are
stubs on purpose — every line in this project is about validation.

```bash
dotnet run --project NValidation/Samples/NValidation.SampleApi
```

Then work through [NValidation.SampleApi.http](NValidation.SampleApi.http).

## Registration

```csharp
builder.Services.AddNValidation(o =>
{
    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
    o.AddValidationFilter(builder.Configuration.GetSection("Validation"));
});

builder.Services.AddControllers();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
```

`AddValidationFilter` is opt-in: which validators exist and how a host dispatches to them are separate
decisions, and an application without controllers makes only the first one.

## Minimal API — validate in the handler

`ValidationActionFilter` is an MVC filter, so a minimal API endpoint is untouched by it. `POST /cars` takes
the throwing path and `POST /cars/checked` the returning one; both are three lines and both are explicit.

`POST /cars/listing-check` also hands the rules something the car cannot answer for itself — the policy of
the market it is offered in:

```csharp
var listingCheck = new NValidationOptions
{
    ValidationGroups = ValidationGroups.Only(CarValidator.ListingGroup),
    ValidationData = [new ListingPolicy(MaximumMileage: 200_000, RequiresServiceHistory: true)],
};
```

`CarValidator` reads it with `Must<ListingPolicy>` and `When<ListingPolicy>`. The options are built once
and shared by every request.

## Controllers — validated before the action

`CarsController.Create` contains no validation code:

```csharp
[HttpPost("")]
[ValidationGroups(CarValidator.CreateGroup)]
public ActionResult<string> Create(Car car)
{
    return this.Ok(car.Vin);
}
```

The attribute adds the Create group to the default group, so the chain `CarValidator` declares for a car
being taken in runs here and on no other endpoint.

The filter resolved `IValidator<Car>` and ran it, so an invalid payload never reached the action:

```json
{
  "status": 400,
  "errors": {
    "Vin": ["The VIN must be exactly 17 characters long."],
    "Model.Name": ["Name is required."],
    "Model.Manufacturer.Name": ["Name is required."],
    "Model.Manufacturer.CountryCode": ["CountryCode must be exactly 3 characters long."],
    "Model.Manufacturer.ContactEmail": ["ContactEmail is not a valid email address."],
    "Model.SeatCount": ["SeatCount must be between 1 and 9."],
    "Model.BasePrice": ["BasePrice is required."],
    "Mileage": ["Mileage must be greater than or equal to 0."],
    "PurchasePrice": ["PurchasePrice must be greater than 0."]
  }
}
```

Everything wrong with the request is reported together, and a nested failure carries the path to the
property it belongs to, so a form binds each message to the input it came from.

### A check can run one group alone

`POST /api/cars/listing-check` answers whether a car can be offered for sale, and nothing else:

```csharp
[HttpPost("listing-check")]
[ValidationGroups(CarValidator.ListingGroup, Only = true)]
public IActionResult CheckListing(Car car)
{
    return this.NoContent();
}
```

`Only = true` leaves the default group out, so a car with a wrong VIN but a plate and a price passes here.
The price chain is in the default group and in the Listing group, so it is checked by this endpoint and by
every other.

### A route value is not a payload

`PUT /api/cars/{carId}` takes an id from the route and a car from the body. Only the body is validated: the
filter looks at parameters bound from the body or the form and leaves the rest alone.

### An endpoint can opt out and validate itself

`POST /api/cars/valuations` answers in a legacy error shape which its clients parse, so it is excluded from
the filter and runs the same validator itself:

```csharp
public async Task<IActionResult> ValuateAsync(
    [SkipNValidation("Answers in a legacy error shape which deployed clients parse; validated explicitly below.")]
    CarValuation carValuation,
    CancellationToken cancellationToken)
{
    var validationResult = await this.carValuationValidator.ValidateAsync(carValuation, cancellationToken);
    if (!validationResult.Succeeded)
    {
        return this.BadRequest(new { error = "invalid_request", ... });
    }
    ...
}
```

The rules stay in one place; only the shape of the answer differs.

### A payload nobody validates is reported

`POST /api/cars/imports` takes a `CarImport`, for which no validator exists.
`Validation:MissingValidatorBehavior` in [appsettings.json](appsettings.json) decides what that means:

| Setting | Effect |
| --- | --- |
| `Ignore` | The action runs. The default, so adding the filter to an existing application changes nothing. |
| `Log` | The action runs, and one warning names the action and the parameter type. |
| `Throw` | The request fails. A payload nobody validates is a gap in the application, and this is how it stops being invisible. |

`Throw` suits a development or test host. It stays quiet about anything carrying `[SkipNValidation]`: that
was a decision, not an oversight.
