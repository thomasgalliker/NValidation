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

| Package                  | What it adds                                                           |
|--------------------------|------------------------------------------------------------------------|
| `NValidation`            | The validators, rules and messages, and helper methods for unit tests. |
| `NValidation.AspNetCore` | Validation integration for ASP.NET Core⁄ applications.                 |

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
type. That is also how a payload with two validators in one assembly is settled: name the one you want before scanning,
and the scan passes over it instead of refusing to choose.

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
| Text        | `MinimumLength`, `MaximumLength`, `Length(exact)`, `Length(min, max)`, `Matches`, `NotContaining`          |
| Email       | `EmailAddress`, `EmailTopLevelDomainIn`, `EmailTopLevelDomainNotIn`                                        |
| Comparison  | `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `Between`, `EqualTo`, `NotEqualTo` |
| Numbers     | `MultipleOf`, `NotNaN`                                                                                     |
| Dates       | `InThePast`, `InTheFuture`                                                                                 |
| Collections | `MinimumCount`, `MaximumCount`, `NoDuplicates`                                                             |
| Enums       | `IsInEnum`                                                                                                 |
| Custom      | `Must`, `SetValidator`                                                                                     |

Chain modifiers: `When`, `Unless`, `WithMessage`, `WithDisplayName`, `WithErrorCode`, `WithValidationBehavior`. The last
of those is the local exception to a setting the validator and the registration also carry — see
[Validation behavior](#validation-behavior).

`error.Code` defaults to the member path, which is what a client usually binds to. Where the client's field is not
shaped like the model's, override it — the message is unaffected:

```csharp
this.Property(c => c.Model.Manufacturer.Name)
    .WithErrorCode("manufacturerName")
    .WithDisplayName("Manufacturer")
    .NotEmpty();

// reports: { "manufacturerName": ["Manufacturer is required."] }
```

### Validation behavior

How much a validator reports is one decision asked at two scales: whether a run keeps going once a property has
reported, and whether a property's chain keeps going once one of its rules has failed. Both live under one setting, and
the level it applies to is where you write it rather than a word in its name:

```csharp
// the registration — the default for every validator resolved from the container
services.AddNValidation(o =>
{
    o.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError;
});

// the validator
public sealed class CarValidator : Validator<Car>
{
    public CarValidator()
    {
        this.ValidationBehaviors.Property = ValidationBehavior.All;

        this.Property(c => c.Vin).NotEmpty().Length(17);
    }
}

// one property, whose rules judge genuinely separate things
this.Property(c => c.Vin)
    .Length(17)
    .Matches("^[A-HJ-NPR-Z0-9]+$")
    .WithValidationBehavior(ValidationBehavior.All);
```

| Setting                        | Default            | Governs                                      |
|--------------------------------|--------------------|----------------------------------------------|
| `ValidationBehaviors.Class`    | `All`              | whether the run goes on to the next property |
| `ValidationBehaviors.Property` | `StopAtFirstError` | whether a chain goes on to its next rule     |

Both axes are nullable, and `null` — which is what they start as — means *inherit from the level above*. Naming one
therefore never silently changes the other: a validator that sets `Class` leaves `Property` taking whatever the
registration configured, and a registration that sets neither leaves both at the defaults above. That is also why the
setting is mutated rather than assigned; replacing the whole object would replace the axis you did not mean to touch.

The defaults are chosen for the common case. Reporting every property at once is what lets a caller fix a form in one
pass. Stopping within a chain is right because a chain's rules usually run coarse to fine: an empty string fails
`NotEmpty` and `Length(17)` alike, and only the first of those tells the caller anything. Set `Property` to `All` for
the chain whose rules judge separate things — a VIN is the wrong length *and* carries a letter no VIN may contain, and a
caller wants to hear both.

**`Class = StopAtFirstError` stops the run as soon as anything has been reported** — the properties after it are never
looked at. It therefore also stops the chain that produced it, overriding `Property`, since a setting by that name which
went on judging the same property would be a trap. The single exception is a chain that says otherwise through
`WithValidationBehavior`, because it says so at the point it applies; that is how you get *everything about the first
field that is wrong, then stop*. A property whose `When` did not hold reported nothing, so there is nothing for the run
to stop on and the next property is still judged.

That usually means a single message, but do not rely on it as a cap. A run is stopped *between* rules, and one rule that
reported several at once is not cut short: a validator merged in with `SetValidator` has already had its own say, and a
`ForEach` reports on every entry it walked. What a composed validator found is its decision, not its composer's, so it
is passed on whole rather than truncated.

The setting is resolved while validating, not while the rules are declared, so where in a constructor you write it makes
no difference. A validator composed into another — through `SetValidator`, or per entry through `ForEach` — decides for
itself, and is never cut short by what its composer has already reported.

One caution for an HTTP payload: a stopping validator produces a problem details body naming a single field. That is
often right for a machine caller, and usually wrong for a form a person is filling in.

Coming from FluentValidation, `ClassLevelCascadeMode` maps onto `Class` and `RuleLevelCascadeMode` onto `Property`, with
`Continue` reading as `All` and `Stop` as `StopAtFirstError`.

### Comparisons

The comparison rules are written once, over `IComparable<T>`, so they work for every numeric type and for `DateTime`,
`DateTimeOffset`, `TimeSpan`, `DateOnly` and `TimeOnly` alike:

```csharp
this.Property(c => c.Mileage).GreaterThanOrEqualTo(0);
this.Property(c => c.BasePrice).Between(1m, 999m);
this.Property(c => c.UnitsProduced).GreaterThan(1_000L);
this.Property(c => c.ServiceInterval).LessThanOrEqualTo(TimeSpan.FromDays(365));
```

Each of them — and `EqualTo`/`NotEqualTo` with it — also compares against another property of the same object, on either
side of which the value may be optional. A missing value has nothing to compare and passes, and so does a property
reached through an object the payload omitted:

```csharp
this.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);
this.Property(c => c.Mileage).LessThanOrEqualTo(c => c.Model.MileageCap);   // skipped when Model is absent
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

A payload that omitted `Model` reports `Model`, not a server error. This is the same answer the rest of the library
gives to something absent — a null nested object is skipped by `SetValidator`, a missing collection by its own rules, an
absent value by a comparison, and the *compared* property of a two-property rule by the same guard — so requiring
presence is always a rule of its own, next to the rules about the value.

The expression has to reach the property through the validator's own parameter. `x => x.Address.Street` is a path;
`x => x.Lines[0].Street` and `x => somethingElse.Street` are not, and are refused where they are declared rather than
silently reported under `Street`.

### Email addresses

`EmailAddress` parses the value with `System.Net.Mail.MailAddress` rather than matching it against a pattern. The
address forms that are legal are far broader than a hand-written pattern allows — a quoted local part, an IP literal, an
internationalized domain — which is why FluentValidation deprecated its own RFC 5322 regex; a parser also cannot be made
to backtrack by a hostile value.

The value has to be the address **alone**. `MailAddress` parses the header forms too, so `Foo <a@b.com>`,
`a@b.com, c@d.com` and a value with surrounding whitespace all parse — and each is something other than the single
address the field asked for. They are rejected.

Which domains you accept is a separate decision, and a separate rule:

```csharp
this.Property(u => u.Email)
    .NotEmpty()
    .EmailAddress()
    .EmailTopLevelDomainNotIn("test", "invalid", "example");
```

`NotContaining` is the general form for text a field will not carry, wherever it comes from:

```csharp
this.Property(u => u.DisplayName).NotContaining("admin", "support");
```

It compares without regard to case, and the message names none of the terms — a blocklist that reports its own entries
is one the next value works around.

### Dates

`InThePast` and `InTheFuture` compare in UTC, and each has an overload taking a `TimeProvider`, so a test can decide
what "now" is:

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

The element builder is a validator, so it carries its own [validation behavior](#validation-behavior):
`record.ValidationBehaviors.Class = ValidationBehavior.StopAtFirstError` governs what *one entry* reports, and every
entry is still walked.

It is built where it is declared rather than by the container, so — like a validator you construct with `new` — it takes
the built-in defaults and not what `AddNValidation` configured. Set it on the element builder itself where an element's
rules should follow a different policy from the defaults.

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
// reports: ServiceMileages[1] -> "ServiceMileages[1] must be greater than or equal to 0."
```

A rule declared on the element itself names no property, so the message names the element by the very code the failure
is reported under. `WithDisplayName(...)` overrides that as it does anywhere else.

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
collection's own rules.

Each collection rule walks the sequence once, and no further than its own question needs — `MaximumCount(50)` stops at
the fifty-first entry. A *chain* of them asks one question each, so `NotEmpty().MaximumCount(50).ForEach(...)` walks it
three times. That is free for a `List<T>` or an array, which answer `Count` without being walked at all, but a property
typed `IEnumerable<T>` backed by a live query runs that query once per rule, and one that cannot be enumerated twice
will throw. Materialize such a property before validating it; the library cannot do it for you without handing your own
rules a different object than the one your model holds.

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

A validator is a plain object, so a test constructs it and runs it against whatever data the case is about.
`NValidation.Testing` ships in the same package and needs no test framework and no assertion library of its own:

```csharp
using NValidation.Testing;

[Fact]
public async Task ValidateAsync_WithoutAName_ReportsTheName()
{
    // Arrange
    var validator = new ManufacturerValidator();
    var manufacturer = new Manufacturer { CountryCode = "CHE" };

    // Act
    var result = await validator.ValidateAsync(manufacturer);

    // Assert
    result.ShouldReport("Name", "Name is required.");
}
```

`ShouldReport` states the **whole** expected result: nothing else may be present and the count is implied, so a repeated
entry asks for a repeated failure. Order is ignored. A failure throws `ValidationAssertionException`, naming what was
missing, what was unexpected, and the near miss in between:

```
Expected the validation result to report exactly 2 errors:
  Vin      "Vin is mandatory."
  Mileage  (any message)
but it reported 2 errors:
  Vin   "Vin is required."
  Cost  "Cost is required."

Not reported:
  Vin      "Vin is mandatory."  (an error was reported under "Vin", but its message differs)
  Mileage  (any message)        (nothing was reported under "Mileage")

Not expected:
  Vin   "Vin is required."
  Cost  "Cost is required."
```

Assert the message, not only the code. A test which checks the code alone passes when a rule reports the right property
with the wrong message — a value that is too large reported as "must be greater than". The message is matched with
wildcards, so a fragment is as easy to express as the whole of it, and `ExpectedError` defaults it to `*` for a test
that is about which properties report rather than about the wording:

```csharp
result.ShouldReport("Vin", "Vin is required.");        // code + message
result.ShouldReport("Mileage", "*greater than*");      // code + part of the message
result.ShouldReport([
    new("FeatureIds"),                                 // code only
    new("Model.Manufacturer.ContactEmail", "*not a valid email address*"),
    new("ServiceHistory[0].Workshop", "Workshop is required.")]);
```

The same overloads take a `ValidationException` or a `{ code: [messages] }` dictionary, which is what a test of an
endpoint holds rather than a `ValidationResult`:

```csharp
exception.ShouldReport("Vin", "The VIN is required.");

var errors = (IReadOnlyDictionary<string, string[]>)problemDetails.Extensions["errors"]!;
errors.ShouldReport([
    new("Vin", "The VIN is required."),
    new("Mileage", "The mileage must be greater than or equal to 0.")]);
```

Success is `result.Errors.Should().BeEmpty()` in whichever assertion library you already use.

### One rule at a time

`TestValidator<T>` declares the rule under test in the test itself, so a reader does not have to open a second file to
learn what is being validated:

```csharp
var validator = new TestValidator<Invoice>();
validator.Property(i => i.Reference).NotEmpty().MaximumLength(32);

var result = await validator.ValidateAsync(new Invoice());

result.ShouldReport("Reference", "Reference is required.");
```

Because rules report a message *key* rather than a text, a test can assert the key instead of the wording and stay
independent of translations. Hand the validator `MessageKeyProvider.Instance` and it answers with keys:

```csharp
var validator = new TestValidator<Invoice>(MessageKeyProvider.Instance);
validator.Property(i => i.Reference).MinimumLength(10);

var result = await validator.ValidateAsync(new Invoice { Reference = "AB" });

result.ShouldReport("Reference", "MinimumLength");
```

A rule that compares against "now" takes a clock the test owns, so it does not start failing on a future Tuesday:

```csharp
var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

var validator = new TestValidator<Invoice>();
validator.Property(i => i.DueDate).InTheFuture(clock);

var result = await validator.ValidateAsync(new Invoice { DueDate = new DateTime(2025, 12, 31) });

result.ShouldReport("DueDate", "DueDate must be a date in the future.");
```

### Testing your own message provider

An application which resolves messages itself owes every key a text. One assertion covers the lot, so a missing
translation shows up in the suite rather than as a raw key in a response:

```csharp
[Fact]
public void GetMessage_AnswersForEveryKeyOfTheCore()
{
    new ResourceValidationMessageProvider().ShouldResolveEveryCoreMessageKey();
}
```

It checks that each key of `ValidationMessageProviderAssertions.CoreMessageKeys()` resolves to something other than the
key itself, and that no `{Placeholder}` is left unsubstituted. It does not require a message to name the failing
property — that is the translation's call.

## License

This project is licensed under the MIT license.
