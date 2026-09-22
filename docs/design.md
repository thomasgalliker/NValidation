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

Three settings can be given at several levels: the message provider, the two behavior axes and the rule
groups a run selects. All travel one ladder, resolved once per pass from the most specific level that
names the setting:

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

The group selection is the one exception on rung 1: a validator cannot select groups for itself. The
same validator serves every operation, so which of its rules apply is a fact about the call, and a
validator that could overrule the caller on it would be a validator nobody could reuse.

The registration hands its settings only to the validators it constructs. It registers no message
provider on the host's behalf, because a provider handed over would outrank the options of a call, and a
default captured at resolution would hide a `Default` assigned afterwards.

## Rule groups

A chain carries zero or more names; a run carries a selection. A chain in no group runs in every
validation, and a grouped chain runs where the selection names one of its groups or is
`ValidationGroups.All`. A call that selects nothing therefore validates exactly as it did before any
group was declared, which is what lets a group be added to one chain of a shipped validator without
every existing caller having to say so. What it gives up is "everything except": there is no selection
that means *all but Create*, because the case for it is a rule about the object, and `When` already
answers that.

The gate is asked in the validator's rule loop, before the chain's condition and before the property is
read. A chain the run did not select has therefore reported nothing and read nothing, so it cannot trip
a `StopAtFirstError` run and cannot dereference an object the payload omitted. It is the same answer
`When` gets, one level earlier.

`IsSynchronous` is still settled over every rule, groups included: it is decided when the rules are
declared and the selection is not known until a run asks. A validator whose only awaiting chain is
grouped runs through the awaiting loop even where that group was not selected. Deciding it per run would
mean re-deciding it per run, which is the cost the flag exists to avoid.

The groups of the rules are frozen into an array beside the rules, and that array is null where no chain
is in a group. The loop is then chosen once per pass rather than branched per chain: a validator
declaring no group is walked by the loop it was walked by before the feature existed. What it still
costs is 8 bytes on the frame, which is what took the frame past a cache line — the one price the
feature charges a validator that does not use it, and the reason the selection is not also copied into
anything the rules touch per chain.

Names are compared ordinally. A group name is a token the application chose and the caller repeats, like
an error code, and a case-insensitive match would make `Create` and `create` the same group in the
library while they stay two constants in the application.

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

## The email grammar is the library's own

`EmailAddress()` reads a value with a scanner of its own — RFC 5321 §4.1.2 `Mailbox`, with UTF-8 where
RFC 6531 admits it — rather than handing it to `System.Net.Mail.MailAddress`. That parser is a fine
reader of mail headers and errs permissive as a validator: it accepts a trailing dot in the local part
(its source calls this a deliberate departure, for compatibility with other mail clients), a domain
label beginning or ending with a hyphen, any bracketed text as an address literal, a local part or a
domain of any length, and any non-ASCII domain without asking whether it is a well-formed
internationalized name. None of that can be tightened from outside, because the parser *is* the
definition. Owning the grammar is what lets the rule say what it accepts.

Full conformance is not the default, though. The RFC grammar admits comments, folding whitespace,
quoted local parts and address literals — forms that are legal, that no provider issues, and that a
contact-email field never means. The default is the everyday shape: a dot-separated local part, one
`@`, a domain of two or more labels. Each legal form it leaves out is admitted by a refinement, and the
three refinements together are exactly the RFC's `Mailbox`, so a caller who wants full conformance is
one chain away from it rather than in a different mode.

The refinements sit on `EmailAddressRuleBuilder<T>`, which derives from the plain builder, so they exist
only where they apply and their misuse is a compile error: the plain builder's own members return the
plain builder, which does not have them. That is why `PropertyRuleBuilder<T, TProperty>` is a class
rather than a `readonly struct`. A struct cannot be derived from, and a wrapper does not work either —
C# does not apply extension methods through a user-defined conversion, and every rule in the library is
an extension method, so a wrapper would have ended every chain at the first refinement. The class costs
one 24-byte object per declared property, once, when the validator is built — the path
`ValidatorResolutionBenchmark` measures at some 26 KB per graph, and the reason validators are promoted
to singletons. It costs nothing where it matters: the builder is never touched while validating, and
`RuleContext`, the struct that keeps a rule run allocation-free, is unchanged.

The domain rules are refinements of the same rule rather than rules of their own. Two rules would parse
the value twice, could report two failures for one bad value, and had an order that mattered — a domain
rule written before the address rule silently passed everything. One rule parses once, asks each
constraint in turn over that parse, reports the first to object under its own error code, and cannot be
written in the wrong order.

An internationalized domain is the one place the scanner reaches outside itself: `IdnMapping.GetAscii`
is what knows the IDNA rules, and it reports a malformed name by throwing, with no `Try` form on any
target framework. The `catch` around it is how its answer is heard, not an error being swallowed, and it
is reached only when a domain carries a character beyond ASCII — which, in practice, is almost never.

## The URL grammar is the library's own, and stays a subset of `Uri`

`Url()` reads a value with a scanner of its own — RFC 3986, with the non-ASCII RFC 3987 admits — rather
than handing it to `Uri`. The reasoning is the email rule's, with one fact that settles it on its own.

**The verdict would depend on the operating system.** On Linux and macOS, `Uri.TryCreate("/orders/42",
UriKind.Absolute, out _)` succeeds and yields `file:///orders/42`, because a rooted path reads as a Unix
file path; so do `//example.com/path` and `C:\temp`. On Windows it answers differently. A rule whose
verdict depends on the build agent is not one anybody can reason about, and nothing outside the parser can
correct it.

The rest is the familiar shape of a reader pressed into service as a validator. `Uri` accepts
`javascript:alert(1)`, `data:text/html;base64,…`, `about:blank` and any invented scheme, which is the
stored-XSS sink a link field turns into. It trims surrounding whitespace, so a value that round-trips
through it is not the value that was validated. It accepts hosts DNS refuses — `my_host.example`,
`-example.com`, `example.com.`, the malformed punycode `xn--a.com` that `IdnMapping` itself throws on —
and it enforces no length limit on anything. And it rewrites what it just approved: `http://0x7f.1/` and
`http://2130706433/` become `127.0.0.1`, `http://1.2.3/` becomes `1.2.0.3`, `/%zz` becomes `/%25zz`,
`%2e%2e/%2e%2e/etc` becomes `/etc`, and `http://example.com@evil.example/` points at `evil.example`. The
application then stores the string it was given, not the one the parser read.

Pairing it with a round-trip check, as the email rule once did with `MailAddress`, does not work here:
`https://aurora-motors.example` has an `AbsoluteUri` of `https://aurora-motors.example/`, so the check
would refuse the most ordinary way anyone writes a URL.

It also allocates. `Uri.TryCreate` costs 56 bytes per call, so a validator with one URL rule would stop
being a pass that allocates the frame and nothing else. The scanner adds nothing over an empty pass.

**What makes owning the grammar safe is that the scanner accepts a strict subset of what `Uri` accepts.**
A value the rule approves is one the application can hand to `Uri` afterwards. That is a property rather
than a hope: `UrlsTests` sweeps every string up to five characters over an alphabet covering each
character class the scanner distinguishes, and fails if any accepted value is one `Uri` refuses. A second
test pins the stronger claim where it matters — for `http` and `https`, the two read the same scheme and
the same host.

A few decisions inside the grammar are worth stating, because none of them follows from the RFC alone:

- **A host whose last label is all digits is an address, never a name.** No top-level domain is numeric,
  and RFC 3986 reads a host of that shape as IPv4. One line refuses `1.2.3`, `123.456.789.0`, `0x7f.1`
  and `2130706433` as names, which is exactly the set another parser expands into an address nobody wrote,
  and it leaves the real dotted-quad form to `AllowIPAddress()`, where it is parsed rather than guessed at.
- **`[` and `]` are admitted in a query and a fragment**, which RFC 3986 reserves for the host. A query of
  the form `?ids[]=1` is everywhere and every browser sends it; refusing what the rest of the stack
  accepts would only be a false alarm. They stay refused in a path.
- **Credentials before the host are refused by default.** `https://example.com@evil.example/` reads to a
  person as a URL for one host and points at another, and a field that never carries credentials should
  not accept the shape. `AllowUserInfo()` is one method for the field that does.
- **A network-path reference stays refused even under `AllowRelative()`.** `//example.com/x` is a legal
  relative reference, and it names a host, which is how a field meant to hold a path sends a reader
  somewhere else.

What a host is lives in `HostNames`, shared with `EmailAddress()`: the DNS label grammar, the octet
limits, IDNA for a name beyond ASCII, and the two address forms. Two definitions of a valid host name in
one library drift, and the day they disagree is the day a value passes one rule and fails the other. A
test holds the two rules to the same answer.

The library ships nothing that claims to make a URL safe to fetch. A name resolves when the request is
made, it can point anywhere, and a redirect can move it again, so no rule about the text can decide where
a request ends up. A `RefusePrivateNetwork()` would read as a guarantee and deliver none, and the caller
who relied on it would be worse off than the one who knew they had to check at the socket.
