# NValidation.Benchmark

```bash
dotnet run -c Release --project Tests/NValidation.Benchmark -- --filter '*'
```

Add `--job short` while iterating; drop it for numbers worth quoting. `--filter` takes a full name, so
`*Benchmark.ValidationBenchmark.*` picks that class alone rather than everything ending in
`ValidationBenchmark`.

## Baseline

Taken 2026-09-16 on an Apple M4 Pro, .NET 10, `--job medium`. Ratios matter more than the absolute
numbers; take them again on the same machine before and after a change rather than comparing to these.
[COMPARISON.md](COMPARISON.md) sets this run against the previous one, row by row.

Run benchmarks with nothing else happening on the machine. A build running alongside one is enough to
move the timings.

### One validation — `ValidationBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `SingleObject` (4 rules, one flat object) | 53 ns | 64 B |
| `WholePayload` (`CarValidator`, valid) | 238 ns | 504 B |
| `WholePayloadWithFailures` | 500 ns | 2 336 B |
| `WithAnAwaitingRule` (one rule that truly suspends) | 1 305 ns | 815 B |

A validation which reports nothing allocates **64 bytes whatever the validator's width** — one
`ValidationFrame` for the pass, and nothing per property. The rule chains cost no allocation at all:
`RuleContext` is a struct over that frame, and the error list is not built until something fails. The
frame carries the run's inherited settings, the collection entry under judgement and the cancellation
token, which is the 8 bytes it grew by when composed validators started inheriting their composer's
settings.

Validation is asynchronous only, but a validator whose every rule judges rather than awaits runs with no
async state machine anywhere in it — and so does a validator it composes through `SetValidator` or
`ForEach`, as long as that one judges too. That is what took `WholePayload` from 384 ns to 238 ns: its
nested validators and its collection used to force every loop onto the awaiting side.
`WithAnAwaitingRule` is here to contrast; it is the only benchmark that genuinely suspends.

Every benchmark **returns** its task rather than `await`ing it, so BenchmarkDotNet awaits it and the
measurement is the library's cost, not a wrapper state machine belonging to the benchmark. Awaiting here
once doubled the apparent overhead.

### What a property chain costs — `ChainCostBenchmark`

The same rule declared a varying number of times over one payload, so the slope is the per-chain term.

| Chains | Mean | Allocated |
| ---: | ---: | ---: |
| 0 | 8.8 ns | 64 B |
| 1 | 17.9 ns | 64 B |
| 4 | 25.8 ns | 64 B |
| 16 | 57.7 ns | 64 B |

**Flat in allocation, ~2.7 ns per chain in time.** This is the benchmark to watch: the per-chain term is
what grows with a real payload, and it is the one that used to dominate — it was 112 B and ~24 ns a
chain before the frame and the struct context. The context is five references and a count; growing it
by two references once cost 6 ns on the first chain, because its construction stopped being inlined.

### Resolving a validator — `ValidatorResolutionBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `Scoped` | 9 135 ns | 26 400 B |
| `Singleton` | 23 ns | 128 B |
| `Scoped`, with safe promotion | 24 ns | 128 B |

Building the validator graph costs **over thirty times what using it does**, which makes it by far the
largest number in the library. A validator declares its rules in its constructor and never changes them,
so `PromoteSafeValidatorsToSingleton` is on by default and a validator which provably depends on nothing
scoped is built once for the process. The `Scoped` row is what a host pays only where it names that
lifetime itself, or where promotion declined.

### Per entry of a collection — `CollectionValidationBenchmark`

| Entries | Mean | Allocated | Before |
| ---: | ---: | ---: | ---: |
| 0 | 18 ns | 64 B | 208 B |
| 10 | 512 ns | 1 472 B | 4 280 B |
| 100 | 4 573 ns | 12 992 B | 40 280 B |
| 1 000 | 44 074 ns | 128 192 B | 400 280 B |

**~128 B per entry, down from ~400 B, and nothing for the `ForEach` itself.** One error list and one
`ElementScope` — the entry's position and the name it is reported under — are reused for the whole
collection rather than allocated per entry, and an entry's name (`ServiceHistory[7]`, two strings) is
built only when the entry actually has something to report. The empty collection used to cost 192 B
because the composed rule passed a method group of the struct context as a delegate, which boxed the
context and allocated the delegate on every validation; it now hands the frame over.

What remains is exactly two `ValidationFrame`s per entry: one for the entry's inline rules and one for
the validator its entries have of their own. The argument that makes reusing the scope safe — entries
are judged one after another, and nothing an entry reported keeps a reference to it — would apply to the
frames too, but a frame is what every rule context points at, and 128 B an entry is not worth a mutable
instance.

Measured through a validator that declares element rules and nothing else. `CarValidator` caps its
history, and a chain stops at its first failure, so measuring through it reported a collection over the
cap as *cheap* — it never reaches the entries at all.

### Conditions — `ConditionBenchmark`

Conditions fold into one another as they are declared, so three of them ask three nested delegates before
the property is read. Measured at 16 ns for none, 16 ns for one and 18 ns for three: the folding is not
worth avoiding.

### Not measured here

The ASP.NET Core filter is not benchmarked — that would need a request-pipeline harness. Its per-request
reflection was removed on the reasoning that the answer is fixed once the application model is built,
not on a measurement.
