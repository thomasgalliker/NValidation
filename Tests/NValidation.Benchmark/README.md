# NValidation.Benchmark

```bash
dotnet run -c Release --project Tests/NValidation.Benchmark -- --filter '*'
```

Add `--job short` while iterating; drop it for numbers worth quoting. `--filter` takes a full name, so
`*Benchmark.ValidationBenchmark.*` picks that class alone rather than everything ending in
`ValidationBenchmark`.

## Baseline

Taken 2026-09-23 on an Apple M4 Pro, .NET 10, `--job medium`. Ratios matter more than the absolute
numbers; take them again on the same machine before and after a change rather than comparing to these.
[COMPARISON.md](COMPARISON.md) sets this run against the previous one, row by row.

Run benchmarks with nothing else happening on the machine. A build running alongside one is enough to
move the timings.

### One validation — `ValidationBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `SingleObject` (4 rules, one flat object) | 59 ns | 72 B |
| `WholePayload` (`CarValidator`, valid) | 270 ns | 528 B |
| `WholePayloadWithFailures` | 546 ns | 2 360 B |
| `WithAnAwaitingRule` (one rule that truly suspends) | 2 189 ns | 831 B |

A validation which reports nothing allocates **72 bytes whatever the validator's width** — one
`ValidationFrame` for the pass, and nothing per property. The rule chains cost no allocation at all:
`RuleContext` is a struct over that frame, and the error list is not built until something fails. The
frame carries the run's inherited settings, the collection entry under judgement and the cancellation
token: 8 bytes of it are what composed validators inheriting their composer's settings cost, and 8 more
are what the call asks for — the rule groups it selects, and the data and properties it hands over, which
share one reference. The second 8 are what took the frame past a cache line, which is most of what the
groups cost a validator that declares none — see [Rule groups](#rule-groups--groupbenchmark).

`WholePayload` is measured through `CarValidator`, which declares chains in the `Create` and `Listing`
groups. A validation selecting none walks the chains of the default group alone, kept in an array of their
own, so the grouped chains cost it nothing.

Validation is asynchronous only, but a validator whose every rule judges rather than awaits runs with no
async state machine anywhere in it — and so does a validator it composes through `SetValidator` or
`ForEach`, as long as that one judges too. That is what took `WholePayload` from 384 ns to 238 ns: its
nested validators and its collection used to force every loop onto the awaiting side.
`WithAnAwaitingRule` is here to contrast; it is the only benchmark that genuinely suspends, and the one
that varies most — from 1.6 µs to 3.6 µs within this run, and 1.1 µs in a run of the same code an hour
earlier, because what it measures is mostly the thread pool resuming it.

`SingleObject` carries one `EmailAddress()` rule, and the few nanoseconds it gained over the previous
run is that rule: the address scanner reads the full grammar where the previous check took a shortcut
for the everyday shape. `WholePayload` carries the same rule on 240 ns of work, where the difference is
inside the run's noise. `EmailAddressBenchmark` below has the number on its own.

Every benchmark **returns** its task rather than `await`ing it, so BenchmarkDotNet awaits it and the
measurement is the library's cost, not a wrapper state machine belonging to the benchmark. Awaiting here
once doubled the apparent overhead.

### What a property chain costs — `ChainCostBenchmark`

The same rule declared a varying number of times over one payload, so the slope is the per-chain term.

| Chains | Mean | Allocated |
| ---: | ---: | ---: |
| 0 | 10.6 ns | 72 B |
| 1 | 17.6 ns | 72 B |
| 4 | 26.6 ns | 72 B |
| 16 | 65.1 ns | 72 B |

**Flat in allocation, ~3 ns per chain in time.** This is the benchmark to watch: the per-chain term is
what grows with a real payload, and it is the one that used to dominate — it was 112 B and ~24 ns a
chain before the frame and the struct context. The context is five references and a count; growing it
by two references once cost 6 ns on the first chain, because its construction stopped being inlined.

It did not move when `PropertyRuleBuilder` became a class: the builder is what a chain is *declared*
through, and nothing on the validation path holds one.

### Resolving a validator — `ValidatorResolutionBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `Scoped` | 13 873 ns | 37 136 B |
| `Singleton` | 25 ns | 128 B |
| `Scoped`, with safe promotion | 25 ns | 128 B |

Building the validator graph costs **over thirty times what using it does**, which makes it by far the
largest number in the library. A validator declares its rules in its constructor and never changes them,
so `PromoteSafeValidatorsToSingleton` is on by default and a validator which provably depends on nothing
scoped is built once for the process. The `Scoped` row is what a host pays only where it names that
lifetime itself, or where promotion declined.

The `Scoped` row grew by about 8 KB and 3.6 µs with the rework of rule groups, and nearly all of it is the
sample rather than the library: `CarValidator` now declares what a listing check asks for — five chains
more, two of them inside a condition block — and a declared chain costs its builder, its closures and the
expression tree its lambda compiles to. Built outside the container, the `CarValidator` of the previous
run allocates 13.1 KB against the previous library and 13.4 KB against this one; the current one
allocates 19.3 KB. The library's share is a few fields: those a validator carries to keep what it
resolved, and the groups each rule records for a validator it hands its value to. The two promoted rows
show what any of it costs a host that leaves promotion on: nothing.

### Per entry of a collection — `CollectionValidationBenchmark`

| Entries | Mean | Allocated | Before |
| ---: | ---: | ---: | ---: |
| 0 | 17 ns | 72 B | 208 B |
| 10 | 510 ns | 1 640 B | 4 280 B |
| 100 | 4 705 ns | 14 600 B | 40 280 B |
| 1 000 | 47 746 ns | 144 200 B | 400 280 B |

**~144 B per entry, down from ~400 B, and nothing for the `ForEach` itself.** One error list and one
`ElementScope` — the entry's position and the name it is reported under — are reused for the whole
collection rather than allocated per entry, and an entry's name (`ServiceHistory[7]`, two strings) is
built only when the entry actually has something to report. The empty collection used to cost 192 B
because the composed rule passed a method group of the struct context as a delegate, which boxed the
context and allocated the delegate on every validation; it now hands the frame over.

What remains is exactly two `ValidationFrame`s per entry: one for the entry's inline rules and one for
the validator its entries have of their own — 144 B rather than the 128 B of the run before rule
groups, because each of the two frames grew by the 8 bytes the selection costs. The argument that makes reusing the scope safe — entries
are judged one after another, and nothing an entry reported keeps a reference to it — would apply to the
frames too, but a frame is what every rule context points at, and 144 B an entry is not worth a mutable
instance.

A validator composed into another keeps what its pass resolves to while its composer keeps handing it the
same settings, so from the second entry on an entry's pass reads what an earlier one resolved rather than
resolving it again. A composer handing over something new each time — options built per call — makes it
keep nothing rather than allocate for it.

Measured through a validator that declares element rules and nothing else. `CarValidator` caps its
history, and a chain stops at its first failure, so measuring through it reported a collection over the
cap as *cheap* — it never reaches the entries at all.

### Conditions — `ConditionBenchmark`

Conditions fold into one another as they are declared, so three of them ask three nested delegates before
the property is read. Measured at 18.1 ns for none, 18.7 ns for one and 20.0 ns for three: the folding is
not worth avoiding.

The same conditions declared as `When` blocks around the chains cost the same — 17.8, 18.9 and 19.7 ns —
because a block's condition is folded into each chain declared inside it rather than asked once for the
block.

### Rule groups — `GroupBenchmark`

The same chains over the same payload, declared in no group and then all in one, so what a group costs
is the difference between the rows rather than a number on its own.

| | 4 chains | 16 chains |
| --- | ---: | ---: |
| `Ungrouped` — no chain is in a group | 26.3 ns | 63.1 ns |
| `GroupedAndSelected` — every chain is in the selected group | 35.5 ns | 102.7 ns |
| `GroupedAndSkipped` — no group is selected, so every chain is skipped | 10.9 ns | 10.5 ns |
| `GroupedUnderAll` — `ValidationGroups.All` | 27.2 ns | 65.0 ns |
| `Exclusive` — `ValidationGroups.Only("Create")` over the same chains | 35.0 ns | 102.3 ns |
| `ExclusiveOverMixed` — the same, where half the chains are in the default group | 26.7 ns | 62.9 ns |
| `DefaultTagged` — every chain in the default group and `Create`, nothing selected | 26.3 ns | 63.3 ns |

**A call selecting no group pays nothing for the grouped chains.** The chains in the default group are
frozen into an array of their own, and a call that names no group walks that array: a pass whose every
chain is grouped costs the same 10.5 ns for 4 chains or 16, the price of a validator with no chain at all.

**A validator which declares no group is walked by the loop it was walked by before groups existed.**
The gate is asked once per pass rather than once per chain: where each chain runs is frozen into arrays
beside the rules, those are null when nothing is grouped, and the two loops are separate methods.
Measured the other way — one branch inside the shared loop — the same 16 chains cost 70 ns rather than
64.5. The arrays run parallel rather than as one array of records, so a chain its own groups select costs
the gate the one reference it would cost with nothing else to know.

What the feature costs a validator with no group at all is therefore the frame's 8 bytes, and the cache
line they push it over: 55.5 ns before rule groups existed, 59.2 ns with the frame grown and no gate in
the loop at all, 64.5 ns as it first shipped.

**Selecting everything, or the default group alone, needs no gate.** `ValidationGroups.All` runs every
chain, so it is walked by the loop without one and costs what `Ungrouped` does. So does `DefaultTagged`,
where every chain is in the default group as well as a named one.

**`Only` costs what an additive selection costs**: the same gate, asked of the same chains. Where it
leaves chains of the default group out — `ExclusiveOverMixed` — those are passed over without their
property being read.

### Reaching a nested validator's group — `ReachThroughBenchmark`

A model whose four chains are in the default group, one of them handing the manufacturer to a validator
whose four chains are all in `Create`, validated three ways. Each costs two frames, the model's and the
manufacturer's.

| | Mean | Allocated |
| --- | ---: | ---: |
| `DefaultGroup` — nothing selected | 49.0 ns | 144 B |
| `BothGroups` — `Create` on top of the default group | 91.1 ns | 144 B |
| `NestedGroupAlone` — `ValidationGroups.Only("Create")` | 85.9 ns | 144 B |

`NestedGroupAlone` runs the manufacturer's four chains and, of the model, only the part of one chain that
hands the manufacturer on: the model's own rules are in the default group, which the selection leaves
out. It costs what `BothGroups` does less the model's three own chains, give or take the gate asked of
the four: reaching a group through a chain adds no pass and no allocation of its own.

### Data a validation carries — `ValidationDataBenchmark`

Four chains, handed a `ListingPolicy` or not; the last two rows put a condition that reads it on every
chain.

| | Mean | Allocated |
| --- | ---: | ---: |
| `NoData` | 23.5 ns | 72 B |
| `DataReused` — options holding the data, built once | 28.4 ns | 72 B |
| `DataFresh` — the same options built for every call | 59.1 ns | 248 B |
| `WhenAbsent` — a condition on the data, none handed over | 25.0 ns | 72 B |
| `WhenPresent` — the same, with the data handed over | 48.0 ns | 72 B |

**Data kept with the options it travels in allocates nothing per call.** It rides in the reference the
group selection already takes, so the frame is the size it was. Options built for every call cost the
options, the data and the object pairing them with the selection. A chain whose condition reads data the
call did not hand over is skipped without its property being read.

### Validating some properties — `PartialValidationComparisonBenchmark`

The NValidation rows of the comparison: five chains, validated whole and limited to two of them.

| | Mean | Allocated |
| --- | ---: | ---: |
| Every property | 34.9 ns | 72 B |
| Two properties | 55.7 ns | 72 B |

**Limiting a validation is not a way to make a cheap one cheaper.** Each chain's name is matched against
the selection, which costs more than the three cheap rules it spares here. What it is for is reporting only
what a request touched, and skipping the rules of the rest where they are expensive.

### Reading a mail address — `EmailAddressBenchmark`

One validator, one rule, the same value four ways: the scanner behind `EmailAddress()` (every
refinement on), the framework's mail parser plus a round-trip check — the implementation the scanner
replaced, kept verbatim in the benchmark — the small pattern the .NET guidance recommends, and the
one-`@` check as the floor. Values are chosen so that accepting and refusing are both measured, since
the two paths cost very different amounts.

| Value | Scanner | Framework parser | Pattern | One `@` |
| --- | ---: | ---: | ---: | ---: |
| `info@aurora-motors.example` | 31 ns · 64 B | 26 ns · 64 B | 47 ns · 64 B | 13 ns · 64 B |
| `sales+fleet@aurora-motors.co.uk` | 33 ns · 64 B | 27 ns · 64 B | 49 ns · 64 B | 14 ns · 64 B |
| `"john doe"@example.com` | 27 ns · 64 B | 92 ns · 280 B | refused | 13 ns · 64 B |
| `verkauf@aurora-motörs.example` | 301 ns · 208 B | 142 ns · 296 B | 50 ns · 64 B | 14 ns · 64 B |
| `not an email` | 96 ns · 824 B | 152 ns · 792 B | 115 ns · 800 B | 106 ns · 760 B |
| `missing-at.example.com` | 95 ns · 824 B | 149 ns · 760 B | 126 ns · 800 B | 110 ns · 760 B |
| `a@-b.example` | 98 ns · 824 B | accepted | accepted | accepted |
| a domain with a character IDNA refuses | 2 471 ns · 1 216 B | accepted | accepted | accepted |
| 4 KB with no `@` | 96 ns · 824 B | 2 072 ns · 760 B | 2 172 ns · 800 B | 262 ns · 760 B |
| 4 KB local part, then `@example.com` | 97 ns · 824 B | accepted, 1 832 ns | accepted, 2 108 ns | accepted, 159 ns |

The 64 B on every accepting row is the validation frame, so the scanner allocates nothing to accept an
ASCII address, and nothing to refuse one: a refusal's ~800 B is the error being reported, and the
scanner's rows carry 64 B more than the floor because "is not a valid email address" is a longer
message than "is not valid". This table was taken before the frame grew to 72 B for rule groups, so
every row of it allocates 8 B more today; what the scanner itself allocates is unchanged.

**Where the scanner is slower, and why.** The everyday address costs it about 6 ns more than the
check it replaced, which took a shortcut for exactly that shape and left everything else to the
framework's parser. The scanner reads the full grammar for every value — the 64- and 255-octet
limits, the 63-octet label, the whole of `atext`, and whether the text is well-formed — and that is
the price of it. An internationalized domain costs the round trip through `IdnMapping`, which is the
one accepting row that allocates; a domain IDNA refuses costs the exception that API reports with,
and nothing else reaches that path. The pattern is slower than the scanner on every value it decides
the same way, and faster only where it accepts what the scanner refuses.

**Where it is faster.** Every refusal, by a third or more, and by twenty times on a long value: the
scanner stops at the first character that cannot be part of an address, and nothing is parsed into an
object first. A quoted local part, which the previous check always handed to the parser, costs the
same as a plain one.

### The address and its domain — `EmailAddressChainBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `EmailAddress().RequireTopLevelDomain(...)`, one rule | 42 ns | 64 B |
| The same as two rules, as it was | 130 ns | 328 B |

The domain rules are refinements of the address rule rather than rules of their own, so a value is
parsed once and the domain is read off that parse. Two rules parsed it twice and allocated the parsed
address both times.

### Not measured here

The ASP.NET Core filter is not benchmarked — that would need a request-pipeline harness. Its per-request
reflection was removed on the reasoning that the answer is fixed once the application model is built,
not on a measurement.
