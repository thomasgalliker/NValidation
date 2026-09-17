# Previous run against this run

The figures of the run taken before the architecture review, set against the run taken after it, for
every NValidation row the benchmark project measures. Both runs: Apple M4 Pro, .NET 10, BenchmarkDotNet
`--job medium`, nothing else running. The previous run's standard deviations were not kept, so the
significance test uses this run's: a change counts when it is past 5% and past three standard deviations.
Allocation figures are exact, so every byte shown as changed did change.

Ratios matter more than the absolute numbers; take both runs again on the same machine before trusting a
difference smaller than the noise column.


## `ValidationBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| SingleObject | 54.0 ns | 53.1 ns | 0.6 ns | -2% (within noise) | 56 B | 64 B |
| WholePayload | 384.0 ns | 238.2 ns | 2.4 ns | -38% (faster) | 480 B | 504 B |
| WholePayloadWithFailures | 930.0 ns | 500.3 ns | 2.3 ns | -46% (faster) | 2,784 B | 2,336 B |
| WithAnAwaitingRule | 1,379.0 ns | 1,305.4 ns | 84.0 ns | -5% (within noise) | 790 B | 815 B |

## `ChainCostBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 0 chains | 9.5 ns | 8.8 ns | 0.0 ns | -7% (faster) | 56 B | 64 B |
| 1 chain | 16.0 ns | 17.9 ns | 0.1 ns | +12% (slower) | 56 B | 64 B |
| 2 chains | 19.0 ns | 20.9 ns | 0.2 ns | +10% (slower) | 56 B | 64 B |
| 4 chains | 24.2 ns | 25.8 ns | 0.1 ns | +7% (slower) | 56 B | 64 B |
| 8 chains | 36.4 ns | 36.5 ns | 0.1 ns | +0% (within noise) | 56 B | 64 B |
| 16 chains | 59.1 ns | 57.7 ns | 0.6 ns | -2% (within noise) | 56 B | 64 B |

## `CollectionValidationBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 0 entries | 148.0 ns | 17.9 ns | 0.1 ns | -88% (faster) | 192 B | 64 B |
| 10 entries | 761.0 ns | 512.1 ns | 13.7 ns | -33% (faster) | 1,448 B | 1,472 B |
| 100 entries | 6,441.0 ns | 4,572.7 ns | 37.5 ns | -29% (faster) | 11,528 B | 12,992 B |
| 1,000 entries | 61.7 µs | 44.1 µs | 452.5 ns | -29% (faster) | 112,328 B | 128,192 B |

## `ComparisonBenchmark (NValidation rows)`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 4 chains, valid | 51.3 ns | 52.2 ns | 0.4 ns | +2% (within noise) | 56 B | 64 B |
| 4 chains, invalid | 1,078.9 ns | 386.3 ns | 1.6 ns | -64% (faster) | 4,240 B | 2,256 B |

## `PayloadComparisonBenchmark (NValidation rows)`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| nested payload, valid | 376.8 ns | 241.3 ns | 3.1 ns | -36% (faster) | 480 B | 504 B |
| nested payload, invalid | 941.9 ns | 501.0 ns | 7.7 ns | -47% (faster) | 2,784 B | 2,336 B |

## `WideComparisonBenchmark (NValidation rows)`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 34 chains, valid | 232.3 ns | 243.5 ns | 23.7 ns | +5% (within noise) | 56 B | 64 B |
| 34 chains, invalid | 1,775.4 ns | 689.3 ns | 3.6 ns | -61% (faster) | 6,456 B | 3,192 B |

## `ValidatorResolutionBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| Scoped | 8,979.0 ns | 9,135.1 ns | 168.1 ns | +2% (within noise) | 25,536 B | 26,400 B |
| Singleton | 25.6 ns | 22.8 ns | 0.1 ns | -11% (faster) | 128 B | 128 B |
| ScopedWithSafePromotion | 23.7 ns | 24.2 ns | 0.7 ns | +2% (within noise) | 128 B | 128 B |

## What moved the numbers

- **Composed validators stay on the synchronous path.** `SetValidator` and `ForEach` used to register an
  awaiting check, so any validator with a nested object or a collection ran every loop through the async
  twins even when nothing inside could suspend. They now ask the composed validator once, when the rule is
  declared, and pick the synchronous twin when it judges rather than awaits. This is the whole of the
  `WholePayload` gain and most of the per-entry collection gain.
- **`ForEach` no longer boxes the rule context.** The composed rule passed a method group of the struct
  context as a delegate, which boxed the context and allocated the delegate on every validation: 136 bytes
  and about 130 ns whether or not the collection had entries. It now hands the frame itself to the element
  builder, which is why an empty collection fell from 192 B to the 64 B floor.
- **Failures cost less to report.** `AddError` takes its arguments as a `params ReadOnlySpan`, the
  placeholder formatter scans the template by hand instead of running a regular expression with a closure
  per placeholder, and a rule inside a `ForEach` enriches its arguments once rather than copying the
  dictionary twice.
- **Every pass carries 8 bytes more.** The settings a validator resolves — its provider and its two
  behavior axes — now travel down to the validators it composes, together with the cancellation token,
  inside the run the frame holds. That is the 56 B → 64 B per pass, and the 16 B more per collection entry
  (two frames an entry). It is the only allocation figure that went up on the validation path.
- **The scoped resolution path allocates about 860 B more per graph.** A validator the container builds
  is now handed one `NValidationOptions` record instead of two references. It is paid only where a host
  names a scoped lifetime; the promoted path is unchanged at 128 B.

## What to distrust

- The wide valid row (34 chains) is the noisiest number in either run: a standard deviation of 24 ns on a
  mean of 244 ns, with a median of 225 ns. `ChainCostBenchmark`, which measures the same thing with a
  tighter error bar, shows the per-chain term unchanged.
- The one awaiting rule row varies by ±84 ns between iterations; its 5% difference is noise.
- A regression appeared and was fixed between the two runs: growing the rule context by two references
  pushed its construction out of the JIT's inlining budget and cost 6 ns on the first chain. The context
  now holds references to its rule and its check instead of copying five fields out of them, and its
  construction is force-inlined. The chain-cost table above is the run after that fix.
- `ScopedWithSafePromotion` briefly measured the same as `Scoped` while the promotion rule was being
  simplified; the rule was put back and the row above is the corrected run. This benchmark is the guard
  for that setting, and it did its job.
