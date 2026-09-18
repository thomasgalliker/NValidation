# Previous run against this run

The figures of the run taken for the second performance review, set against the run taken after the
email rule was rewritten and `PropertyRuleBuilder` became a class, for every NValidation row the
benchmark project measures. Both runs: Apple M4 Pro, .NET 10, BenchmarkDotNet `--job medium`, nothing
else running. The previous run's standard deviations were not kept, so the significance test uses this
run's: a change counts when it is past 5% and past three standard deviations. Allocation figures are
exact, so every byte shown as changed did change — and on the validation path, none did.

Ratios matter more than the absolute numbers; take both runs again on the same machine before trusting a
difference smaller than the noise column.


## `ValidationBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| SingleObject | 53.1 ns | 56.3 ns | 0.4 ns | +6% (slower) | 64 B | 64 B |
| WholePayload | 238.2 ns | 236.3 ns | 1.3 ns | -1% (within noise) | 504 B | 504 B |
| WholePayloadWithFailures | 500.3 ns | 493.7 ns | 8.2 ns | -1% (within noise) | 2,336 B | 2,336 B |
| WithAnAwaitingRule | 1,305.4 ns | 1,179.6 ns | 232.0 ns | -10% (within noise) | 815 B | 815 B |

## `ChainCostBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 0 chains | 8.8 ns | 9.0 ns | 0.0 ns | +2% (within noise) | 64 B | 64 B |
| 1 chain | 17.9 ns | 16.1 ns | 0.1 ns | -10% (faster) | 64 B | 64 B |
| 2 chains | 20.9 ns | 18.9 ns | 0.1 ns | -10% (faster) | 64 B | 64 B |
| 4 chains | 25.8 ns | 23.7 ns | 0.1 ns | -8% (faster) | 64 B | 64 B |
| 8 chains | 36.5 ns | 34.2 ns | 0.2 ns | -6% (faster) | 64 B | 64 B |
| 16 chains | 57.7 ns | 55.5 ns | 0.2 ns | -4% (within noise) | 64 B | 64 B |

## `CollectionValidationBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 0 entries | 17.9 ns | 17.1 ns | 0.1 ns | -5% (within noise) | 64 B | 64 B |
| 10 entries | 512.1 ns | 490.3 ns | 20.6 ns | -4% (within noise) | 1,472 B | 1,472 B |
| 100 entries | 4,572.7 ns | 4,994.0 ns | 521.1 ns | +9% (within noise) | 12,992 B | 12,992 B |
| 1,000 entries | 44.1 µs | 44.4 µs | 2.0 µs | +1% (within noise) | 128,192 B | 128,192 B |

## `ComparisonBenchmark (NValidation rows)`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 4 chains, valid | 52.2 ns | 57.4 ns | 0.3 ns | +10% (slower) | 64 B | 64 B |
| 4 chains, invalid | 386.3 ns | 340.2 ns | 2.8 ns | -12% (faster) | 2,256 B | 2,256 B |

## `PayloadComparisonBenchmark (NValidation rows)`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| nested payload, valid | 241.3 ns | 243.7 ns | 1.9 ns | +1% (within noise) | 504 B | 504 B |
| nested payload, invalid | 501.0 ns | 508.7 ns | 11.7 ns | +2% (within noise) | 2,336 B | 2,336 B |

## `WideComparisonBenchmark (NValidation rows)`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 34 chains, valid | 243.5 ns | 235.0 ns | 2.4 ns | -3% (within noise) | 64 B | 64 B |
| 34 chains, invalid | 689.3 ns | 686.2 ns | 2.5 ns | -0% (within noise) | 3,192 B | 3,192 B |

## `ValidatorResolutionBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| Scoped | 9,135.1 ns | 9,493.4 ns | 38.3 ns | +4% (within noise) | 26,400 B | 27,128 B |
| Singleton | 22.8 ns | 28.9 ns | 1.0 ns | +27% (see below) | 128 B | 128 B |
| ScopedWithSafePromotion | 24.2 ns | 29.5 ns | 0.7 ns | +22% (see below) | 128 B | 128 B |

## What moved the numbers

- **A malformed address is refused without being parsed.** The four-chain invalid payload carries
  `not-an-address`. The previous check handed it to the framework's mail parser, which read it
  backwards to the end before saying no; the scanner sees no `@` and stops. That is the whole of the
  46 ns the row lost. Measured on its own, that value fell from 152 ns to 96 ns, and 4 KB of junk from
  2,072 ns to 96 ns.
- **The everyday address costs about 5 ns more to accept.** The four-chain valid payload is the one
  row that got slower, and `SingleObject` is the same validator. The previous check took a shortcut for
  exactly that shape and left everything else to the parser; the scanner reads the full grammar for
  every value — the 64- and 255-octet limits, the 63-octet label, the whole of `atext`, whether the
  text is well-formed. On the nested payload, which carries the same rule on 240 ns of work, the
  difference is inside the noise.
- **The builder became a class, and the validation path did not notice.** A builder is what a chain is
  *declared* through, in the validator's constructor; nothing that runs a validation holds one. Every
  allocation figure on every row above is identical to the previous run, and the chain benchmark —
  the same measurement with no address rule in it — moved only in the faster direction.
- **Building a validator graph allocates 728 bytes more.** One 24-byte builder per declared property,
  22 of them in the `CarValidator` graph, plus the options object, the closure, its delegate and the
  derived builder an `EmailAddress()` rule now declares. It is paid where a host names a scoped
  lifetime; with promotion on, which is the default, the graph is built once for the process, and the
  two promoted rows allocate the same 128 B they did.
- **The address and its domain are one rule.** `EmailAddress().RequireTopLevelDomain(...)` parses the
  value once and reads the domain off that parse: 42 ns and 64 B, against 130 ns and 328 B for the two
  rules it replaced, which each parsed and each allocated the parsed address.

## What to distrust

- The two promoted resolution rows moved by about 6 ns each. Nothing on their path changed — they
  return a validator the container already built, and no builder is touched — and they allocate the
  same 128 B. Both rows do identical work and moved together, which points at the machine between two
  runs two days apart rather than at the library; the `Scoped` row, which does thirty times the work,
  moved by 4%.
- The 100-entry collection row: a mean of 4,994 ns against a median of 4,532 ns, with a standard
  deviation of 521 ns — one launch ran hot. The 10- and 1,000-entry rows on either side of it are
  within 4% and 1%.
- The one awaiting rule varies by ±232 ns between iterations, as it always has; its 10% is noise.
- The address-rule figures quoted above (152 → 96 ns, 130 → 42 ns) come from a separate run the same
  day on the same code, because those benchmarks did not exist when the previous run was taken.
