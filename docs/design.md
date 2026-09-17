# Design notes

The reasoning behind the shape of NValidation, kept here so the code and its doc comments can say what
a member does without arguing why on every one. Each section names the decision, what it buys, and what
it deliberately gives up.

## Rules are plain C# declared in a constructor

`Property(x => x.Name).NotEmpty()` builds a chain per property. The expression is read once to produce
three things: the property name the failure is reported under (`PropertyPath`), a compiled accessor
(`PropertyAccessor`, cached per closed generic and property name) and a guard against dereferencing an
object the payload omitted (`ReachabilityGuard`). The caches are statics of a generic instantiation
rather than dictionaries keyed by `Type`, so a validator declared for a type from a collectible
`AssemblyLoadContext` dies with the context.

The expression has to reach the property through the lambda's own parameter. The path it produces is
both the reported name and the cache key, so two expressions sharing a path would share a delegate and
validate the wrong value. `Property(name, accessor)` exists for a value that is not a member path.

A validator freezes its rules on the first validation: the rules and each chain's checks are taken as
arrays, and a rule declared afterwards throws. Silently ignoring it was the alternative, and it was worse.

## Everything absent passes

A null nested object, a missing collection, an absent value, a compared property behind an object the
payload omitted: none of them fails the rule that was asked about the *value*. Requiring presence is
always a rule of its own (`NotNull`, `NotEmpty`, `NotDefault`). A rule that dereferenced without a
guard would turn a bad request into a server error, which is the one thing a validation library must
never do.

## An annotation is not a rule

`Property(x => x.Name)` always builds the chain for the *nullable* form of the property's type:
`TProperty?` on an unconstrained type parameter, which adds `?` for a reference type and is a no-op for a
value type. That is what lets one `NotEmpty()` be written for `string?` and still bind to a property
declared `string`, and it is what keeps `RuleContext.Value` maybe-null in every rule — a payload that
arrived over the wire can carry `null` whatever the model declared.

Nothing here reads `NullableAttribute`. An annotation is a claim about the code that declares it, not
about the payload, and metadata a trimmer may remove is not a place to keep rules.

`NotNull()` therefore does not narrow the chain to the non-nullable form afterwards. It could only be
sound under `StopAtFirstError`, and the behavior is resolved while validating rather than while the rules
are declared — the compile-time type would depend on a run-time setting. Under `ValidationBehavior.All`
the chain carries on and every rule after `NotNull()` is handed the same null.

## Error code is the message key

A rule reports a code from `ValidationErrorCodes` and its arguments; the host resolves the wording
through `IValidationMessageProvider`. `WithErrorCode` therefore does two things at once — it names the
rule a client branches on and selects the text — which is what lets an application's own `Must` rule
localize exactly like a shipped one. `WithMessage` is a template substituted against the same arguments.

## The settings ladder

Two settings can be given at several levels: the message provider and the two behavior axes. Both travel
one ladder, resolved once per pass from the most specific level that names the setting:

1. what the validator declared for itself (`ValidationMessageProvider`, `ValidationBehaviors`);
2. what the pass inherits — the options passed to `ValidateAsync`, or, for a validator composed into
   another, what its composer resolved;
3. what `AddNValidation` configured, for a validator the container built;
4. `NValidationOptions.Default`;
5. the built-in English, every property, one message each.

What a pass resolves is substituted into the run it hands down, so a nested validator and the element
chain of a `ForEach` inherit their composer's answer as rung 2 and keep anything they declared for
themselves. This is what makes one setting on the outermost validator, on the options of a call or on
`Default` reach a whole graph, without a validator's own word ever being overruled by composition.

`NValidationOptions` is an immutable record and `ValidationBehaviors` a readonly record struct with
nullable axes; `null` means "the level below answers". There is nothing to freeze: `Default` is a
reference swapped atomically, and a run already holding the old options keeps them.

The registration hands its settings only to the validators it constructs. It registers no message
provider on the host's behalf, because a provider handed over would outrank the options of a call, and a
default captured at resolution would hide a `Default` assigned afterwards.

## One object per pass

`RuleContext<T, TProperty>` — what a rule sees — is a readonly struct over a `ValidationFrame`, one
object allocated per validator pass. The frame holds the instance, the error list (built only when
something fails) and the `ValidationRun`: the inherited settings, the `ForEach` entry under judgement
and the cancellation token. A pass that reports nothing allocates the frame and nothing else, whatever
the validator's width. A failed result takes the list rather than copying it.

The context is rebuilt per rule rather than mutated between rules, which is what confines `WithMessage`
and `WithErrorCode` to the rule they follow. Composed rules hand the frame itself to the element
builder; passing a method group of the struct context would box it and allocate a delegate on every run.

## Synchronous where nothing awaits

Validation is asynchronous only, because one awaiting rule makes a whole chain asynchronous and a
synchronous entry point could only work by deciding at run time whether the rules happened to finish.
But almost every rule judges rather than awaits, so a chain, a validator and a `ForEach` whose every
rule does are run through synchronous twins of the awaiting loops, with no async state machine anywhere.
`SetValidator` and `ForEach` ask the composed validator once, when they are declared, and choose the
twin accordingly. The twins are kept adjacent; a change to the cascade rule is made to both.

`IsSynchronous` is settled at declaration, so a deterministic synchronous entry point would be possible
now; it is not offered because nothing has asked for it, and adding it later breaks nothing.

## Collections

`ForEach` judges each entry as its own pass. One error list and one `ElementScope` serve the whole
collection: the scope is pointed at each entry in turn, what an entry reports is copied out under the
entry's name straight away, and an entry is named only when it has something to report. The scope also
supplies `{CollectionIndex}` and the property name of a rule declared on the entry itself, whichever
provider renders the message. The two frames an entry costs are not reused across entries: the same
argument that makes scope reuse safe — nothing an entry reported keeps a reference to it — would apply,
but a frame is what every rule context points at, and the saving is not worth a mutable instance.

## Registration

A validator declares its rules in its constructor and never changes, so building it per request is the
largest cost the library imposes; a validator which provably depends on nothing scoped is registered as a
singleton. A lifetime the host named is left alone by the default promotion and lifted only when the host
asked for promotion by name, which is why the setting is a tri-state underneath a `bool` — the one place
the library needs to tell "on by default" from "on because you said so". Registration is order-independent: an explicit
`AddValidator` wins over a scan wherever it is written, and two explicit registrations for one payload
are refused.
