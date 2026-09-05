# NValidation

NValidation is a small, explicit validation library for .NET.

Rules are plain C# on a typed rule chain, messages are pluggable, and nothing happens by convention:
what a validator checks is what you can read in its constructor.

## Why NValidation?

- Rules declared per property, in one readable chain
- No attributes, no conventions, no reflection over your model's metadata
- Messages resolved through an interface, so they localize with whatever the application already uses
- Validators are plain objects: constructible, injectable, and unit-testable on their own
- Nested objects validated by their own validator, with the error codes prefixed automatically
- A separate `NValidation.AspNetCore` package for the RFC7807 problem details response

## Packages

| Package                  | What it adds                                                                                                 |
|--------------------------|--------------------------------------------------------------------------------------------------------------|
| `NValidation`            | The validators, rules and messages. Depends only on `Microsoft.Extensions.DependencyInjection.Abstractions`. |
| `NValidation.AspNetCore` | Maps a validation failure to a 400 problem details response.                                                 |

Both target .NET 8 and .NET 10.

## Quick start

### 1. Define a validator

```csharp
using NValidation;

public sealed class CarValidator : Validator<Car>
{
    public CarValidator(IValidator<CarModel> carModelValidator)
    {
        this.Property(c => c.Vin)
            .NotEmpty()
            .Must(vin => vin == null || vin.Length == 17, "The VIN must be exactly 17 characters long.");

        this.Property(c => c.Model)
            .NotNull()
            .SetValidator(carModelValidator);

        this.Property(c => c.Mileage)
            .GreaterThanOrEqualTo(0);

        this.Property(c => c.FirstRegistration)
            .NotEmpty()
            .WithDisplayName("Registration date");

        this.Property(c => c.SoldDate)
            .GreaterThanOrEqualTo(c => c.FirstRegistration);
    }
}
```

### 2. Register it

Everything this library needs is configured in one delegate:

```csharp
services.AddNValidation(o => o
    .AddValidator<CarModelValidator>()
    .AddValidator<CarValidator>());
```

Or let an assembly be scanned, which finds every `IValidator<T>` in it and resolves each one's own dependencies:

```csharp
services.AddNValidation(o => o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly));
```

`AddValidator<TValidator>()` reads the validated type off the validator itself. Name both
(`AddValidator<Car, CarValidator>()`) where you would rather the compiler checked that a validator really does validate
what you think it does.

Registration uses `TryAdd`, so a validator registered explicitly beforehand wins over whatever a scan finds for the same
type.

#### Lifetimes

Validators are **scoped** by default, because a validator may depend on something that is itself scoped — the database
an async uniqueness rule asks — and a longer-lived validator would capture it.

A validator declares its rules in its constructor and never changes afterwards, so where nothing scoped is involved,
registering them as singletons builds those rules once for the process instead of once per request:

```csharp
services.AddNValidation(o =>
{
    o.ValidatorLifetime = ServiceLifetime.Singleton;

    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly)

     // The one that cannot follow the default overrides it, rather than dragging the rest down.
     .AddValidator<VinUniquenessValidator>(ServiceLifetime.Scoped);
});
```

Nothing reaches the service collection until the delegate has run, so `ValidatorLifetime` governs every validator
wherever in the delegate you set it.

The message provider is built by the container and registered as a **singleton**: it is a lookup asked for text, it has
to be thread-safe anyway because validators run concurrently, and being longer-lived than every validator is what lets
validators of any lifetime be handed it. Resolve the language while the message is produced — a `Func<string>` over a
resource — rather than in the constructor. A provider that genuinely cannot be shared goes through `o.Services` instead,
at the cost of forcing every validator that uses it to be scoped too.

### 3. Validate

```csharp
var result = await this.carValidator.ValidateAsync(car);

if (!result.Succeeded)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.Code}: {error.Message}");
    }
}
```

`error.Code` is the C# property path — `Vin`, or `Model.Manufacturer.Name` for a nested one — so a client can bind each
message to the input it belongs to.

Alternatively, `result.ThrowIfInvalid()` raises a `ValidationException` carrying the same errors grouped by code.

`ValidateAndThrowAsync` is the same thing for a caller that would rather treat a failure as an exception than as a
result to inspect.

**Validation is asynchronous, and only asynchronous.** Most rules are synchronous, but a rule may `await` whatever it
needs — a uniqueness check against a database, a lookup against another service — and one such rule makes the whole
chain asynchronous. A synchronous entry point would therefore be a promise the library cannot keep: it could only work
by deciding at run time whether your rules happened to finish in time, which is exactly the kind of answer that differs
between a cache hit and a cache miss. A caller in a synchronous method awaits the call itself, and can see the cost it
is paying.

## Rules

| Group       | Rules                                                                                                      |
|-------------|------------------------------------------------------------------------------------------------------------|
| Presence    | `NotNull`, `NotEmpty` (text and collections), `NotDefault` (any value type)                                |
| Text        | `MinimumLength`, `MaximumLength`, `Length(exact)`, `Length(min, max)`, `Matches`, `EmailAddress`           |
| Comparison  | `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `Between`, `EqualTo`, `NotEqualTo` |
| Numbers     | `MultipleOf`, `NotNaN`                                                                                     |
| Dates       | `InThePast`, `InTheFuture`                                                                                 |
| Collections | `MinimumCount`, `MaximumCount`, `NoDuplicates`                                                             |
| Enums       | `IsInEnum`                                                                                                 |
| Custom      | `Must`, `SetValidator`                                                                                     |

Chain modifiers: `When`, `Unless`, `WithMessage`, `WithDisplayName`, `WithErrorCode`, `ContinueOnFailure`.

`error.Code` defaults to the member path, which is what a client usually binds to. Where the client's field is not
shaped like the model's, override it — the message is unaffected:

```csharp
this.Property(c => c.Model.Manufacturer.Name)
    .WithErrorCode("manufacturerName")
    .WithDisplayName("Manufacturer")
    .NotEmpty();

// reports: { "manufacturerName": ["Manufacturer is required."] }
```

### Comparisons

The comparison rules are written once, over `IComparable<T>`, so they work for every numeric type and for `DateTime`,
`DateTimeOffset`, `TimeSpan`, `DateOnly` and `TimeOnly` alike:

```csharp
this.Property(c => c.Mileage).GreaterThanOrEqualTo(0);
this.Property(c => c.BasePrice).Between(1m, 999m);
this.Property(c => c.UnitsProduced).GreaterThan(1_000L);
this.Property(c => c.ServiceInterval).LessThanOrEqualTo(TimeSpan.FromDays(365));
```

Each of them also compares against another property of the same object, on either side of which the value may be
optional — a missing value has nothing to compare and passes:

```csharp
this.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);
```

Requiring a value is a separate decision, and `NotNull()` or `NotEmpty()` is what makes it.

### Presence

`NotEmpty` asks whether there is any content, which only a string or a collection can answer.
`NotDefault` asks whether a value type was set at all — an enum's zero member, a `DateTime.MinValue`, an empty `Guid` —
which is what arrives when nothing was chosen.

A chain declared through another object is skipped when that object is not there, rather than throwing:

```csharp
this.Property(c => c.Model).NotNull();          // whether it has to be there at all
this.Property(c => c.Model.Manufacturer.Name).NotEmpty();   // judged only if it is
```

A payload that omitted `Model` reports `Model`, not a server error. This is the same answer the rest of
the library gives to something absent — a null nested object is skipped by `SetValidator`, a missing
collection by its own rules, an absent value by a comparison — so requiring presence is always a rule of
its own, next to the rules about the value.

### Dates

`InThePast` and `InTheFuture` compare in UTC and take an optional `TimeProvider`, so a test can decide what "now" is:

```csharp
this.Property(m => m.FoundedDate).InThePast(this.timeProvider);
```

A `DateTime` of kind `Unspecified` — what a date deserialized without an offset carries — is read as UTC rather than as
local time, so the same payload gets the same verdict whatever time zone the host runs in. Use `DateTimeOffset` where
the input genuinely carries one.

Your own rules are extension methods on `PropertyRuleBuilder<T, TProperty>`, so they chain exactly like the shipped
ones:

```csharp
public static PropertyRuleBuilder<T, string?> Vin<T>(this PropertyRuleBuilder<T, string?> builder)
{
    return builder.Add(context =>
    {
        if (context.Value is { Length: not 17 })
        {
            context.AddError(new ValidationError(context.Code, "The VIN must be exactly 17 characters long."));
        }
    });
}
```

## Collections

Rules about the collection and rules about its elements go on the same chain, with `ForEach` last:

```csharp
this.Property(c => c.ServiceHistory)
    .NotEmpty()
    .MaximumCount(50)
    .ForEach(record => record.Property(r => r.Workshop).NotEmpty());
```

A chain belongs to the property it started on, so rules for a second property of the same entry are a second statement —
the element builder is a validator, and takes as many as the entry needs:

```csharp
this.Property(c => c.ServiceHistory)
    .ForEach(record =>
    {
        record.Property(r => r.Workshop).NotEmpty().MaximumLength(100);
        record.Property(r => r.Mileage).GreaterThanOrEqualTo(0);

        // A rule may consult the rest of the entry it is judging, but not the object the collection
        // hangs off; a rule about that belongs on the collection itself.
        record.Property(r => r.Mileage)
            .Must((r, mileage) => r.Cost == 0m || mileage > 0, "A paid service records its mileage.");
    });
```

`ForEach` is a rule like any other, so a chain that has already failed does not reach it — too many entries is reported
on its own, rather than alongside a complaint about each of them. It returns nothing, so declare it last.

Each failure is reported under the element's position, so a caller can bind it to the row it came from:

```json
{
  "errors": {
    "ServiceHistory[1].Workshop": [
      "Workshop is required."
    ]
  }
}
```

For a collection of scalars there is no property to name, so the element itself is the subject:

```csharp
this.Property(c => c.ServiceMileages).ForEach(mileage => mileage.Element().GreaterThanOrEqualTo(0));
// reports: ServiceMileages[1]
```

Where the element already has a validator, use it:

```csharp
this.Property(c => c.ServiceHistory).ForEach(serviceRecordValidator);
```

`Where(...)` on the element builder restricts which elements are judged; the ones it skips keep their position, so an
index always points at the row the caller sent. Where a position is not what the caller matches on, identify each
element by something of its own:

```csharp
this.Property(c => c.ServiceHistory)
    .ForEach(record => record
        .WithIndexer((r, _) => r.InvoiceNumber)
        .Property(r => r.Workshop).NotEmpty());

// reports: ServiceHistory[INV-9912].Workshop
```

A missing collection and a `null` element are skipped — whether entries have to be there at all is a question for the
collection's own rules. The collection is enumerated exactly once, so a property typed `IEnumerable<T>` backed by a
query is safe.

Messages about an element can name its position with `{CollectionIndex}`, whichever way the rules were declared — an
entry's own validator answers through the provider of the run it was composed into, not its own.

## Messages and localization

Rules report a message *key*, never a text. The key is resolved through `IValidationMessageProvider`, so the application
decides where the wording comes from and in which language:

```csharp
public sealed class ResourceValidationMessageProvider : IValidationMessageProvider
{
    private static readonly Dictionary<string, Func<string>> Messages = new(StringComparer.Ordinal)
    {
        [ValidationMessageKeys.NotEmpty] = () => Strings.ValidationMessage_Required,
        [ValidationMessageKeys.MaximumLength] = () => Strings.ValidationMessage_MaxLength,
    };

    public string GetMessage(string messageKey, IReadOnlyDictionary<string, object?> arguments)
    {
        return Messages.TryGetValue(messageKey, out var message)
            ? ValidationMessageFormatter.Format(message(), arguments)
            : DefaultValidationMessageProvider.Instance.GetMessage(messageKey, arguments);
    }
}
```

```csharp
services.AddNValidation(o => o.MessageProvider = typeof(ResourceValidationMessageProvider));
```

Messages use named placeholders — `{PropertyName}`, `{MaxLength}`, `{OtherValue}`, `{Step:0.00}` — and a message uses
only the ones it needs. A translation is free to leave the property name out, which is what you want for a message shown
underneath an already labelled input. Without a provider the built-in English messages are used.

## ASP.NET Core

```csharp
services.AddProblemDetails();
services.AddExceptionHandler<ValidationExceptionHandler>();
```

A `ValidationException` then comes out as a 400 with the failures under a top-level `errors` member:

```json
{
  "status": 400,
  "errors": {
    "Vin": [
      "The VIN must be exactly 17 characters long."
    ],
    "Model.Manufacturer.Name": [
      "Name is required."
    ]
  }
}
```

To return a failure instead of throwing:

```csharp
var result = await this.carValidator.ValidateAsync(car);

if (!result.Succeeded)
{
    return this.ValidationProblem(result);
}
```

An application which already has its own exception-to-problem-details handler should read
`ValidationException.Errors` there rather than registering `ValidationExceptionHandler`, so every error response keeps
going through one place.

### Validating a controller's payload automatically

`ValidationActionFilter` validates an action's payload before the action runs. For every parameter bound from the
request body or form, it resolves the validator registered for that parameter's declared type and runs it; a type with
no registered validator is left alone.

```csharp
services.AddControllers(o => o.Filters.Add<ValidationActionFilter>());
```

The action is then free of validation code:

```csharp
[HttpPost("")]
public ActionResult<string> Create(Car car)
{
    // car is valid: an invalid one never got here.
}
```

Failures of every payload of the request are collected into one `ValidationException`, so the response reports all of
them at once. Route and query values are not payloads and are never validated.

An endpoint which reports failures in its own shape opts out, and validates itself:

```csharp
public async Task<IActionResult> ValuateAsync(
    [SkipValidation("Answers in a legacy error shape which deployed clients parse.")] CarValuation carValuation)
```

The reason is optional — plain `[SkipValidation]` excludes just as well — but it is what tells the next reader that the
gap was a decision. The attribute goes on a parameter, an action or a whole controller.

`MissingValidatorBehavior` decides what happens to a payload which has neither a validator nor
`[SkipValidation]` — `Ignore` (the default), `Log`, or `Throw` to make the gap impossible to miss on a development host:

```csharp
services.AddNValidation(o =>
{
    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
    o.AddValidationFilter(f => f.MissingValidatorBehavior = MissingValidatorBehavior.Throw);
});
```

`AddValidationFilter` lives in `NValidation.AspNetCore` and is opt-in: this is an MVC filter, and an application built
on minimal APIs validates by calling its validator in the handler. Pass an
`IConfiguration` section instead of a delegate to bind the behaviour from configuration.

A runnable end-to-end example lives in [`Samples/NValidation.SampleApi`](Samples/NValidation.SampleApi).

## Testing your own rules

A validator is a plain object, so a test constructs it and runs it against whatever data the case is about:

```csharp
[Fact]
public async Task ValidateAsync_WithoutAName_ReportsTheName()
{
    // Arrange
    var validator = new ManufacturerValidator();
    var manufacturer = new Manufacturer { CountryCode = "CHE" };

    // Act
    var result = await validator.ValidateAsync(manufacturer);

    // Assert
    result.Errors.Should().ContainSingle().Which.Code.Should().Be("Name");
}
```

That is also how a rule of your own is tested: declare a validator whose only rule is the one under test, and take its
parameters through the constructor where the test needs to vary them.

```csharp
internal sealed class VinValidator : Validator<Car>
{
    public VinValidator(int length) => this.Property(c => c.Vin).Length(length);
}
```

## License

This project is licensed under the MIT license.
