# NValidation

[![Version](https://img.shields.io/nuget/v/NValidation.svg)](https://www.nuget.org/packages/NValidation)
[![Downloads](https://img.shields.io/nuget/dt/NValidation.svg)](https://www.nuget.org/packages/NValidation)
[![Buy Me a Coffee](https://img.shields.io/badge/support-buy%20me%20a%20coffee-FFDD00)](https://buymeacoffee.com/thomasgalliker)

NValidation is a small, explicit validation library for .NET.

Rules are plain C# on a typed rule chain, messages are pluggable, and nothing happens by convention:
what a validator checks is what you can read in its constructor.

## Why NValidation?

- Rules declared per property, in one readable chain
- No attributes, no conventions, no reflection over your model's metadata
- Messages resolved through an interface, so they localize with whatever the application already uses
- Validators are plain objects: constructible, injectable, and unit-testable on their own
- Nested objects validated by their own validator, with the error property names prefixed automatically
- A separate `NValidation.AspNetCore` package for the RFC7807 problem details response

## Download and Install NValidation

This library is available on NuGet: https://www.nuget.org/packages/NValidation/
Use the following command to install NValidation using the NuGet Package Manager Console:

    PM> Install-Package NValidation

For ASP.NET Core application, use the integration package:

    PM> Install-Package NValidation.AspNetCore

| Package                                                                                              | What it adds                                                                                                               | Depends on                        |
|------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|-----------------------------------|
| [`NValidation`](https://www.nuget.org/packages/NValidation/)                                         | The validators, rules and messages, and helper methods for unit tests.                                                     | -                                 |
| [`NValidation.AspNetCore`](https://www.nuget.org/packages/NValidation.AspNetCore/)                   | The RFC7807 problem details response, and the MVC filter that validates a payload before the action runs.                  | `NValidation.DependencyInjection` |
| [`NValidation.DependencyInjection`](https://www.nuget.org/packages/NValidation.DependencyInjection/) | `AddNValidation`: registering validators with an `IServiceCollection`, and binding the registration from `IConfiguration`. | `NValidation`                     |

The core package has no dependencies at all, so a host which only constructs validators does not acquire
a container's abstractions in order to do it. Each package pulls in the one above it, so installing
`NValidation.AspNetCore` is enough for a web application.

All three target .NET 8 and later.

## Contents

- [Basic usage of validators](#basic-usage-of-validators)
- [Validation of objects](#validation-of-objects)
    - [Conditions: When and Unless](#conditions-when-and-unless)
    - [Rule groups](#rule-groups)
    - [Data a validation carries](#data-a-validation-carries)
    - [Validating some properties](#validating-some-properties)
- [Validation results](#validation-results)
- [Built-in validators](#built-in-validators)
- [Custom validators](#custom-validators)
- [Overriding defaults](#overriding-defaults)
    - [Validation options](#validation-options)
- [Localization](#localization)
- [Dependency injection](#dependency-injection)
- [ASP.NET Core integration](#aspnet-core-integration)
- [Testing your own rules](#testing-your-own-rules)

## Basic usage of validators

### 1. Define a validator

A validator derives from `Validator<T>` and declares its rules in its constructor. There is no
configuration step and no registration of rules at run time: the chain you can read is the chain that
runs.

```csharp
using NValidation;

public sealed class CarValidator : Validator<Car>
{
    public CarValidator(IValidator<CarModel> carModelValidator)
    {
        this.Property(c => c.Vin)
            .NotEmpty()
            .Must(vin => vin == null || vin.Length == 17)
            .WithMessage("The VIN must be exactly 17 characters long.");

        this.Property(c => c.Model)
            .NotNull()
            .SetValidator(carModelValidator);

        this.Property(c => c.Mileage)
            .GreaterThanOrEqualTo(0);

        this.Property(c => c.FirstRegistration)
            .WithDisplayName("Registration date")
            .NotDefault();

        this.Property(c => c.SoldDate)
            .GreaterThanOrEqualTo(c => c.FirstRegistration);
    }
}
```

`Property` takes an expression reaching a property through the validator's own parameter, and returns a
chain to hang rules on. Each rule returns the chain again, so a property's rules read top to bottom.

### 2. Register it

Everything this library needs is configured in one delegate:

```csharp
services.AddNValidation(o => o
    .AddValidator<CarModelValidator>()
    .AddValidator<CarValidator>());
```

Or let an assembly be scanned, which finds every `IValidator<T>` in it and resolves each one's own
dependencies:

```csharp
services.AddNValidation(o => o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly));
```

[Dependency injection](#dependency-injection) has the rest: the overloads, the lifetimes, and what the
container configures on a validator it built.

### 3. Validate

```csharp
var result = await this.carValidator.ValidateAsync(car);

if (!result.Succeeded)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.Message}");
    }
}
```

`ValidateAsync` returns a `ValueTask<ValidationResult>` and takes a `CancellationToken`, which it hands
to every rule that asked for one:

```csharp
var result = await this.carValidator.ValidateAsync(car, cancellationToken);
```

It never throws for a validation failure — a failure is a result, and the caller decides what it means.
It does throw `ArgumentNullException` for a null instance, because nothing can be said about an object
that is not there.

[Validation results](#validation-results) covers what comes back, and `ValidateAndThrowAsync` for a
caller that would rather treat a failure as an exception.

### Validation is asynchronous, and only asynchronous

Most rules are synchronous, but a rule may `await` whatever it needs — a uniqueness check against a
database, a lookup against another service — and one such rule makes the whole chain asynchronous. A
synchronous entry point would therefore be a promise the library cannot keep: it could only work by
deciding at run time whether your rules happened to finish in time, which is exactly the kind of answer
that differs between a cache hit and a cache miss. A caller in a synchronous method awaits the call
itself, and can see the cost it is paying.

Where every rule does finish synchronously — which is true of every rule shipped here — the returned
`ValueTask` completes synchronously and allocates nothing to represent waiting.

### Using a validator without a container

A validator is a plain object. Construct it and call it:

```csharp
var validator = new ManufacturerValidator();

var result = await validator.ValidateAsync(manufacturer);
```

A validator that takes other validators is constructed the same way — there is no container involved, so
you pass them yourself:

```csharp
var validator = new CarValidator(
    new CarModelValidator(new ManufacturerValidator()),
    new ServiceRecordValidator());
```

A validator built this way was handed nothing, so it falls back to
[`NValidationOptions.Default`](#validation-options) — the built-in English and the built-in
[validation behavior](#validation-behavior), until an application says otherwise:

```csharp
// Once, at startup, before anything validates.
NValidationOptions.Default = new NValidationOptions
{
    MessageProvider = new ResourceValidationMessageProvider(),
    ValidationBehaviors = new() { Property = ValidationBehavior.All },
};
```

That reaches every validator in the process which has not said otherwise for itself, the three nested
ones above included.

For one call rather than the whole process — a request whose messages are in its own language — pass the
options instead:

```csharp
var result = await validator.ValidateAsync(car, options);
```

And `validator.ValidationMessageProvider` and `validator.ValidationBehaviors` settle it for one validator,
outranking both. What a validator settles for itself is inherited by the validators it composes, so
setting the provider on `validator` reaches the three nested ones too — as a default, which a nested
validator that declared its own keeps. See [the override ladder](#the-override-ladder) for the whole
order, and [what the container configures](#what-the-container-configures) for how this relates to
`AddNValidation`.

### Validating an instance whose type is known only at run time

`IValidator<T>` is the typed contract and the one to prefer. Where a caller holds a validator it looked
up by `Type` — a pipeline dispatching on a parameter's declared type, say — the non-generic `IValidator`
is implemented by every validator and takes an `object`:

```csharp
var validator = (IValidator)serviceProvider.GetRequiredService(validatorType);

var result = await validator.ValidateAsync(payload, cancellationToken);
```

An instance of the wrong type is an `InvalidCastException`, not a validation failure.

## Validation of objects

A payload is rarely one flat object. A car has a model, the model has a manufacturer, and the car has a
service history of its own. Each of those is validated by the validator that belongs to it, and the
failures come back under one path the client can bind to.

### Nested objects

`SetValidator` hands a property to the validator that owns that type. Its errors are reported under the
property they came through, prefixed the whole way down:

```csharp
public sealed class CarValidator : Validator<Car>
{
    public CarValidator(IValidator<CarModel> carModelValidator)
    {
        this.Property(c => c.Model)
            .NotNull()
            .SetValidator(carModelValidator);
    }
}

// CarModelValidator reports "Name";     the car reports "Model.Name"
// ManufacturerValidator reports "Name"; the car reports "Model.Manufacturer.Name"
```

Each validator states only what it knows. `ManufacturerValidator` has no idea it is being run under a
car, which is what lets it be used on its own, tested on its own, and composed into a second payload
without changing.

A **null nested object is skipped** rather than reported. Whether it has to be there at all is a separate
question, and `NotNull()` is what asks it — which is why the chain above declares both.

The same applies to a chain declared *through* another object:

```csharp
this.Property(c => c.Model).NotNull();                      // whether it has to be there at all
this.Property(c => c.Model.Manufacturer.Name).NotEmpty();    // judged only if it is
```

A payload that omitted `Model` reports `Model`, not a server error. This is the same answer the rest of
the library gives to something absent — a null nested object is skipped by `SetValidator`, a missing
collection by its own rules, an absent value by a comparison, and the *compared* property of a
two-property rule by the same guard — so requiring presence is always a rule of its own, next to the
rules about the value.

The expression has to reach the property through the validator's own parameter. `x => x.Address.Street`
is a path; `x => x.Lines[0].Street` and `x => somethingElse.Street` are not, and are refused where they
are declared rather than silently reported under `Street`.

### Conditions: When and Unless

A rule chain that only applies in some states says so. `When` takes a predicate over the whole object,
and governs **the entire chain** regardless of where in it you write it:

```csharp
// Only an electric car has a battery, so the whole chain is skipped for every other engine.
this.Property(m => m.BatteryCapacityKwh)
    .NotNull()
    .GreaterThan(0m)
    .When(m => m.EngineType == EngineType.Electric);
```

`Unless` is the same thing negated, for the wording that reads better:

```csharp
this.Property(c => c.TradeInValue)
    .NotNull()
    .Unless(c => c.IsListedForSale);
```

Repeated conditions are **and**-ed, so two calls mean both have to hold. Because a condition governs the
whole chain rather than the rule it follows, a property whose rules need different conditions is
declared as two chains:

```csharp
this.Property(c => c.SoldDate).NotNull().When(c => c.Condition == CarCondition.Used);
this.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);
```

Where several chains share a condition, a block says it once, and `Otherwise` declares the chains for the
opposite case:

```csharp
this.When(m => m.EngineType == EngineType.Electric, () =>
{
    this.Property(m => m.BatteryCapacityKwh).NotNull().GreaterThan(0m);
})
.Otherwise(() =>
{
    this.Property(m => m.FuelConsumption).GreaterThan(0d);
});
```

The block's condition joins every chain declared inside it, ahead of each chain's own `When`, and is asked
for each of them — so keep it cheap. Blocks nest, `Unless` is the negated block, and both combine with
[`Group`](#rule-groups). The rules of the entries of a `ForEach` are declared on its own builder, which
has the same blocks.

A property whose condition did not hold reported nothing, which matters for
[validation behavior](#validation-behavior): there is nothing for a stopping run to stop on, and the next
property is still judged. A rule that depends on what the request is for rather than on what the object
holds is a [rule group](#rule-groups) instead, and one that depends on something only the caller knows
reads the [data the validation carries](#data-a-validation-carries).

### Rule groups

`When` asks about the object. Some rules depend on something the object cannot answer: what the request
is *for*. A car being taken in has to be appraised; the same car corrected a week later does not. That
is a rule group — a name a chain carries, and a name a call asks for:

```csharp
// Runs only where a validation asked for the Create group.
this.Property(c => c.TradeInValue)
    .LessThanOrEqualTo(c => c.PurchasePrice)
    .WithGroup("Create");
```

```csharp
var options = new NValidationOptions { ValidationGroups = "Create" };

var result = await validator.ValidateAsync(car, options);
```

**Every chain declared without a group is in the default group, and every validation runs the default
group** unless it says otherwise. So putting one chain in a group never switches another off, and a call
which asks for nothing is the validator it was before any group existed:

| The call asks for                     | What runs                                                |
|---------------------------------------|----------------------------------------------------------|
| nothing, or `ValidationGroups.None`   | the default group                                        |
| `"Create"`                            | the default group, and every chain in the `Create` group |
| `["Create", "Update"]`                | the default group, and every chain in either group       |
| `ValidationGroups.Only("Listing")`    | every chain in the `Listing` group, and nothing else     |
| `ValidationGroups.All`                | every chain the validator declares                       |

Where a whole section of a validator belongs to one group, `Group` says it once instead of ending every
chain with `WithGroup`:

```csharp
public sealed class CarValidator : Validator<Car>
{
    public CarValidator()
    {
        this.Property(c => c.Vin).NotEmpty();          // always

        this.Group("Create", () =>
        {
            this.Property(c => c.TradeInValue).NotNull();
            this.Property(c => c.IntakeCondition).NotNull();
        });
    }
}
```

The blocks nest, and a chain inside a nested one is in the groups of both. `WithGroup` written on a
chain inside a block adds to what the block gave it, and a chain declared after the block is in none of
its groups. The element builder of a `ForEach` has the same `Group`, for the rules of an entry.

A group covers **the whole chain** wherever it is written, exactly as `When` does. A chain the run did
not select reports nothing — its condition is not asked and its property is not even read — so there is
nothing for a [stopping run](#validation-behavior) to stop on, and the next property is still judged.

#### In the default group and another

`WithGroup` puts a chain in a group *instead of* the default group. Naming `ValidationGroups.DefaultGroup`
beside the other keeps it in both: it runs in every validation, and a selection of the other group alone
still finds it.

```csharp
// Checked in every validation, and by a listing check that asks for the Listing group alone.
this.Property(c => c.PurchasePrice)
    .GreaterThan(0m)
    .WithGroup(ValidationGroups.DefaultGroup, "Listing");
```

#### One group alone: Only

`ValidationGroups.Only(...)` runs the named groups without the default group — a listing check that asks
what a listing needs, and nothing else about the car:

```csharp
var options = new NValidationOptions { ValidationGroups = ValidationGroups.Only("Listing") };

var result = await validator.ValidateAsync(car, options);
```

What it reaches beyond the validator it was passed to:

- A nested validator, or the entries of a `ForEach`, that **declares** one of the groups runs those chains
  alone.
- One that declares none of them is validated as a plain call would validate it: it cannot say which of its
  rules belong to a group it has never heard of, so all of its default group is the answer.
- A chain in the default group whose nested validator declares one of the groups is **reached through** —
  the part of the chain that hands its value on runs, and the chain's own rules do not. So
  `Only("Pricing")` finds the `Pricing` chains of a model validator behind
  `this.Property(c => c.Model).NotNull().SetValidator(modelValidator)` without reporting a missing model.
- The inline rules and the validator of one `ForEach` answer as one: where either declares a selected
  group, the other is passed over.

**A selection that would run nothing is refused.** `Only` on a validator that declares none of the named
groups — a mistyped name, the wrong validator — throws `InvalidOperationException` naming the groups it
does declare, rather than checking nothing and passing. The additive forms stay lenient, so one options
object can serve validators that do not all declare the same groups. Mind where an exclusive selection is
set, though: on `NValidationOptions.Default`, or on a controller whose actions take different payloads,
every validator it is passed to has to declare one of its groups.

The selection belongs to the run rather than to the validator, which is why there is no
`ValidationGroups` to set on a validator: one validator serves every operation, and which of its rules
apply is what the caller knows. It reaches a nested validator and the element chain of a `ForEach` like
every other setting — see [the override ladder](#the-override-ladder) — so one selection on the
outermost call reaches the whole graph.

Names are compared exactly, so `"create"` and `"Create"` are two groups. Declare them as constants and
the compiler keeps the rules and the callers in step.

For a controller action, the group is named where the endpoint is declared rather than passed by hand —
see [`[ValidationGroups]`](#selecting-rule-groups-for-an-endpoint).

### Data a validation carries

Some rules depend on something the object cannot answer and the call cannot name as a group: the market a
car is listed in decides how far it may have been driven. The caller hands that over as data, and a rule
asks for it by its type:

```csharp
public sealed record ListingPolicy(int MaximumMileage, bool RequiresServiceHistory);

var options = new NValidationOptions
{
    ValidationData = [new ListingPolicy(MaximumMileage: 200_000, RequiresServiceHistory: true)],
};
```

```csharp
this.Property(c => c.Mileage)
    .Must<ListingPolicy>((car, mileage, policy) => mileage <= policy.MaximumMileage)
    .WithMessage("The mileage is above what this market lists.");

this.Property(c => c.ServiceHistory)
    .NotEmpty()
    .When<ListingPolicy>((car, policy) => policy.RequiresServiceHistory);
```

| Read with                                  | What it does                                                                         |
|--------------------------------------------|--------------------------------------------------------------------------------------|
| `When<TData>((obj, data) => ...)`          | A condition over the data, covering the whole chain like `When`                      |
| `this.When<TData>((obj, data) => ..., () => ...)` | The same as a block, for every chain declared inside                          |
| `Must<TData>((obj, value, data) => ...)`   | A one-off rule over the data, reporting `Must` like [`Must`](#a-one-off-rule-must)   |
| `context.TryGetData<TData>(out var data)`  | Inside `Add` or `AddAsync`, for a [rule of your own](#rulecontext)                   |

**A call that hands over no `TData` skips a chain that asks for one**, and `Must<TData>` passes: the
caller did not ask for what the data would decide. A value is found by its type, so declare a type of
your own for what you hand over. Two values that would both answer one request are refused — two of the
same type when the data is built, and two that are both a base type or interface when a rule asks for
it — because the answer would depend on the order they happened to be written in.

The data travels with the call: it reaches nested validators and the entries of a `ForEach`, and a
validator written by hand finds it on the options it is handed. It sits on the same
[ladder](#the-override-ladder) as the other settings, and the most specific level that names data
supplies all of it — levels are not merged. Options built once and reused cost nothing per call.

### Validating some properties

A request that changed two fields does not need to hear about the rest. `ValidationProperties` limits a
validation to the properties it names, spelled as failures are reported:

```csharp
var options = new NValidationOptions { ValidationProperties = ["PurchasePrice", "Model.Name"] };

// Or with expressions, so a rename is followed by the compiler.
var options = new NValidationOptions
{
    ValidationProperties = ValidationProperties.For<Car>(c => c.PurchasePrice, c => c.Model!.Name),
};
```

| A chain whose property is | Runs                                                                                                |
|---------------------------|-----------------------------------------------------------------------------------------------------|
| named, or below a name    | in full — `Model` validates the whole model                                                         |
| on the way to a name      | only the part that hands its value on — `Model.Name` runs the model validator's `Name` chain alone |
| anything else             | not at all                                                                                          |

A name without a position applies to every entry of a collection — `ServiceHistory.Workshop` — and a
name with one is refused. Names are compared ignoring case, because they usually come from a client, and
a name that matches nothing is passed by rather than refused, so a client's mistake does not become a
failed request. The selection applies on top of [rule groups](#rule-groups): a chain runs only where both
select it.

A rule comparing two properties belongs to the property it is declared on, so a request limited to
`PurchasePrice` does not re-check `TradeInValue`'s comparison against it. Name both where both matter.

### Collections and elements

Rules about the collection and rules about its elements go on the same chain, with `ForEach` last:

```csharp
this.Property(c => c.ServiceHistory)
    .NotEmpty()
    .MaximumCount(50)
    .ForEach(record => record.Property(r => r.Workshop).NotEmpty());
```

A chain belongs to the property it started on, so rules for a second property of the same entry are a
second statement — the element builder takes as many as the entry needs:

```csharp
this.Property(c => c.ServiceHistory)
    .ForEach(record =>
    {
        record.Property(r => r.Workshop).NotEmpty().MaximumLength(100);
        record.Property(r => r.Mileage).GreaterThanOrEqualTo(0);

        // A rule may consult the rest of the entry it is judging, but not the object the collection
        // hangs off; a rule about that belongs on the collection itself.
        record.Property(r => r.Mileage)
            .Must((r, mileage) => r.Cost == 0m || mileage > 0)
            .WithMessage("A paid service records its mileage.");
    });
```

`ForEach` is a rule like any other, so a chain that has already failed does not reach it — too many
entries is reported on its own, rather than alongside a complaint about each of them. It returns `void`,
so it is a statement and has to be declared last.

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

Where the element already has a validator, use it:

```csharp
this.Property(c => c.ServiceHistory).ForEach(serviceRecordValidator);
```

#### The element builder

Inside `ForEach` you are handed an `ElementRuleBuilder<TElement>`, whose whole surface is these members:

| Member                                     | What it does                                                                                           |
|--------------------------------------------|--------------------------------------------------------------------------------------------------------|
| `Property(expression)`                     | A rule chain on one property of the entry, exactly as on a validator                                   |
| `Element()`                                | A rule chain on the entry itself, for a collection of scalars                                          |
| `Group(name, rules)`                       | Puts the entry's chains declared inside in a [rule group](#rule-groups)                                |
| `When(predicate, rules)`, `Unless(...)`    | A [condition block](#conditions-when-and-unless) over the entry, with `Otherwise` for the other entries |
| `Where(predicate)`                         | Restricts which entries are judged at all; repeated calls are and-ed                                   |
| `SetValidator(validator)`                  | Hands each entry to a validator of its own                                                             |
| `WithIndexer(indexer)`                     | Identifies an entry by something other than its position                                               |
| `ValidationBehaviors`                      | What *one entry* reports — see [validation behavior](#validation-behavior)                             |

Everything but `ValidationBehaviors` is settled when the `ForEach` is declared: calling `SetValidator`,
`Where` or `WithIndexer` on the builder afterwards throws.

For a collection of scalars there is no property to name, so the element itself is the subject:

```csharp
this.Property(c => c.ServiceMileages).ForEach(mileage => mileage.Element().GreaterThanOrEqualTo(0));
// reports: ServiceMileages[1] -> "ServiceMileages[1] must be greater than or equal to 0."
```

A rule declared on the element itself names no property, so the message names the element by the very
name the failure is reported under. `WithDisplayName(...)` overrides that as it does anywhere else.

`Where(...)` restricts which elements are judged; the ones it skips keep their position, so an index
always points at the row the caller sent. Where a position is not what the caller matches on, identify
each element by something of its own:

```csharp
this.Property(c => c.ServiceHistory)
    .ForEach(record => record
        .WithIndexer((r, _) => $"{r.Mileage} km")
        .Property(r => r.Workshop).NotEmpty());

// reports: ServiceHistory[120000 km].Workshop instead of ServiceHistory[1].Workshop
```

What `WithIndexer` returns is echoed into the reported property name, so identify an entry by something
the caller already sent rather than by something the response should not be carrying.

The element builder carries its own [validation behavior](#validation-behavior):
`record.ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError }` governs what *one
entry* reports, and every entry is still walked. An axis left unset inherits from the validator the
`ForEach` was declared in, exactly as a nested validator does. Set it on the element builder itself where
an element's rules should follow a different policy.

A missing collection and a `null` element are skipped — whether entries have to be there at all is a
question for the collection's own rules.

Each collection rule walks the sequence once, and no further than its own question needs —
`MaximumCount(50)` stops at the fifty-first entry. A *chain* of them asks one question each, so
`NotEmpty().MaximumCount(50).ForEach(...)` walks it three times. That is free for a `List<T>` or an array,
which answer `Count` without being walked at all, but a property typed `IEnumerable<T>` backed by a live
query runs that query once per rule, and one that cannot be enumerated twice will throw. Materialize such
a property before validating it; the library cannot do it for you without handing your own rules a
different object than the one your model holds.

Messages about an element can name its position with `{CollectionIndex}`, whichever way the rules were
declared — an entry's own validator answers through the provider of the run it was composed into, not its
own.

### Validation behavior

How much a validator reports is one decision asked at two scales: whether a run keeps going once a
property has reported, and whether a property's chain keeps going once one of its rules has failed. Both
live under one setting, and the level it applies to is where you write it rather than a word in its name:

```csharp
// the registration — the default for every validator resolved from the container
services.AddNValidation(o =>
{
    o.ValidationBehaviors = new() { Class = ValidationBehavior.StopAtFirstError };
});

// the validator
public sealed class CarValidator : Validator<Car>
{
    public CarValidator()
    {
        this.ValidationBehaviors = new() { Property = ValidationBehavior.All };

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

`ValidationBehaviors` is a small value whose two axes are nullable, and `null` — which is what they
start as — means *inherit from the level below*. Naming one therefore never silently changes the other:
`new() { Class = ... }` leaves `Property` taking whatever the level below configured, and a level that
sets neither leaves both to [the options this call or this process was given](#validation-options), and
failing those the defaults above.

The defaults are chosen for the common case. Reporting every property at once is what lets a caller fix a
form in one pass. Stopping within a chain is right because a chain's rules usually run coarse to fine: an
empty string fails `NotEmpty` and `Length(17)` alike, and only the first of those tells the caller
anything. Set `Property` to `All` for the chain whose rules judge separate things — a VIN is the wrong
length *and* carries a letter no VIN may contain, and a caller wants to hear both.

**`Class = StopAtFirstError` stops the run as soon as anything has been reported** — the properties after
it are never looked at. It therefore also stops the chain that produced it, overriding `Property`, since a
setting by that name which went on judging the same property would be a trap. The single exception is a
chain that says otherwise through `WithValidationBehavior`, because it says so at the point it applies;
that is how you get *everything about the first field that is wrong, then stop*. A property whose `When`
did not hold — or whose [rule group](#rule-groups) the run did not select — reported nothing, so there is
nothing for the run to stop on and the next property is still judged.

That usually means a single message, but do not rely on it as a cap. A run is stopped *between* rules, and
one rule that reported several at once is not cut short: a `ForEach` reports on every entry it walked, and
a validator merged in with `SetValidator` which decided for itself to report everything is passed on
whole. One which decided nothing inherits the stopping run and stops at its own first error, so the whole
graph answers with the first thing that is wrong.

The setting is resolved while validating, not while the rules are declared, so where in a constructor you
write it makes no difference. A validator composed into another — through `SetValidator`, or per entry
through `ForEach` — inherits what its composer resolved, unless it declared otherwise for itself.

One caution for an HTTP payload: a stopping validator produces a problem details body naming a single
field. That is often right for a machine caller, and usually wrong for a form a person is filling in.

## Validation results

A run answers with a `ValidationResult`. It is immutable, it is never null, and a successful one carries
an empty list rather than a null one.

| Member                                       | Type                                    | What it is                                                                                                           |
|----------------------------------------------|-----------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| `Succeeded`                                  | `bool`                                  | `true` when nothing was reported                                                                                     |
| `Errors`                                     | `IReadOnlyList<ValidationError>`        | Every failure, in the order the rules produced them                                                                  |
| `ThrowIfInvalid()`                           | `void`                                  | Throws a `ValidationException` when it failed; does nothing when it did not                                          |
| `ToErrorsDictionary()`                       | `IReadOnlyDictionary<string, string[]>` | The messages grouped by property name                                                                                |
| `ValidationResult.Success`                   | `static ValidationResult`               | The shared empty result                                                                                              |
| `ValidationResult.FromValidationErrors(...)` | `static ValidationResult`               | Builds one from errors you produced yourself — takes `params ValidationError[]` or an `IEnumerable<ValidationError>` |

`ToErrorsDictionary()` is the shape a client expects and the shape the problem details response carries:

```csharp
var errors = result.ToErrorsDictionary();

// { "Vin": ["Vin is required."],
//   "Model.Manufacturer.Name": ["Name is required."] }
```

It returns an empty dictionary for a successful result rather than throwing, so a caller can hand it
straight to whatever renders it.

### What one error says

A `ValidationError` answers three questions:

| Member         | Answers               | Example                                                        |
|----------------|-----------------------|----------------------------------------------------------------|
| `PropertyName` | where the failure is  | `Vin`, `Model.Manufacturer.Name`, `ServiceHistory[1].Workshop` |
| `ErrorCode`    | which rule failed     | `NotEmpty`, `GreaterThan`                                      |
| `Message`      | what to show a reader | `Vin is required.`                                             |

`PropertyName` is the C# property path, so a client can bind each message to the input it belongs to.
`ErrorCode` is what a client branches on, because it does not change when a translation lands — which
matters here, since `NotEmpty`, `NotNull` and `NotDefault` all render the same English sentence.
`Arguments` carries the values the message was rendered from (`{PropertyName}`, `{MinLength}` and so on)
for a caller that logs a failure in parts rather than as a sentence.

```csharp
public ValidationError(string propertyName, string message);
public ValidationError(string propertyName, string message, string? errorCode);
public ValidationError(string propertyName, string message, string? errorCode,
                       IReadOnlyDictionary<string, object?>? arguments);
```

`PropertyName` and `Message` are always there. `ErrorCode` is null only for an error built with the
two-argument constructor, and `Arguments` is null for one whose message did not come from a template.
Everything the shipped rules report carries all four.

### Where a failure is reported

The reported name is the member path of the expression the chain was declared with, and it is built the
same way wherever the failure came from:

| Declared                                                        | Reported                     |
|-----------------------------------------------------------------|------------------------------|
| `this.Property(c => c.Vin)`                                     | `Vin`                        |
| `this.Property(c => c.Model.Manufacturer.Name)`                 | `Model.Manufacturer.Name`    |
| `SetValidator` on `Model`, which reports `Name`                 | `Model.Name`                 |
| `ForEach` over `ServiceHistory`, whose entry reports `Workshop` | `ServiceHistory[1].Workshop` |
| `Element()` on a collection of scalars                          | `ServiceMileages[1]`         |

A nested validator keeps its own flat names and the composer prefixes them, so the same validator reports
`Name` on its own and `Model.Manufacturer.Name` two levels down. `WithPropertyName` overrides the whole
path for one property — see [overriding defaults](#overriding-defaults).

### Throwing instead of returning

`result.ThrowIfInvalid()` raises a `ValidationException` carrying the same errors grouped by property
name. `ValidateAndThrowAsync` is the same thing in one call, for a caller that would rather treat a
failure as an exception than as a result to inspect:

```csharp
await this.carValidator.ValidateAndThrowAsync(car, cancellationToken);

// equivalent to
var result = await this.carValidator.ValidateAsync(car, cancellationToken);
result.ThrowIfInvalid();
```

The exception carries the failures in the same shape as `ToErrorsDictionary()`:

```csharp
public IReadOnlyDictionary<string, string[]> Errors { get; }
```

Its `Message` is every failure's message joined with a space, which is what ends up in a log line. Two
further constructors take a message — with or without an inner exception — for an application that raises
one itself; those carry no errors. Constructing one from an empty dictionary is refused, because an
exception saying nothing failed is a bug at the point it is thrown rather than at the point it is caught.

```csharp
try
{
    await this.carValidator.ValidateAndThrowAsync(car, cancellationToken);
}
catch (ValidationException exception)
{
    foreach (var (propertyName, messages) in exception.Errors)
    {
        this.logger.LogWarning("{PropertyName}: {Messages}", propertyName, string.Join(" ", messages));
    }
}
```

In an ASP.NET Core application you rarely catch it: register the exception handler and it comes out as a
problem details response — see [ASP.NET Core integration](#aspnet-core-integration).

## Built-in validators

The rules are grouped by the question they ask rather than listed alphabetically, because that is how you
go looking for one: *is it there*, *is it the right shape*, *is it in range*, *is it one of these*.

| Group                               | Rules                                                                                                                                                          |
|-------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [Presence](#presence)               | `NotNull`, `NotEmpty`, `NotDefault`                                                                                                                            |
| [Text](#text)                       | `MinimumLength`, `MaximumLength`, `Length`, `Matches`, `NotContaining`                                                                                         |
| [Email addresses](#email-addresses) | `EmailAddress`, refined by `AllowQuotedLocalPart`, `AllowAddressLiteral`, `AllowSingleLabelDomain`, `RequireTopLevelDomain`, `RefuseTopLevelDomain`            |
| [URLs](#urls)                       | `Url`, refined by `AllowUserInfo`, `AllowIPAddress`, `AllowLoopback`, `AllowSingleLabelHost`, `AllowRelative`, `RequireScheme`, `RequireHost`, `RequireDomain` |
| [Comparison](#comparison)           | `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `Between`, `EqualTo`, `NotEqualTo`, `OneOf`                                            |
| [Numbers](#numbers)                 | `MultipleOf`, `NotNaN`, `PrecisionScale`                                                                                                                       |
| [Dates](#dates)                     | `InThePast`, `InTheFuture`                                                                                                                                     |
| [Collections](#collections)         | `MinimumCount`, `MaximumCount`, `NoDuplicates`, `ForEach`                                                                                                      |
| [Enums](#enums)                     | `IsInEnum`                                                                                                                                                     |
| [Custom](#custom-validators)        | `Must`, `Must<TData>`, `MustAsync`, `SetValidator`                                                                                                             |

Two things hold for every rule below, and are worth reading once rather than thirty-five times:

- **Anything absent passes.** A null value, a missing collection, a property reached through an object the
  payload omitted — none of them is a failure of the rule that was asked about the *value*. Requiring
  presence is always a rule of its own. The three presence rules are the only ones that depart from this,
  which is what they are for.
- **The error code is also the key the message is resolved under.** Each card names the code it reports
  and the placeholders its message may use; [localization](#localization) is a matter of answering for
  those codes.

Every card below shows the signature, what makes it fail, an example against the same `Car` domain, and
the code and placeholders it reports.

### Presence

Three rules, one English sentence between them — *"{PropertyName} is required."* They are separate rules
because they ask genuinely different questions, and a client that has to tell them apart branches on the
`ErrorCode` rather than on the text.

**`NotNull()`** — a reference type, or a `Nullable<T>`

Fails when the value is null, and asks nothing else. This is the rule that makes a nested object
mandatory.

```csharp
this.Property(c => c.Model).NotNull();
this.Property(m => m.BasePrice).NotNull();     // decimal?
```

Error code `NotNull` · `{PropertyName}`

**`NotEmpty()`** — `string?`, or any `IEnumerable`

Asks whether there is any content, which only a string or a collection can answer. **Fails on null**, and
for a string also on one that is only whitespace.

```csharp
this.Property(c => c.Vin).NotEmpty();
this.Property(c => c.ServiceHistory).NotEmpty();
```

Error code `NotEmpty` · `{PropertyName}`

**`NotDefault()`** — any `struct`, or a `Nullable<T>` of one

Asks whether a value type was set at all — an enum's zero member, a `DateTime.MinValue`, an empty `Guid`
— which is what arrives when nothing was chosen. The nullable form fails for null as well.

```csharp
this.Property(c => c.FirstRegistration)
    .WithDisplayName("Registration date")
    .NotDefault();
```

Error code `NotDefault` · `{PropertyName}`

A reference type has no `default` worth asking about, so `NotDefault` does not apply to one — use
`NotNull`.

### Text

**`MinimumLength(int minimumLength)`** — `string?`

Fails when the value is shorter than `minimumLength`. Whitespace counts; the value is not trimmed first.

```csharp
this.Property(c => c.Vin).MinimumLength(11);
```

Error code `MinimumLength` · `{PropertyName}`, `{MinLength}`

**`MaximumLength(int maximumLength)`** — `string?`

Fails when the value is longer than `maximumLength`.

```csharp
this.Property(m => m.Name).MaximumLength(100);
```

Error code `MaximumLength` · `{PropertyName}`, `{MaxLength}`

**`Length(int length)`** — `string?`

Fails when the value is not exactly `length` characters long. For a code of a fixed width — an ISO country
code, a VIN — this says in one rule what a minimum and a maximum say in two.

```csharp
this.Property(m => m.CountryCode).Length(3);
```

Error code `Length` · `{PropertyName}`, `{Length}`

**`Length(int minimumLength, int maximumLength)`** — `string?`

Fails outside the range; **both bounds are inclusive**. Note that this overload reports **`LengthBetween`**, not
`Length` — the two say different things, so they resolve different messages.

```csharp
this.Property(c => c.RegistrationPlate).Length(2, 10);
```

Error code `LengthBetween` · `{PropertyName}`, `{MinLength}`, `{MaxLength}`

**`Matches(string pattern)`**, **`Matches(string pattern, RegexOptions options)`**, **`Matches(Regex regex)`**
— `string?`

Fails when the pattern does not match. A value that is null **or only whitespace** passes, so a pattern
never doubles as a presence rule.

```csharp
this.Property(c => c.Vin).Matches("^[A-HJ-NPR-Z0-9]{17}$");
```

The pattern is compiled once where the rule is declared, not once per validation, and it carries a **one
second match timeout**. A pattern that times out counts as *not matching* — a hostile value gets a
validation failure rather than an unhandled exception on the request thread.

Error code `Matches` · `{PropertyName}`, and `{Pattern}` — which the built-in English does not use, on the
grounds that a regular expression is not an explanation, but which your own provider can pick up.

**`NotContaining(params string[] values)`**, **`NotContaining(StringComparison comparison, params string[] values)`** —
`string?`

Fails when the value contains any of the terms. The general form for text a field will not carry, wherever
it comes from:

```csharp
this.Property(m => m.Name).NotContaining("prototype", "internal");
```

It compares without regard to case by default, and the message names none of the terms — a blocklist that
reports its own entries is one the next value works around. Pass a `StringComparison` first where the
comparison should be exact.

Error code `NotContaining` · `{PropertyName}`

### Email addresses

**`EmailAddress()`** — `string?`

`EmailAddress` reads the value against RFC 5321's grammar for a mailbox with a scanner of its own rather
than a pattern: one pass over the characters, nothing allocated, and nothing a hostile value can make it
backtrack on. What it accepts by default is the everyday shape — a dot-separated local part, one `@`,
then a domain of two or more labels — because that is what a contact-email field means, and the legal
forms it leaves out are ones no provider issues.

```csharp
this.Property(m => m.ContactEmail).EmailAddress();
```

The value has to be the address **alone**: `Foo <a@b.com>`, `a@b.com, c@d.com` and a value with
surrounding whitespace are each something other than the single address the field asked for, and are
rejected. So are the things that look like addresses and are not: a trailing dot in the local part, a
domain label beginning or ending with a hyphen, a bracketed domain that is not an IP address, a local
part over 64 octets or a domain over 255. A domain beyond ASCII — `verkauf@aurora-motörs.example` — is
checked against IDNA rather than waved through.

Error code `EmailAddress` · `{PropertyName}`

`EmailAddress()` returns a builder of its own, carrying the refinements below. Write them right after
it: every other rule and decoration returns the plain builder, which does not have them, so a
refinement in the wrong place is a compile error rather than a surprise.

Three admit a legal form the plain rule refuses:

| Refinement                 | Admits                                                                        |
|----------------------------|-------------------------------------------------------------------------------|
| `AllowQuotedLocalPart()`   | `"john doe"@example.com` — and with it `"a@b"@example.com`                    |
| `AllowAddressLiteral()`    | `parts@[192.0.2.1]`, `parts@[IPv6:2001:db8::1]`, checked to be real addresses |
| `AllowSingleLabelDomain()` | `root@localhost`                                                              |

All three together are exactly what RFC 5321 calls a `Mailbox`:

```csharp
this.Property(m => m.ContactEmail)
    .EmailAddress()
    .AllowQuotedLocalPart()
    .AllowAddressLiteral()
    .AllowSingleLabelDomain();
```

Which domains you accept is a separate decision, and two refinements carry it:

```csharp
this.Property(m => m.ContactEmail)
    .NotEmpty()
    .EmailAddress()
    .RefuseTopLevelDomain("test", "invalid", "example");
```

**`RequireTopLevelDomain(params string[] topLevelDomains)`** — the address must sit under one of those
listed. Entries are compared without regard to case and a leading dot is optional, so `.ch` and `ch`
mean the same thing. An address with no top-level domain — a single label, or an address literal — is
under none of them and fails.

Error code **`EmailTopLevelDomain`** · `{PropertyName}`, `{TopLevelDomains}` — the entries as they were
written, without their dots and without repeats.

**`RefuseTopLevelDomain(params string[] topLevelDomains)`** — the blocklist counterpart. An address with
no top-level domain has nothing on the list and passes.

Error code **`EmailTopLevelDomainNotAllowed`** · `{PropertyName}`, `{TopLevelDomain}` — the one that
matched, as the address wrote it.

The refinements change the one rule `EmailAddress()` declared rather than adding rules of their own, so
a value is parsed once and reports at most one failure: a value that is not an address reports that, and
nothing about its domain.

A syntax rule says an address is *well-formed*. It does not say the address exists, or that the person
typing it owns it; the only check that settles either is a message to it with a link to confirm.

### URLs

**`Url()`** — `string?`

`Url` reads the value against RFC 3986 with a scanner of its own rather than a pattern: one pass over the
characters, nothing allocated, and nothing a hostile value can make it backtrack on. What it accepts by
default is a URL that names a host — `http` or `https`, then a host of two or more labels, then whatever
path, query and fragment it carries — because that is what a link field means.

```csharp
this.Property(m => m.Website).Url();
```

Handing the value to `Uri` instead would accept a great deal a link field never means, and none of it
could be tightened from outside, because that parser *is* the definition. It reads `javascript:alert(1)`
and `data:text/html;base64,…` as URLs. It trims surrounding whitespace. It reads `/orders/42` as an
absolute `file:` URL on Linux and macOS but not on Windows, so the same payload would get a different
verdict per platform. And it rewrites what it just approved: `http://0x7f.1/` becomes `127.0.0.1`,
`…/%zz` becomes `…/%25zz`, and `http://example.com@evil.example/` points at `evil.example`.

So the value has to be one URL **alone**, and one that says what it means:

| Refused                                                                    | Because                                        |
|----------------------------------------------------------------------------|------------------------------------------------|
| `https://example.com `, `https://exa mple.com/`, a tab or newline anywhere | whitespace is not part of a URL                |
| `https://example.com/%zz`, `…/%2`                                          | a percent-escape that is not one               |
| `https://-example.com/`, `https://example-.com/`, `https://example.com./`  | a label begins and ends with a letter or digit |
| `http://my_host.example/`                                                  | an underscore is not a host-name character     |
| `http://1.2.3/`, `http://0x7f.1/`, `http://2130706433/`                    | shaped like an address, and not one            |
| `//example.com/path`, `/orders/42`                                         | no scheme, which `AllowRelative()` covers      |
| `javascript:alert(1)`, `mailto:a@b.com`, `about:blank`                     | no host, so there is nothing to point at       |

Error code `Url` · `{PropertyName}`

A host whose last label is all digits is read as an address and never as a name, which is what tells
`192.0.2.1` from `example.com`: no top-level domain is numeric. A host beyond ASCII —
`https://aurora-motörs.example/` — is checked against IDNA rather than waved through, by the same grammar
`EmailAddress()` reads a domain with.

`Url()` returns a builder of its own, carrying the refinements below. Write them right after it: every
other rule and decoration returns the plain builder, which does not have them, so a refinement in the
wrong place is a compile error rather than a surprise.

Five admit a form the plain rule refuses:

| Refinement               | Admits                                                                         |
|--------------------------|--------------------------------------------------------------------------------|
| `AllowUserInfo()`        | `https://user:pass@example.com/`                                               |
| `AllowIPAddress()`       | `http://192.0.2.1/`, `http://[2001:db8::1]/`, checked to be real addresses     |
| `AllowLoopback()`        | `http://localhost:5001/`, anything in `127.0.0.0/8`, `[::1]`, and nothing else |
| `AllowSingleLabelHost()` | `http://intranet/`                                                             |
| `AllowRelative()`        | `/orders/42`, `../a`, `?page=2`, `#top`                                        |

The plain rule refuses credentials because `https://example.com@evil.example/` reads to a person as a URL
for `example.com` and points somewhere else; opt in only where the field is known to carry them.

`AllowLoopback()` is narrower than the two around it on purpose, so a development configuration can take
`http://localhost:5001` without also taking every bare name and every address.

`AllowRelative()` still refuses `//example.com/x`, although that is a legal relative reference: it names a
host, which is how a field meant to hold a path sends a reader somewhere else.

Which schemes and hosts you accept is a separate decision, and three refinements carry it:

```csharp
this.Property(m => m.Website)
    .NotEmpty()
    .Url()
    .RequireScheme("https")
    .RequireDomain("aurora-motors.example");
```

**`RequireScheme(params string[] schemes)`** — replaces the `http` and `https` the plain rule allows
rather than adding to them. Entries are compared without regard to case, and may be written with or
without their trailing `:` or `://`.

Error code **`UrlScheme`** · `{PropertyName}`, `{Schemes}`. A well-formed URL whose scheme is not allowed
reports this rather than `Url`, which is what lets the message name the schemes it allows even where
nobody wrote `RequireScheme`: `ftp://files.example.com/a` is told apart from text that is not a URL.

**`RequireHost(params string[] hosts)`** — the host must be one of those listed, exactly.
`api.example.com` does not satisfy `example.com`.

**`RequireDomain(params string[] domains)`** — the host must be one of those listed, or a label under one.
The boundary is a label, so `api.example.com` sits under `example.com` and `evil-example.com` does not.
Entries may be written with or without their leading dot.

Error code **`UrlHost`** · `{PropertyName}`, `{Hosts}` — both lists, as they were written. Where both are
declared, a host satisfying either one passes.

The refinements change the one rule `Url()` declared rather than adding rules of their own, so a value is
parsed once and reports at most one failure: a value that is not a URL reports that, and nothing about its
scheme or its host. A relative reference names neither, so the scheme and host rules have nothing to judge
and pass it.

A syntax rule says a URL is *well-formed*. It does not say it resolves, and it does not say it is safe to
fetch: the host is resolved when the request is made, a name can point anywhere, and a redirect can move
it again. Guarding a request against where it ends up is the job of the code making the request, and no
rule about the text can stand in for it.

### Comparison

The comparison rules are written once, over `IComparable<T>`, so they work for every numeric type and for
`DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly` and `TimeOnly` alike:

```csharp
this.Property(c => c.Mileage).GreaterThanOrEqualTo(0);
this.Property(m => m.BasePrice).Between(1m, 999_999m);
this.Property(m => m.UnitsProduced).GreaterThan(1_000L);
this.Property(c => c.ServiceInterval).LessThanOrEqualTo(TimeSpan.FromDays(365));
```

They are constrained to `struct, IComparable<T>`, which is to say they order values, not text. Two strings
are compared with `EqualTo`/`NotEqualTo` below; ordering them is a `Must`.

**`GreaterThan(value)`**, **`GreaterThanOrEqualTo(value)`**, **`LessThan(value)`**, **`LessThanOrEqualTo(value)`**

Each fails when the comparison does not hold. Each has six overloads: the property may be `TValue` or
`TValue?`, and the thing compared against may be a constant or another property, itself of either
nullability.

| Rule                   | Against a value        | Against another property            |
|------------------------|------------------------|-------------------------------------|
| `GreaterThan`          | `GreaterThan`          | `GreaterThanOtherProperty`          |
| `GreaterThanOrEqualTo` | `GreaterThanOrEqualTo` | `GreaterThanOrEqualToOtherProperty` |
| `LessThan`             | `LessThan`             | `LessThanOtherProperty`             |
| `LessThanOrEqualTo`    | `LessThanOrEqualTo`    | `LessThanOrEqualToOtherProperty`    |

`{PropertyName}` and `{OtherValue}` for the value forms; `{PropertyName}` and `{OtherPropertyName}` for
the other-property forms.

Each of them — and `EqualTo`/`NotEqualTo` with it — compares against another property of the same object,
on either side of which the value may be optional. A missing value has nothing to compare and passes, and
so does a property reached through an object the payload omitted:

```csharp
this.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);
this.Property(c => c.Mileage).LessThanOrEqualTo(c => c.Model.WarrantyMileageCap);   // skipped when Model is absent
```

Requiring a value is a separate decision, and `NotNull()` or `NotEmpty()` is what makes it.

The failure is always reported under the property the chain was declared for, never under the one it was
compared against. `{OtherPropertyName}` renders the *other* property's display name when it declared one,
which is what makes a cross-property message readable:

```csharp
this.Property(c => c.FirstRegistration).WithDisplayName("Registration date").NotDefault();
this.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);

// reports: SoldDate -> "SoldDate must be greater than or equal to Registration date."
```

**`Between(TValue from, TValue to)`**, **`Between(from, to, bool inclusive)`**, **
`Between(from, to, bool inclusiveFrom, bool inclusiveTo)`**

Fails outside the range. With no bool, **both bounds are inclusive**. Which bounds are included changes
which code is reported, because a message naming a range it has just refused would be worse than useless:

| `inclusiveFrom` | `inclusiveTo` | Error code             | Default English                                                  |
|-----------------|---------------|------------------------|------------------------------------------------------------------|
| `true`          | `true`        | `Between`              | *{PropertyName} must be between {From} and {To}.*                |
| `false`         | `false`       | `BetweenExclusive`     | *{PropertyName} must be greater than {From} and less than {To}.* |
| `false`         | `true`        | `BetweenExclusiveFrom` | *{PropertyName} must be greater than {From} and at most {To}.*   |
| `true`          | `false`       | `BetweenExclusiveTo`   | *{PropertyName} must be at least {From} and less than {To}.*     |

```csharp
this.Property(m => m.SeatCount).Between(1, 9);                       // 1 and 9 both allowed
this.Property(m => m.FuelConsumption).Between(0d, 50d, inclusive: false);
```

`{PropertyName}`, `{From}`, `{To}` in all four cases. A `from` greater than `to` throws where the rule is
declared rather than failing every value at run time.

**`EqualTo(value)`** and **`NotEqualTo(value)`** — a `struct` implementing `IEquatable<T>`, or a `string?`

These are the only comparison rules that take text, and they take a `StringComparison` with it —
`Ordinal` by default:

```csharp
this.Property(m => m.CountryCode).EqualTo("CHE");
this.Property(c => c.RegistrationPlate)
    .NotEqualTo(c => c.PreviousRegistrationPlate, StringComparison.OrdinalIgnoreCase);
```

A null string passes both, so `NotEqualTo("X")` does not fire on a value that is not there.

| Rule         | Against a value | Against another property  |
|--------------|-----------------|---------------------------|
| `EqualTo`    | `EqualTo`       | `EqualToOtherProperty`    |
| `NotEqualTo` | `NotEqualTo`    | `NotEqualToOtherProperty` |

**`OneOf(params TValue[] values)`** — any value type, `string`, and their nullable forms

Fails when the value is not one of the values named. The closed-set rule: an enum a client may send a
number for, a currency the endpoint settles in, a status the contract froze.

```csharp
this.Property(c => c.Condition).OneOf(CarCondition.New, CarCondition.Used);
this.Property(c => c.Currency).OneOf(StringComparison.OrdinalIgnoreCase, "CHF", "EUR");
```

Error code `OneOf` · `{PropertyName}`, `{AllowedValues}`

The message names the allowed values, unlike `NotContaining`, which names none of the ones it refuses:
an allowlist may say what it allows, while a blocklist that reports its own entries is one the next
value is written around. Naming no values at all throws where the rule is declared.

### Numbers

**`MultipleOf(decimal step)`**, **`MultipleOf(int step)`** — `decimal`, `decimal?`, `int`, `int?`

Fails when the value is not a whole multiple of `step`. The remainder is exact — there is no epsilon — so
this is a rule about `decimal` and `int`, and deliberately not about `double`.

```csharp
this.Property(c => c.ServiceIntervalKm).MultipleOf(1_000);
this.Property(c => c.PurchasePrice).MultipleOf(0.05m);
```

Error code `MultipleOf` · `{PropertyName}`, `{Step}`

A step of zero or less throws where the rule is declared. `{Step}` takes a format specifier like any other
placeholder, so a provider can write `{Step:0.00}`.

**`PrecisionScale(int precision, int scale)`** — `decimal`, `decimal?`

Fails when the number carries more digits than the contract behind it holds: at most `precision` in
total, of which at most `scale` follow the decimal point. Typically the shape of the column it lands in.

```csharp
this.Property(c => c.PurchasePrice).PrecisionScale(9, 2);   // up to 9,999,999.99
```

Error code `PrecisionScale` · `{PropertyName}`, `{Precision}`, `{Scale}`

Trailing zeros are representation rather than value, so `1.50m` and `1.5m` are judged the same: there is
one behaviour rather than a flag to pick between two. A precision of zero or less, a negative scale, or
a scale larger than the precision all throw where the rule is declared.

**`NotNaN()`** — `double`, `double?`, `float`, `float?`

Fails for `NaN`, which is what a figure that was never measured deserializes as. Infinities pass; a bound
on those is a comparison rule.

```csharp
this.Property(m => m.FuelConsumption).NotNaN();
```

Error code `NotNaN` · `{PropertyName}`

### Dates

**`InThePast()`**, **`InThePast(TimeProvider timeProvider)`**, **`InTheFuture()`**, **
`InTheFuture(TimeProvider timeProvider)`**
— `DateTime`, `DateTime?`, `DateTimeOffset`, `DateTimeOffset?`

`InThePast` and `InTheFuture` compare in UTC, and each has an overload taking a `TimeProvider`, so a test
can decide what "now" is:

```csharp
this.Property(m => m.FoundedDate).InThePast(this.timeProvider);
this.Property(c => c.NextServiceAt).InTheFuture();
```

Both are **strict**: a value equal to "now" fails either of them. A `DateTime` of kind `Unspecified` —
what a date deserialized without an offset carries — is read as UTC rather than as local time, so the same
payload gets the same verdict whatever time zone the host runs in. Use `DateTimeOffset` where the input
genuinely carries one.

Error codes `InThePast` and `InTheFuture` · `{PropertyName}`

### Collections

**`MinimumCount(int minimumCount)`** and **`MaximumCount(int maximumCount)`** — any `IEnumerable`

Fail when the sequence holds too few or too many entries. Each walks the sequence no further than its own
question needs, so `MaximumCount(20)` stops at the twenty-first entry rather than counting the lot.

```csharp
this.Property(c => c.ServiceHistory).MaximumCount(20);
this.Property(c => c.FeatureIds).MinimumCount(1);
```

Error codes `MinimumCount` and `MaximumCount` · `{PropertyName}` with `{MinCount}` / `{MaxCount}`

**`NoDuplicates()`** — any `IEnumerable`

Fails when two entries are equal. Equality is the entries' own — which means a value type is boxed on the
way in, and a reference type without an `Equals` of its own compares by identity. There is no comparer
overload: a rule about equality that is not the type's own equality is a `Must` over a typed `HashSet<T>`,
where the reader can see which equality was meant.

```csharp
this.Property(c => c.FeatureIds).NoDuplicates();
```

Error code `NoDuplicates` · `{PropertyName}`

**`ForEach(Action<ElementRuleBuilder<TElement>> declareRules)`** and **`ForEach(IValidator<TElement> validator)`**

Judges each entry, reporting under its position. It returns `void`, so it terminates the chain and is
declared last. See [collections and elements](#collections-and-elements) for the element builder and for
what an entry's failures are reported under.

### Enums

**`IsInEnum()`** — any `enum`, or a `Nullable<T>` of one

Fails for a value the enum does not declare, which is what an out-of-range integer deserializes into:

```csharp
this.Property(m => m.EngineType).IsInEnum();
this.Property(c => c.IntakeCondition).IsInEnum();     // CarCondition?
```

Error code `IsInEnum` · `{PropertyName}`

A `[Flags]` enum accepts any combination of the bits it declares, and fails for a value carrying a bit it
does not. Zero is a *valid* member whenever the enum declares one, so an enum whose zero means "nothing
chosen" pairs this with `NotDefault()`.

### Error codes and their messages

Every code the core can report, the English the built-in provider answers with, and the placeholders that
message is rendered from. This is also the checklist a provider of your own has to answer for — and
`ShouldResolveEveryCoreErrorCode` is how a test holds it to that, so the table does not have to be copied
anywhere.

| Error code                          | Default English message                                                            | Placeholders                                   |
|-------------------------------------|------------------------------------------------------------------------------------|------------------------------------------------|
| `Must`                              | {PropertyName} is not valid.                                                       | `{PropertyName}`                               |
| `NotEmpty`                          | {PropertyName} is required.                                                        | `{PropertyName}`                               |
| `NotNull`                           | {PropertyName} is required.                                                        | `{PropertyName}`                               |
| `NotDefault`                        | {PropertyName} is required.                                                        | `{PropertyName}`                               |
| `NotNaN`                            | {PropertyName} must be a number.                                                   | `{PropertyName}`                               |
| `MinimumLength`                     | {PropertyName} must be at least {MinLength} characters long.                       | `{PropertyName}`, `{MinLength}`                |
| `MaximumLength`                     | {PropertyName} must not exceed {MaxLength} characters.                             | `{PropertyName}`, `{MaxLength}`                |
| `Length`                            | {PropertyName} must be exactly {Length} characters long.                           | `{PropertyName}`, `{Length}`                   |
| `LengthBetween`                     | {PropertyName} must be between {MinLength} and {MaxLength} characters long.        | `{PropertyName}`, `{MinLength}`, `{MaxLength}` |
| `Matches`                           | {PropertyName} has an invalid format.                                              | `{PropertyName}`, `{Pattern}`                  |
| `EmailAddress`                      | {PropertyName} is not a valid email address.                                       | `{PropertyName}`                               |
| `EmailTopLevelDomain`               | {PropertyName} must use one of the following top-level domains: {TopLevelDomains}. | `{PropertyName}`, `{TopLevelDomains}`          |
| `EmailTopLevelDomainNotAllowed`     | {PropertyName} must not use the top-level domain {TopLevelDomain}.                 | `{PropertyName}`, `{TopLevelDomain}`           |
| `Url`                               | {PropertyName} is not a valid URL.                                                 | `{PropertyName}`                               |
| `UrlScheme`                         | {PropertyName} must use one of the following schemes: {Schemes}.                   | `{PropertyName}`, `{Schemes}`                  |
| `UrlHost`                           | {PropertyName} must point at one of the following hosts: {Hosts}.                  | `{PropertyName}`, `{Hosts}`                    |
| `NotContaining`                     | {PropertyName} contains text that is not allowed.                                  | `{PropertyName}`                               |
| `GreaterThan`                       | {PropertyName} must be greater than {OtherValue}.                                  | `{PropertyName}`, `{OtherValue}`               |
| `GreaterThanOrEqualTo`              | {PropertyName} must be greater than or equal to {OtherValue}.                      | `{PropertyName}`, `{OtherValue}`               |
| `LessThan`                          | {PropertyName} must be less than {OtherValue}.                                     | `{PropertyName}`, `{OtherValue}`               |
| `LessThanOrEqualTo`                 | {PropertyName} must be less than or equal to {OtherValue}.                         | `{PropertyName}`, `{OtherValue}`               |
| `Between`                           | {PropertyName} must be between {From} and {To}.                                    | `{PropertyName}`, `{From}`, `{To}`             |
| `BetweenExclusive`                  | {PropertyName} must be greater than {From} and less than {To}.                     | `{PropertyName}`, `{From}`, `{To}`             |
| `BetweenExclusiveFrom`              | {PropertyName} must be greater than {From} and at most {To}.                       | `{PropertyName}`, `{From}`, `{To}`             |
| `BetweenExclusiveTo`                | {PropertyName} must be at least {From} and less than {To}.                         | `{PropertyName}`, `{From}`, `{To}`             |
| `EqualTo`                           | {PropertyName} must be {OtherValue}.                                               | `{PropertyName}`, `{OtherValue}`               |
| `NotEqualTo`                        | {PropertyName} must not be {OtherValue}.                                           | `{PropertyName}`, `{OtherValue}`               |
| `GreaterThanOtherProperty`          | {PropertyName} must be greater than {OtherPropertyName}.                           | `{PropertyName}`, `{OtherPropertyName}`        |
| `GreaterThanOrEqualToOtherProperty` | {PropertyName} must be greater than or equal to {OtherPropertyName}.               | `{PropertyName}`, `{OtherPropertyName}`        |
| `LessThanOtherProperty`             | {PropertyName} must be less than {OtherPropertyName}.                              | `{PropertyName}`, `{OtherPropertyName}`        |
| `LessThanOrEqualToOtherProperty`    | {PropertyName} must be less than or equal to {OtherPropertyName}.                  | `{PropertyName}`, `{OtherPropertyName}`        |
| `EqualToOtherProperty`              | {PropertyName} must match {OtherPropertyName}.                                     | `{PropertyName}`, `{OtherPropertyName}`        |
| `NotEqualToOtherProperty`           | {PropertyName} must not match {OtherPropertyName}.                                 | `{PropertyName}`, `{OtherPropertyName}`        |
| `MultipleOf`                        | {PropertyName} must be a multiple of {Step}.                                       | `{PropertyName}`, `{Step}`                     |
| `InThePast`                         | {PropertyName} must be a date in the past.                                         | `{PropertyName}`                               |
| `InTheFuture`                       | {PropertyName} must be a date in the future.                                       | `{PropertyName}`                               |
| `IsInEnum`                          | {PropertyName} has an invalid value.                                               | `{PropertyName}`                               |
| `MinimumCount`                      | {PropertyName} must contain at least {MinCount} entries.                           | `{PropertyName}`, `{MinCount}`                 |
| `MaximumCount`                      | {PropertyName} must not contain more than {MaxCount} entries.                      | `{PropertyName}`, `{MaxCount}`                 |
| `NoDuplicates`                      | {PropertyName} must not contain duplicate entries.                                 | `{PropertyName}`                               |

Each of these is a constant on `ValidationErrorCodes`, so a provider keys off `ValidationErrorCodes.NotEmpty`
rather than off the string.

Two placeholders are supplied to a message without any built-in text using them, and are there for a
provider of your own: **`{Pattern}`**, the regular expression a `Matches` rule was declared with, and **
`{CollectionIndex}`**, the zero-based position of the entry under judgement inside any `ForEach`. A
message that names `{CollectionIndex}` gets it whichever way the element's rules were declared, including
from a validator composed in with `ForEach(validator)`.

## Custom validators

Most of what an application validates is not a length or a range but a rule of its own. There are three
ways to write one, and which to reach for depends on how often you will write it:

|                                                | For                                                       |
|------------------------------------------------|-----------------------------------------------------------|
| `Must`                                         | A rule this one property has, written where it is used    |
| An extension method over `PropertyRuleBuilder` | A rule several properties or several validators share     |
| `Add` / `AddAsync`                             | The body of either, when it has more to say than a `bool` |

### A one-off rule: `Must`

`Must` takes a predicate. The value alone, or the whole object and the value where the rule is about both:

```csharp
this.Property(c => c.Vin)
    .Must(vin => vin is null || vin.Length == 17);

this.Property(c => c.ServiceHistory)
    .Must((car, history) => history is null || history.All(r => r.Mileage <= car.Mileage));
```

**The predicate is handed the value as it is, including null** — there is no implicit guard, because a
rule that silently passed everything absent would be indistinguishable from one that was never reached.
`vin is null ||` is the idiom, and it is what makes the rule agree with every shipped one: presence is a
separate rule.

Without a message it reports the code `Must`, whose built-in English is *"{PropertyName} is not valid."*
That is rarely what you want to show, so name it:

```csharp
this.Property(c => c.RegistrationPlate)
    .Must(plate => plate is null || plate.StartsWith("CH"))
    .WithErrorCode("SwissPlate");   // the provider resolves "SwissPlate"
```

Naming a rule with `WithErrorCode` is what makes it localize like a shipped one, because the code is also
the key the host's message provider is asked for. A client can branch on `SwissPlate` too, which it could
not do on a sentence.

Where the wording is genuinely a one-off, put it on `WithMessage`. It is a template like any shipped
message, so it may name the placeholders the rule supplies and is used as written where it names none:

```csharp
this.Property(c => c.Vin)
    .Must(vin => vin is null || vin.Length == 17)
    .WithMessage("{PropertyName} must be exactly 17 characters long.");
```

A `Func<string>` overload is re-evaluated at validation time rather than at declaration time, which is
what a message read off a resource needs.

Where the verdict depends on something only the caller knows, `Must<TData>` hands the predicate the
[data the validation carries](#data-a-validation-carries) as well, and passes where the call handed none:

```csharp
this.Property(c => c.Mileage)
    .Must<ListingPolicy>((car, mileage, policy) => mileage <= policy.MaximumMileage)
    .WithErrorCode("ListingMileage");
```

### A reusable rule: an extension method

A rule two validators share is an extension method on `PropertyRuleBuilder<T, TProperty>`, so it chains
exactly like a shipped one:

```csharp
public static class VinRules
{
    public static PropertyRuleBuilder<T, string?> Vin<T>(this PropertyRuleBuilder<T, string?> builder)
    {
        return builder.Add(context =>
        {
            if (context.Value is { Length: not 17 })
            {
                context.AddError("Vin", ("Length", 17));
            }
        });
    }
}
```

```csharp
this.Property(c => c.Vin).NotEmpty().Vin();
```

`Add` takes the rule's body and returns the chain, so `WithMessage`, `WithErrorCode` and the rest apply to
it as they do to any rule.

Note what the body reports: a **code and its arguments**, not a sentence. `"Vin"` is resolved through the
message provider exactly as `NotEmpty` is, so the rule is localizable the day someone needs it, and
`("Length", 17)` is available to the template as `{Length}`. Give the built-in English a home by
[decorating the default provider](#localization); until you do, the code renders as itself, which is
legible enough to ship a first version on.

The literal form is still there for a rule whose wording will never be translated:

```csharp
context.AddError(new ValidationError(context.PropertyName, "The VIN must be exactly 17 characters long."));
```

### A rule that has to await something

`MustAsync` is the short form, handed the value and the `CancellationToken` of the run:

```csharp
this.Property(c => c.Vin)
    .NotEmpty()
    .MustAsync((vin, cancellationToken) => vinRegistry.IsFreeAsync(vin, cancellationToken))
    .WithErrorCode("VinAlreadyRegistered");
```

It reports `Must` unless the chain names it, exactly as the synchronous `Must` does — and naming it is
what makes it localizable, because the code is also the key the provider resolves. Only the
`ValueTask<bool>` shape ships: an overload taking a `Task<bool>` would make every `async` lambda
ambiguous between the two.

`AddAsync` is the seam underneath it, for a rule that reports something other than a single failure. It
is handed the same context plus the `CancellationToken` of the run:

```csharp
public sealed class CarValidator : Validator<Car>
{
    public CarValidator(IVinRegistry vinRegistry)
    {
        this.Property(c => c.Vin)
            .NotEmpty()
            .AddAsync(async (context, cancellationToken) =>
            {
                if (context.Value is not { } vin)
                {
                    return;
                }

                if (await vinRegistry.ExistsAsync(vin, cancellationToken))
                {
                    context.AddError("VinAlreadyRegistered", ("Vin", vin));
                }
            });
    }
}
```

This is the rule that makes the whole library asynchronous, and the reason there is no synchronous entry
point — see [validation is asynchronous](#validation-is-asynchronous-and-only-asynchronous).

Two things worth knowing about a rule like this. It runs only if the chain got that far, so putting it
after `NotEmpty()` is what keeps it from asking the database about an empty string. And it runs on every
validation, so a rule that talks to a database belongs on a validator whose lifetime matches the thing it
talks to — see [lifetimes](#lifetimes).

### A rule that needs a service

A rule reads whatever its validator was constructed with. There is nothing on the context to resolve
services from: the validator is the injection point, which is what makes its dependencies visible in its
signature and its rules testable with a substitute.

```csharp
public sealed class ManufacturerValidator : Validator<Manufacturer>
{
    public ManufacturerValidator(TimeProvider timeProvider, ICountryCodes countryCodes)
    {
        this.Property(m => m.FoundedDate)
            .InThePast(timeProvider);

        this.Property(m => m.CountryCode)
            .NotEmpty()
            .Must(code => code is null || countryCodes.IsKnown(code))
            .WithErrorCode("UnknownCountryCode");
    }
}
```

Registration resolves those dependencies for you — `AddValidator<ManufacturerValidator>()` and the scan
both construct it through the container.

### `RuleContext`

What the body of a custom rule is handed:

| Member                                                       | What it is                                                                       |
|--------------------------------------------------------------|----------------------------------------------------------------------------------|
| `Value`                                                      | The value of the property this chain was declared for                            |
| `Instance`                                                   | The whole object being validated, for a rule about more than one property        |
| `PropertyName`                                               | What a failure of this property is reported under                                |
| `DisplayName`                                                | What a message calls it — the `WithDisplayName` name, or the property name       |
| `HasFailed`                                                  | `true` once a rule in this chain has failed                                      |
| `ValidationMessageProvider`                                  | The message provider, for a rule building a `ValidationError` itself             |
| `TryGetData<TData>(out data)`                                | The [data the validation carries](#data-a-validation-carries), found by its type |
| `CancellationToken`                                          | The token the validation was started with                                        |
| `AddError(errorCode, params (string Name, object? Value)[])` | Reports a failure, resolving the message from the code and the arguments         |
| `AddError(ValidationError)`                                  | Reports a failure the rule composed itself — used by rules reporting per element |
| `GetDisplayName(propertyName)`                               | What a message should call *another* property, for a rule comparing two          |

`AddError(errorCode, ...)` seeds `{PropertyName}` from the property's display name before your arguments,
so a template never has to be told the name of the thing it is about. Your arguments may override it if
they genuinely need to.

## Overriding defaults

Four things about a failure can be named, and they are four different things. It is worth reading them
side by side once:

```csharp
this.Property(c => c.Model.Manufacturer.Name)
    .WithDisplayName("Manufacturer")        // what the message calls it
    .WithPropertyName("manufacturerName")   // what the client binds to
    .NotEmpty().WithErrorCode("REQUIRED");  // what the client branches on

// reports: { "manufacturerName": ["REQUIRED"] }, with ErrorCode "REQUIRED"
```

The message reads `REQUIRED` because the code is also the key the message is resolved under, and the
built-in provider has no text for `REQUIRED`. Give your own provider one and the wording is yours. A raw
code showing up in a response is the reminder that a key still needs a text.

|                          | Applies to                                              | Changes                                                                |
|--------------------------|---------------------------------------------------------|------------------------------------------------------------------------|
| `WithDisplayName(name)`  | the whole property, wherever in the chain it is written | `{PropertyName}` in every message of that property                     |
| `WithPropertyName(name)` | the whole property                                      | `ValidationError.PropertyName` — what the failure is reported under    |
| `WithErrorCode(code)`    | the one rule it follows                                 | `ValidationError.ErrorCode`, and the key the message is resolved under |
| `WithMessage(message)`   | the one rule it follows                                 | the text, substituted against the rule's arguments                     |

Because `WithErrorCode` and `WithMessage` bind to the rule before them, each rule of a chain can carry its
own:

```csharp
this.Property(c => c.Vin)
    .NotEmpty().WithMessage("A VIN is required to list a car.")
    .Length(17).WithMessage("A VIN is {Length} characters long.");
```

`WithDisplayName` and `WithMessage` also come in `Func<string>` forms, re-evaluated at validation time so
a value read off a resource follows the request's culture rather than the process's startup culture.
`WithMessage` additionally has `Func<T, string>` and `Func<T, TProperty, string>` overloads, for a wording
that depends on what was actually sent.

### The override ladder

Settings that exist at more than one level are resolved from the most specific one that names them:

| Default                        | Out of the box               | Overridden at                                                                                                                                                                                                                                         |
|--------------------------------|------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Message text                   | the built-in English         | `NValidationOptions.Default` → `AddNValidation` → what the pass inherits: the options passed to the call, or the settings of the validator this one is composed into → the validator's `ValidationMessageProvider` → `WithMessage` for one rule       |
| Display name                   | the property's member path   | `WithDisplayName`, per property                                                                                                                                                                                                                       |
| Reported property name         | the property's member path   | `WithPropertyName`, per property                                                                                                                                                                                                                      |
| `ValidationBehaviors.Class`    | `All`                        | the same ladder, ending at the validator and then `WithValidationBehavior` on a chain                                                                                                                                                                 |
| `ValidationBehaviors.Property` | `StopAtFirstError`           | the same ladder                                                                                                                                                                                                                                       |
| Validator lifetime             | `ServiceLifetime.Scoped`     | `o.ValidatorLifetime`, or per registration                                                                                                                                                                                                            |
| Selected rule groups           | the default group            | `NValidationOptions.Default` → the options passed to the call, resolved once where the call starts and handed to every validator it reaches. There is no validator rung — a selection belongs to the call, not to the validator                         |
| Data the rules read            | none                         | the same as the rule groups                                                                                                                                                                                                                           |
| Properties validated           | every one                    | the same as the rule groups                                                                                                                                                                                                                           |
| Element index                  | the zero-based position      | `WithIndexer`, per `ForEach`                                                                                                                                                                                                                          |
| Missing-validator behavior     | `Ignore`                     | `AddValidationFilter`                                                                                                                                                                                                                                 |

Every setting of `NValidationOptions` travels that ladder, and each rung is asked only about what it
named: options that set a behavior and no provider change the behavior and leave the messages to the
level below.

What a validator resolves for itself is what the validators it composes inherit: a nested validator, and
the element chain of a `ForEach`, answer like their composer unless they declared otherwise. That is why
one setting on the outermost validator — or on the options of a call, or on
[`NValidationOptions.Default`](#validation-options) — reaches the whole graph, and why a validator which
said something about itself keeps it wherever it is composed.

### Validation options

`NValidationOptions` carries the settings a validator can be given without a container: where its rules
take their message texts from, how much it reports, which [rule groups](#rule-groups) it runs, which
[data](#data-a-validation-carries) its rules may read, and which [properties](#validating-some-properties)
it is limited to. It is immutable — a variant is derived with `with` — and it serves two roles.

**For the process**, build one and assign it to `NValidationOptions.Default`, once at startup:

```csharp
NValidationOptions.Default = new NValidationOptions
{
    MessageProvider = new ResourceValidationMessageProvider(),
    ValidationBehaviors = new() { Property = ValidationBehavior.All },
};
```

**For one call**, pass an instance instead — a request answered in its own language, or one that runs
the rules of a particular operation:

```csharp
var options = NValidationOptions.Default with { MessageProvider = german };

var result = await validator.ValidateAsync(car, options);
```

```csharp
// A group selection is written as a name or a list of them, on top of the default group;
// ValidationGroups.Only(...) leaves the default group out, and ValidationGroups.All runs every group.
var options = new NValidationOptions { ValidationGroups = ["Create", "Import"] };

// What the rules need to know about the request, and what it is limited to.
var options = new NValidationOptions
{
    ValidationGroups = ValidationGroups.Only("Listing"),
    ValidationData = [new ListingPolicy(MaximumMileage: 200_000, RequiresServiceHistory: true)],
    ValidationProperties = ["Mileage", "ServiceHistory"],
};
```

All of them reach a nested validator and the element chain of a `ForEach`, and all of them sit below what a
validator declared for itself — see [the override ladder](#the-override-ladder). Options built once and
kept — in a static field, or captured by an endpoint — cost a call nothing to hand over.

**Options name only what they name.** Every setting starts at `null`, meaning *leave it to the level
below*, so the two above are the same rule applied twice:

```csharp
var options = new NValidationOptions { ValidationBehaviors = new() { Property = ValidationBehavior.All } };

// Reports every rule of a property — and still answers in whatever language was already configured.
var result = await validator.ValidateAsync(car, options);
```

Assigning `NValidationOptions.Default` replaces a *reference*, so it is safe at any time: a run which
already read the old options keeps them, and later runs read the new ones. A test does exactly this —
install an instance, put the original back afterwards.

> **For an application to set, not a library.** A package which configures `NValidationOptions.Default`
> from a module initializer silently changes the wording every one of its consumers sees. A library which
> needs its own settings passes them per call.

### Display names are member paths, not prose

The default display name of a property is its member path, verbatim — `CountryCode` displays as
`CountryCode`, and `Model.Manufacturer.Name` as `Model.Manufacturer.Name`. There is no PascalCase
splitting and no `[Display]` or `[DisplayName]` support — a library that guessed at prose would be wrong
in every language but one. A name meant for a person is one you write:

```csharp
this.Property(c => c.FirstRegistration).WithDisplayName("Registration date");
```

That name is what `{PropertyName}` renders to, including in the message of *another* property that
compares against this one through `{OtherPropertyName}`. Which is why declaring it once, on the property
it belongs to, is enough:

```csharp
// reports: SoldDate -> "SoldDate must be greater than or equal to Registration date."
this.Property(c => c.SoldDate).GreaterThanOrEqualTo(c => c.FirstRegistration);
```

Attributes are absent on purpose: a display name is a presentation decision, and a model shared with a
client or a database has no business carrying one.

### Placeholders

A message is a template with named placeholders. Fifteen names exist, each a constant on
`ValidationMessagePlaceholders`, and a message uses only the ones it needs:

| Placeholder                                | Carries                                           | Supplied by                         |
|--------------------------------------------|---------------------------------------------------|-------------------------------------|
| `{PropertyName}`                           | the property's display name                       | every rule                          |
| `{CollectionIndex}`                        | the entry's zero-based position                   | every rule inside a `ForEach`       |
| `{MinLength}` / `{MaxLength}` / `{Length}` | a character count                                 | the text length rules               |
| `{Pattern}`                                | the regular expression                            | `Matches`                           |
| `{From}` / `{To}`                          | the bounds                                        | `Between`                           |
| `{Step}`                                   | the step                                          | `MultipleOf`                        |
| `{MinCount}` / `{MaxCount}`                | an entry count                                    | the collection count rules          |
| `{TopLevelDomains}` / `{TopLevelDomain}`   | the allowed list / the offending one              | the email domain rules              |
| `{OtherValue}`                             | the value compared against                        | the comparison rules                |
| `{OtherPropertyName}`                      | the display name of the property compared against | the cross-property comparison rules |

A placeholder takes a .NET format specifier after a colon — `{Step:0.00}`, `{To:yyyy-MM-dd}` — and values
are rendered in `CultureInfo.CurrentCulture`, so a number or a date follows the culture of the request
rather than the culture of the host.

A translation is free to leave any of them out, including the property name — which is what you want for a
message shown underneath an already labelled input.

The formatter is deliberately forgiving, because a bad template must not turn a bad request into a server
error:

- A placeholder the rule did not supply is **left in the text as written**. `{Mileage}` in a `NotEmpty`
  message renders as `{Mileage}`, which is visible in a test and harmless in production.
- A format specifier the value cannot honour — `{PropertyName:0.00}` — falls back to rendering the value
  plainly rather than throwing.

### Where an override is refused

`WithMessage` and `WithErrorCode` bind to the rule they follow, so they need one, and it has to be a rule
that produced the failure itself. Following `SetValidator` or `ForEach` they throw where they are
declared:

```csharp
this.Property(c => c.Model)
    .SetValidator(carModelValidator)
    .WithMessage("The model is wrong.");     // InvalidOperationException
```

What a composed validator reported is its own decision — a message replacing every failure a nested
validator found would be the loss of everything it said. Say it on the nested validator's own rules
instead.

## Localization

Rules report an *error code*, never a text. The code is resolved through `IValidationMessageProvider`, so
the application decides where the wording comes from and in which language:

```csharp
public interface IValidationMessageProvider
{
    string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments);
}
```

That is the whole seam. A failure travels as *code + arguments* from the rule that found it to the
provider that words it, and nothing in between has an opinion about language.

### A provider over resources

The usual shape: a map from code to a resource lookup, delegating anything it does not own to the built-in
English so a new rule is never a blank message.

```csharp
public sealed class ResourceValidationMessageProvider : IValidationMessageProvider
{
    private static readonly Dictionary<string, Func<string>> Messages = new(StringComparer.Ordinal)
    {
        [ValidationErrorCodes.NotEmpty] = () => Strings.ValidationMessage_Required,
        [ValidationErrorCodes.MaximumLength] = () => Strings.ValidationMessage_MaxLength,
    };

    public string GetMessage(string errorCode, IReadOnlyDictionary<string, object?> arguments)
    {
        return Messages.TryGetValue(errorCode, out var message)
            ? ValidationMessageFormatter.Format(message(), arguments)
            : DefaultValidationMessageProvider.Instance.GetMessage(errorCode, arguments);
    }
}
```

Two details make this work under a server:

- **The resource is read inside the `Func<string>`, not into the dictionary.** The map is built once for
  the process; the text has to be fetched while the message is produced, because that is the only moment
  the request's culture is current. A provider that caches strings in its constructor serves every request
  in whichever language the first one happened to use.
- **`ValidationMessageFormatter.Format` does the substitution.** Your resource is a template like the
  built-in ones — `{PropertyName} must not exceed {MaxLength} characters.` — and the arguments passed in
  are exactly what the rule supplied. See [placeholders](#placeholders) for what is available.

Your own codes go in the same map. A rule named with `WithErrorCode("SwissPlate")` is looked up under
`"SwissPlate"` like any other, which is what makes a custom rule localize like a shipped one.

### Registering it

```csharp
services.AddNValidation(o => o.MessageProvider = typeof(ResourceValidationMessageProvider));
```

The provider is built by the container and registered as a **singleton**: it is a lookup asked for text,
it has to be thread-safe anyway because validators run concurrently, and being longer-lived than every
validator is what lets validators of any lifetime be handed it. Resolve the language while the message is
produced — a `Func<string>` over a resource — rather than in the constructor. A provider that genuinely
cannot be shared is registered on the service collection directly — see the paragraph below — at the cost
of forcing every validator that uses it to be scoped too.

Registering `IValidationMessageProvider` on the service collection yourself works as well: the
registration hands whatever provider the container holds to the validators it builds, and registers
none on your behalf.

A validator constructed with `new` was never handed anything, so it answers through
[`NValidationOptions.Default`](#validation-options) — which an application sets once, in the place it
would otherwise have configured every instance:

```csharp
NValidationOptions.Default = new NValidationOptions
{
    MessageProvider = new ResourceValidationMessageProvider()
};
```

One validator can still be settled on its own, which outranks both that and the registration:

```csharp
var validator = new ManufacturerValidator
{
    ValidationMessageProvider = new ResourceValidationMessageProvider()
};
```

Without a provider anywhere the built-in English messages are used, so the library is usable before any
of this is set up.

### Holding a provider to every code

An application which resolves messages itself owes every code a text. One assertion covers the lot, so a
missing translation shows up in the suite rather than as a raw code in a response:

```csharp
[Fact]
public void GetMessage_AnswersForEveryCodeOfTheCore()
{
    new ResourceValidationMessageProvider().ShouldResolveEveryCoreErrorCode();
}
```

It checks that each code of `ValidationMessageProviderAssertions.CoreErrorCodes()` resolves to something
other than the code itself, and that no `{Placeholder}` is left unsubstituted. It does not require a
message to name the failing property — that is the translation's call. Because the provider above falls
back to the built-in English, this passes from the first day and starts being informative the day someone
removes the fallback.

To check one code — a code of your own, which the core knows nothing about — there is
`provider.ShouldResolveErrorCode("SwissPlate")`.

## Dependency injection

Everything in this section lives in the
[`NValidation.DependencyInjection`](https://www.nuget.org/packages/NValidation.DependencyInjection/)
package. The core package has no container abstractions in it at all, so an application which only
constructs validators never acquires them.

### AddNValidation

Everything the container needs to know is configured in one delegate, on an `NValidationBuilder`:

```csharp
services.AddNValidation(o =>
{
    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
});
```

There is an overload taking no delegate, for an application that registers its validators itself and only
wants the message provider in place:

```csharp
services.AddNValidation();
```

Nothing reaches the service collection until the delegate has finished, so the order of the calls inside
it never decides anything.

### From appsettings.json

The settings which are a deployment decision rather than a code one take an `IConfiguration` section:

```csharp
services.AddNValidation(builder.Configuration.GetSection("NValidation"), o =>
{
    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
});
```

```json
{
  "NValidation": {
    "ValidatorLifetime": "Singleton",
    "PromoteSafeValidatorsToSingleton": true,
    "ValidationBehaviors": {
      "Class": "All",
      "Property": "StopAtFirstError"
    }
  }
}
```

The delegate runs after the section, so what it names outranks what configuration said. A key which is
absent leaves that setting alone; a key whose value is not one of the permitted ones is refused at
startup, and the message names what was permitted — a typo in a settings file should not be something you
discover from a response.

**Which validators are registered, and where messages come from, stay in code.** Naming an assembly or a
provider type in a settings file turns a typo into a payload that is silently never validated, which is
the one failure this library should not have. A message provider an application already holds goes on
[`NValidationOptions.Default`](#validation-options) instead.

### Registering validators

```csharp
services.AddNValidation(o => o
    .AddValidator<CarModelValidator>()
    .AddValidator<CarValidator>());
```

| Overload                                       | For                                                                                                                   |
|------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------|
| `AddValidator<TValidator>()`                   | The common case — the validated type is read off the validator                                                        |
| `AddValidator<TInstance, TValidator>()`        | Naming both, where you would rather the compiler checked that a validator really does validate what you think it does |
| `AddValidator(Type validatorType)`             | A type decided at run time                                                                                            |
| `AddValidatorsFromAssembly(params Assembly[])` | Every `IValidator<T>` in an assembly, each with its own dependencies resolved                                         |

Each of them also has an overload taking a `ServiceLifetime`, for the one registration that cannot follow
the default.

Registration is order-independent: a validator named explicitly with `AddValidator` wins over whatever a
scan finds for the same type, whether it is named before the scan or after it. That is also how a payload
with two validators in one assembly is settled: name the one you want, and the scan passes over it instead
of refusing to choose. Two explicit registrations naming different validators for one payload is a
contradiction, and is refused.

### Lifetimes

`ValidatorLifetime` is **scoped**, because a validator may depend on something that is itself scoped —
the database an async uniqueness rule asks — and a longer-lived validator would capture it. It is the
fallback rather than what most validators actually get: a validator that provably depends on nothing
scoped is promoted to a singleton, which is [the default](#letting-the-registration-decide).

A validator declares its rules in its constructor and never changes afterwards, so where nothing scoped is
involved, registering them as singletons builds those rules once for the process instead of once per
request:

```csharp
services.AddNValidation(o =>
{
    o.ValidatorLifetime = ServiceLifetime.Singleton;

    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly)

     // The one that cannot follow the default overrides it, rather than dragging the rest down.
     .AddValidator<VinUniquenessValidator>(ServiceLifetime.Scoped);
});
```

Nothing reaches the service collection until the delegate has run, so `ValidatorLifetime` governs every
validator wherever in the delegate you set it.

#### Letting the registration decide

Choosing globally means choosing for the validator that cannot follow. `PromoteSafeValidatorsToSingleton`
decides per validator instead, at registration — and it is **on by default**:

```csharp
services.AddNValidation(o =>
{
    // Already the default; set it to false to register every validator at ValidatorLifetime instead.
    o.PromoteSafeValidatorsToSingleton = true;
    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
});
```

A validator is promoted only when every constructor parameter resolves to something already registered as
a singleton, or to another validator that itself qualifies. One that takes a scoped `DbContext` is left
scoped. The decision is made once, from the service collection, and never depends on what a request does.

A lifetime you name yourself is an instruction and outranks the default, so
`AddValidator<CarValidator>(ServiceLifetime.Transient)` — or a `ValidatorLifetime` you set — is registered
exactly as written. Setting `PromoteSafeValidatorsToSingleton = true` yourself asks for promotion
regardless, which lifts a named lifetime too.

It is on by default because building a validator graph costs far more than using it: a scoped
registration spends more of a request on rebuilding rules that never change than on applying them. The
figures are in the benchmark project, `Tests/NValidation.Benchmark`.

### Injecting services into a validator

A validator is constructed by the container, so it takes what it needs in its constructor and its rules
close over it:

```csharp
public sealed class CarValidator : Validator<Car>
{
    public CarValidator(IValidator<CarModel> carModelValidator, IVinRegistry vinRegistry, TimeProvider timeProvider)
    {
        this.Property(c => c.Model).NotNull().SetValidator(carModelValidator);

        this.Property(c => c.FirstRegistration).InThePast(timeProvider);

        this.Property(c => c.Vin)
            .NotEmpty()
            .AddAsync(async (context, ct) =>
            {
                if (context.Value is { } vin && await vinRegistry.ExistsAsync(vin, ct))
                {
                    context.AddError("VinAlreadyRegistered", ("Vin", vin));
                }
            });
    }
}
```

A nested validator is injected the same way, as `IValidator<CarModel>` — the interface, so the composition
is a dependency like any other and a test can substitute it.

### What the container configures

A validator the container built is **handed** two things a validator you constructed yourself is not:

- its message provider — the `IValidationMessageProvider` the container holds, if any;
- the [validation behavior](#validation-behavior) configured on `AddNValidation`.

What it hands over is a level *below* what a validator declared for itself and below the options passed
to a call: a validator whose constructor set its own `ValidationMessageProvider` or named an axis keeps
what it said, and a per-call wording still reaches a validator resolved from the container.

The container can only hand something to a validator it constructed. What that validator resolves is
then inherited by everything it composes — a nested validator built with `new`, the element chain of a
`ForEach` — so the registration's settings reach the whole graph through its root. A validator nobody
resolved from the container answers through [`NValidationOptions.Default`](#validation-options), which is
the setting to use where you want something to apply everywhere; `AddNValidation` is for what only the
container can decide — which validators exist, and how long they live.

`o.Services` is the service collection itself, which is how `AddValidationFilter` in the ASP.NET Core
package registers what it needs alongside. It is `internal`, so it is there for the integration packages
in this repository rather than for application code.

The only per-call options the filter passes are the rule groups an endpoint asked for, so a request
answered in its own language is not served by passing an `NValidationOptions` per request. Register one thread-safe
provider that resolves
the wording *while the message is produced* — from `CultureInfo.CurrentUICulture`, which
`UseRequestLocalization` already sets per request — and every validator answers in the request's
language without anything being passed at all. See [localization](#localization).

## ASP.NET Core integration

`NValidation.AspNetCore` turns a validation failure into the response an HTTP client expects, and can run
the validators for you.

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

The body is a `HttpValidationProblemDetails` — the framework's own type, so it serializes under a
source-generated serializer and an ahead-of-time published host. The library sets the status and the
errors, and clears the title. `type`, `detail` and the trace identifier are left to the host's problem
details pipeline, so a validation failure looks exactly like every other problem response the application
produces, including whatever a `CustomizeProblemDetails` callback adds. Property names are written
verbatim, dots and indexes included, because they are what the client binds to.

An application which already has its own exception-to-problem-details handler should read
`ValidationException.Errors` there rather than registering `ValidationExceptionHandler`, so every error
response keeps going through one place.

### Returning a failure instead of throwing

```csharp
var result = await this.carValidator.ValidateAsync(car, cancellationToken);

if (!result.Succeeded)
{
    return this.ValidationProblem(result);
}
```

`ValidationProblem(result)` is an extension on `ControllerBase`. Outside a controller — a minimal API
handler, a middleware — `result.ToProblemDetails()` gives you the same body to do what you like with.
Both refuse a *successful* result: a 400 that names no failure is a bug worth hearing about at the point
it is written.

### Validating a controller's payload automatically

`ValidationActionFilter` validates an action's payload before the action runs. For every parameter bound
from the request body or form, it resolves the validator registered for that parameter's declared type and
runs it; a type with no registered validator is left alone.

```csharp
services.AddNValidation(o =>
{
    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
    o.AddValidationFilter();
});
```

`AddValidationFilter` lives in `NValidation.AspNetCore` and is opt-in: this is an MVC filter, and an
application built on minimal APIs validates by calling its validator in the handler. Registering the
filter directly — `services.AddControllers(o => o.Filters.Add<ValidationActionFilter>())` — does the same
thing; they are alternatives rather than steps, and registering it both ways is de-duplicated rather than
running the validators twice.

The action is then free of validation code:

```csharp
[HttpPost("")]
public ActionResult<string> Create(Car car)
{
    // car is valid: an invalid one never got here.
}
```

Failures of every payload of the request are collected into one `ValidationException`, so the response
reports all of them at once. Route and query values are not payloads and are never validated, and a
parameter that bound to null is skipped — an absent body is a binding concern, not a validation one.

An endpoint which reports failures in its own shape opts out, and validates itself:

```csharp
public async Task<IActionResult> ValuateAsync(
    [SkipNValidation("Answers in a legacy error shape which deployed clients parse.")] CarValuation carValuation)
```

The reason is optional — plain `[SkipNValidation]` excludes just as well — but it is what tells the next
reader that the gap was a decision. The attribute goes on a parameter, an action or a whole controller.

`MissingValidatorBehavior` decides what happens to a payload which has neither a validator nor
`[SkipNValidation]` — `Ignore` (the default), `Log`, or `Throw` to make the gap impossible to miss on a
development host:

```csharp
services.AddNValidation(o =>
{
    o.AddValidatorsFromAssembly(typeof(CarValidator).Assembly);
    o.AddValidationFilter(f => f.MissingValidatorBehavior = MissingValidatorBehavior.Throw);
});
```

Pass an `IConfiguration` section instead of a delegate to bind the behaviour from configuration, which is
what lets a development host say `Throw` and production say `Log` without a rebuild:

```csharp
o.AddValidationFilter(builder.Configuration.GetSection("Validation"));
```

```json
{
  "Validation": {
    "MissingValidatorBehavior": "Log"
  }
}
```

`Log` warns once per action parameter rather than once per request, so an unvalidated endpoint is visible
in the log without flooding it.

### Selecting rule groups for an endpoint

A [rule group](#rule-groups) is named where the endpoint is declared, so the action still holds no
validation code:

```csharp
[HttpPost("")]
[ValidationGroups("Create")]
public ActionResult<string> Create(Car car)

[HttpPost("listing-check")]
[ValidationGroups("Listing", Only = true)]
public IActionResult CheckListing(Car car)        // the Listing group, and nothing else

[HttpPut("{carId:int}")]
public IActionResult Update(int carId, Car car)   // the default group, and nothing else
```

The nearest one decides: the parameter's own attribute, then the action's, then the controller's. That
is what lets a controller name the group its endpoints share and one action say otherwise. Name several
groups — `[ValidationGroups("Create", "Import")]` — leave the default group out with `Only = true`, or ask
for every one with `[ValidationGroups(All = true)]`. An endpoint with no attribute validates as a plain call
does. `Only` on a payload whose validator declares none of the groups fails the request rather than
accepting it unchecked, so a controller-level `Only` has to suit every payload below it.

The filter hands no data over: an endpoint whose rules need some calls its validator itself, as a
[minimal API](#minimal-apis) handler does.

Which decision applies is worked out once per action parameter, not once per request.

### Minimal APIs

There is no filter for minimal APIs, and no endpoint filter to add: a handler takes the validator it needs
and calls it. Both shapes are one line.

```csharp
// The throwing path: validate, and let the exception handler turn a failure into the response.
app.MapPost("/cars", async (Car car, IValidator<Car> validator, CancellationToken cancellationToken) =>
{
    await validator.ValidateAndThrowAsync(car, cancellationToken);

    return Results.Ok(new { car.Vin });
});

// A handler which runs a rule group passes the selection, which is what [ValidationGroups] does for an
// action. Both ValidateAsync and ValidateAndThrowAsync take options.
app.MapPost("/cars/intake", async (Car car, IValidator<Car> validator, CancellationToken cancellationToken) =>
{
    var options = new NValidationOptions { ValidationGroups = "Create" };

    await validator.ValidateAndThrowAsync(car, options, cancellationToken);

    return Results.Ok(new { car.Vin });
});

// Options built once are shared by every request: here, the Listing group alone, and the policy of the
// market the rules read.
var listingCheck = new NValidationOptions
{
    ValidationGroups = ValidationGroups.Only("Listing"),
    ValidationData = [new ListingPolicy(MaximumMileage: 200_000, RequiresServiceHistory: true)],
};

app.MapPost("/cars/listing-check", async (Car car, IValidator<Car> validator, CancellationToken cancellationToken) =>
{
    await validator.ValidateAndThrowAsync(car, listingCheck, cancellationToken);

    return Results.NoContent();
});

// The returning path, for an endpoint that would rather decide for itself what a failure means.
app.MapPost("/cars/checked", async (Car car, IValidator<Car> validator, CancellationToken cancellationToken) =>
{
    var result = await validator.ValidateAsync(car, cancellationToken);

    return result.Succeeded
        ? Results.Ok(new { car.Vin })
        : Results.Problem(result.ToProblemDetails());
});
```

### What the package contains

| Type                         | What it is for                                                                                                |
|------------------------------|---------------------------------------------------------------------------------------------------------------|
| `ValidationExceptionHandler` | An `IExceptionHandler` turning a `ValidationException` into a 400 problem details response                    |
| `ValidationActionFilter`     | An MVC filter validating every body- and form-bound parameter before the action runs                          |
| `AddValidationFilter(...)`   | Registers that filter from inside `AddNValidation` — with a delegate, an `IConfiguration` section, or neither |
| `ValidationFilterOptions`    | What the filter is configured with: `MissingValidatorBehavior`                                                |
| `MissingValidatorBehavior`   | `Ignore`, `Log` or `Throw` for a payload with no validator                                                    |
| `SkipNValidationAttribute`   | `[SkipNValidation]` on a parameter, an action or a controller                                                 |
| `ValidationGroupsAttribute`  | `[ValidationGroups("Create")]` on a parameter, an action or a controller: which rule groups the filter runs, with `Only` or `All` |
| `ToProblemDetails()`         | The problem details body for a `ValidationResult` or a `ValidationException`                                  |
| `ValidationProblem(result)`  | The `ControllerBase` shortcut for returning one                                                               |

A runnable end-to-end example lives in
[
`Samples/NValidation.SampleApi`](https://github.com/thomasgalliker/NValidation/tree/develop/Samples/NValidation.SampleApi).

## Testing your own rules

A validator is a plain object, so a test constructs it and runs it against whatever data the case is
about. `NValidation.Testing` ships in the same package and needs no test framework and no assertion
library of its own:

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

`ShouldReport` states the **whole** expected result: nothing else may be present and the count is implied,
so a repeated entry asks for a repeated failure. Order is ignored. A failure throws
`ValidationAssertionException`, naming what was missing, what was unexpected, and the near miss in
between:

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

Assert the message, not only the property name. A test which checks the property name alone passes when a
rule reports the right property with the wrong message — a value that is too large reported as "must be
greater than".

The message is matched **exactly**, the way `Be` does in an assertion library, so an expected message
means what it says. The two looser forms are named, so a weaker assertion is visible at the call site:

```csharp
result.ShouldReport("Vin", "Vin is required.");                         // exact
result.ShouldReport([
    ExpectedError.Matching("Mileage", "*greater than*"),                // a fragment
    ExpectedError.Any("FeatureIds"),                                    // any message
    new("ServiceHistory[0].Workshop", "Workshop is required.")]);
```

`Matching` takes `*` for any run of characters and `?` for exactly one, with `\*` and `\?` for those
characters themselves. Use it and `Any` sparingly: a pinned message is what catches a rule wired to the
wrong error code.

The same overloads take a `ValidationException` or a `{ propertyName: [messages] }` dictionary, which is
what a test of an endpoint holds rather than a `ValidationResult`:

```csharp
exception.ShouldReport("Vin", "The VIN is required.");

problemDetails.Errors.ShouldReport([
    new("Vin", "The VIN is required."),
    new("Mileage", "The mileage must be greater than or equal to 0.")]);
```

Success is `result.Errors.Should().BeEmpty()` in whichever assertion library you already use.

### One rule at a time

`TestValidator<T>` declares the rule under test in the test itself, so a reader does not have to open a
second file to learn what is being validated:

```csharp
var validator = new TestValidator<Invoice>();
validator.Property(i => i.Reference).NotEmpty().MaximumLength(32);

var result = await validator.ValidateAsync(new Invoice());

result.ShouldReport("Reference", "Reference is required.");
```

Declare every rule before validating: the first run freezes the rules, and a rule declared afterwards
throws rather than being silently ignored. `Group` is re-exposed the same way, for a test about a rule
in a [rule group](#rule-groups) — which then needs a selection to run:

```csharp
var validator = new TestValidator<Invoice>();
validator.Group("Create", () => validator.Property(i => i.Reference).NotEmpty());

var result = await validator.ValidateAsync(new Invoice(), new NValidationOptions { ValidationGroups = "Create" });
```

The [condition blocks](#conditions-when-and-unless) — `When`, `Unless` and `When<TData>` — are re-exposed
too, and a rule that reads [data](#data-a-validation-carries) is handed it on the options of the run:

```csharp
var validator = new TestValidator<Car>();
validator.Property(c => c.Mileage).Must<ListingPolicy>((_, mileage, policy) => mileage <= policy.MaximumMileage);

var options = new NValidationOptions { ValidationData = [new ListingPolicy(200_000, false)] };
var result = await validator.ValidateAsync(new Car { Mileage = 250_000 }, options);

result.ShouldReportErrorCode("Mileage", "Must");
```

Because every rule reports an `ErrorCode`, a test can assert *which* rule fired rather than its wording,
and stay independent of translations. One validator, one run, no message provider to swap:

```csharp
var validator = new TestValidator<Invoice>();
validator.Property(i => i.Reference).MinimumLength(10);

var result = await validator.ValidateAsync(new Invoice { Reference = "AB" });

result.ShouldReportErrorCode("Reference", "MinimumLength");
```

`TestValidator<T>` also takes a message provider, for a test about what a provider of your own words a
rule as. `ErrorCodeProvider.Instance` is the one that answers every code with itself, which is how to
assert a code *through* a message where something in the way only exposes the text.

A rule that compares against "now" takes a clock the test owns, so it does not start failing on a future
Tuesday:

```csharp
var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

var validator = new TestValidator<Invoice>();
validator.Property(i => i.DueDate).InTheFuture(clock);

var result = await validator.ValidateAsync(new Invoice { DueDate = new DateTime(2025, 12, 31) });

result.ShouldReport("DueDate", "DueDate must be a date in the future.");
```

`TestTimeProvider.Advance(...)` moves it, for a test about something that happens on either side of a
boundary.

### Testing your own message provider

An application which resolves messages itself owes every code a text. One assertion covers the lot, so a
missing translation shows up in the suite rather than as a raw code in a response:

```csharp
[Fact]
public void GetMessage_AnswersForEveryKeyOfTheCore()
{
    new ResourceValidationMessageProvider().ShouldResolveEveryCoreErrorCode();
}
```

It checks that each code of `ValidationMessageProviderAssertions.CoreErrorCodes()` resolves to something
other than the code itself, and that no `{Placeholder}` is left unsubstituted. It does not require a
message to name the failing property — that is the translation's call.

## Thank You

Thanks to everyone who has contributed to this project.

If you find a bug or want to propose a feature, feel free to open an issue on GitHub.

## License

This project is licensed under the MIT license.
