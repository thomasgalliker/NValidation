# NValidation.Benchmark

```bash
dotnet run -c Release --project Tests/NValidation.Benchmark -- --filter '*'
```

Add `--job short` while iterating; drop it for numbers worth quoting. `--filter` takes a full name, so
`*Benchmark.ValidationBenchmark.*` picks that class alone rather than everything ending in
`ValidationBenchmark`.

## Baseline

Taken 2026-09-18 on an Apple M4 Pro, .NET 10, `--job medium`. Ratios matter more than the absolute
numbers; take them again on the same machine before and after a change rather than comparing to these.
[COMPARISON.md](COMPARISON.md) sets this run against the previous one, row by row.

Run benchmarks with nothing else happening on the machine. A build running alongside one is enough to
move the timings.

### One validation — `ValidationBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `SingleObject` (4 rules, one flat object) | 55 ns | 72 B |
| `WholePayload` (`CarValidator`, valid) | 251 ns | 528 B |
| `WholePayloadWithFailures` | 517 ns | 2 360 B |
| `WithAnAwaitingRule` (one rule that truly suspends) | 1 329 ns | 831 B |

A validation which reports nothing allocates **72 bytes whatever the validator's width** — one
`ValidationFrame` for the pass, and nothing per property. The rule chains cost no allocation at all:
`RuleContext` is a struct over that frame, and the error list is not built until something fails. The
frame carries the run's inherited settings, the collection entry under judgement and the cancellation
token: 8 bytes of it are what composed validators inheriting their composer's settings cost, and 8 more
are the rule groups a run selects. The second 8 are what took the frame past a cache line, which is
most of what the groups cost a validator that declares none — see
[Rule groups](#rule-groups--groupbenchmark).

`WholePayload` is measured through `CarValidator`, which declares one chain in a rule group, so the
figure now includes a chain being skipped.

Validation is asynchronous only, but a validator whose every rule judges rather than awaits runs with no
async state machine anywhere in it — and so does a validator it composes through `SetValidator` or
`ForEach`, as long as that one judges too. That is what took `WholePayload` from 384 ns to 238 ns: its
nested validators and its collection used to force every loop onto the awaiting side.
`WithAnAwaitingRule` is here to contrast; it is the only benchmark that genuinely suspends.

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
| 0 | 10.3 ns | 72 B |
| 1 | 18.4 ns | 72 B |
| 4 | 27.0 ns | 72 B |
| 16 | 64.5 ns | 72 B |

**Flat in allocation, ~2.7 ns per chain in time.** This is the benchmark to watch: the per-chain term is
what grows with a real payload, and it is the one that used to dominate — it was 112 B and ~24 ns a
chain before the frame and the struct context. The context is five references and a count; growing it
by two references once cost 6 ns on the first chain, because its construction stopped being inlined.

It did not move when `PropertyRuleBuilder` became a class: the builder is what a chain is *declared*
through, and nothing on the validation path holds one.

### Resolving a validator — `ValidatorResolutionBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `Scoped` | 9 493 ns | 27 128 B |
| `Singleton` | 29 ns | 128 B |
| `Scoped`, with safe promotion | 30 ns | 128 B |

Building the validator graph costs **over thirty times what using it does**, which makes it by far the
largest number in the library. A validator declares its rules in its constructor and never changes them,
so `PromoteSafeValidatorsToSingleton` is on by default and a validator which provably depends on nothing
scoped is built once for the process. The `Scoped` row is what a host pays only where it names that
lifetime itself, or where promotion declined.

The `Scoped` row grew by about 0.7 KB when `PropertyRuleBuilder` became a class: one 24-byte builder
per declared property, plus the options object and the derived builder an `EmailAddress()` rule now
declares. It is the only allocation figure the change moved, and the two promoted rows show what it
costs a host that leaves promotion on: nothing. The `Singleton` and promoted rows do the same work,
and the few nanoseconds both gained over the previous run are the machine's, not the library's: no code
on that path changed, and they allocate what they did.

### Per entry of a collection — `CollectionValidationBenchmark`

| Entries | Mean | Allocated | Before |
| ---: | ---: | ---: | ---: |
| 0 | 25 ns | 72 B | 208 B |
| 10 | 552 ns | 1 640 B | 4 280 B |
| 100 | 5 030 ns | 14 600 B | 40 280 B |
| 1 000 | 47 276 ns | 144 200 B | 400 280 B |

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

Measured through a validator that declares element rules and nothing else. `CarValidator` caps its
history, and a chain stops at its first failure, so measuring through it reported a collection over the
cap as *cheap* — it never reaches the entries at all.

### Conditions — `ConditionBenchmark`

Conditions fold into one another as they are declared, so three of them ask three nested delegates before
the property is read. Measured at 16 ns for none, 16 ns for one and 18 ns for three: the folding is not
worth avoiding.

### Rule groups — `GroupBenchmark`

The same chains over the same payload, declared in no group and then all in one, so what a group costs
is the difference between the rows rather than a number on its own.

| | 4 chains | 16 chains |
| --- | ---: | ---: |
| `Ungrouped` — no chain is in a group | 27.5 ns | 64.6 ns |
| `GroupedAndSelected` — every chain is in the selected group | 34.5 ns | 98.3 ns |
| `GroupedAndSkipped` — no group is selected, so every chain is skipped | 13.9 ns | 20.1 ns |
| `GroupedUnderAll` — `ValidationGroups.All` | 40.2 ns | 119.2 ns |

**A chain the run skipped costs about 0.4 ns**, which is what the 16-chain skipping row says: 20 ns for
a pass whose every chain was passed over, against 10 ns for a pass with no chain at all. The property is
not read and the chain's condition is not asked, so a grouped chain a request does not want is close to
not being declared.

**A validator which declares no group is walked by the loop it was walked by before groups existed.**
The gate is asked once per pass rather than once per chain: the groups of the rules are frozen into an
array beside them, that array is null when nothing is grouped, and the two loops are separate methods.
Measured the other way — one branch inside the shared loop — the same 16 chains cost 70 ns rather than
64.5.

What the feature costs a validator with no group at all is therefore the frame's 8 bytes, and the cache
line they push it over: 55.5 ns before rule groups existed, 59.2 ns with the frame grown and no gate in
the loop at all, 64.5 ns as it ships.

`GroupedUnderAll` asks one bool and answers yes, so it should be the cheapest of the selecting rows and
is not. The difference is code layout rather than work done; it is reported here as measured.

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
