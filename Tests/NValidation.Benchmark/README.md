# NValidation.Benchmark

```bash
dotnet run -c Release --project Tests/NValidation.Benchmark -- --filter '*'
```

Add `--job short` while iterating; drop it for numbers worth quoting. `--filter` takes a full name, so
`*Benchmark.ValidationBenchmark.*` picks that class alone rather than everything ending in
`ValidationBenchmark`.

## Baseline

Taken 2026-09-05 on an Apple M-series Mac, .NET 10, `--job short`, **after** the section-C work. Ratios
matter more than the absolute numbers; take them again on the same machine before and after a change
rather than comparing to these.

### One validation — `ValidationBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `SingleObject` (4 rules, one flat object) | 215 ns | 536 B |
| `WholePayload` (`CarValidator`, valid) | 814 ns | 2 056 B |
| `WholePayloadWithFailures` | 1 458 ns | 4 272 B |

Validation is asynchronous only, so there is no sync/async pair left to compare. There used to be one,
and it showed the async path costing a constant +72 B: one `Task<ValidationResult>` per validation,
because `AsyncTaskMethodBuilder<TResult>` has no cached completed task for an arbitrary reference-typed
result. `ValueTask<ValidationResult>` removed that, which is why these numbers carry no task allocation
at all — every rule shipped here finishes synchronously, so nothing is ever allocated to represent
waiting.

Every benchmark **returns** its task rather than `await`ing it, so BenchmarkDotNet awaits it and the
measurement is the library's cost, not a wrapper state machine belonging to the benchmark. Awaiting here
once doubled the apparent overhead.

### Resolving a validator — `ValidatorResolutionBenchmark`

| | Mean | Allocated |
| --- | ---: | ---: |
| `Scoped` | 8 381 ns | 25 704 B |
| `Singleton` | 22 ns | 128 B |

Building the validator graph still costs **12× more than using it**. A scoped registration pays that on
every request; `ValidatorLifetime = ServiceLifetime.Singleton` pays it once for the process. This is by
far the largest number in the library, and the reason the lifetime is worth choosing deliberately.

### Per entry of a collection — `CollectionValidationBenchmark`

| Entries | Mean | Allocated | Before section C |
| ---: | ---: | ---: | ---: |
| 0 | 62 ns | 184 B | 184 B |
| 10 | 1 347 ns | 3 216 B | 5 824 B |
| 100 | 12 594 ns | 29 856 B | 56 944 B |
| 1 000 | 129 794 ns | 296 256 B | 590 544 B |

**~296 B per entry, down from ~568 B.** One error list is now reused for the whole collection instead of
allocated per entry, entries report into it directly rather than through a `ValidationResult` each, and
an entry's code — `ServiceHistory[7]`, two strings — is built only when the entry actually has something
to report.

What remains is mostly one `RuleContext` per rule per entry. Reducing that needs a design change, not a
tweak, so it has not been attempted.

Measured through a validator that declares element rules and nothing else. `CarValidator` caps its
history, and a chain stops at its first failure, so measuring through it reported a collection over the
cap as *cheap* — it never reaches the entries at all.

### Not measured here

The ASP.NET Core filter is not benchmarked — that would need a request-pipeline harness. Its per-request
reflection was removed on the reasoning that the answer is fixed once the application model is built,
not on a measurement.
