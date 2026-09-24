# Previous run against this run

The figures of the run taken on the first version of rule groups, set against the run taken after they
were reworked — additive and exclusive selection, data a call hands over, condition blocks, validation
limited to some properties — for every NValidation row the benchmark project measures. Both runs: Apple M4
Pro, .NET 10, BenchmarkDotNet `--job medium`, nothing else running, the same day. Both runs' standard
deviations were kept this time, and the larger of the two is the one tested: a change counts when it is
past 5% and past three standard deviations. Allocation figures are exact, so every byte shown as changed
did change — and on the validation path, none did. Rows marked new measure what did not exist before.

Ratios matter more than the absolute numbers; take both runs again on the same machine before trusting a
difference smaller than the noise column.


## `ValidationBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| SingleObject | 54.5 ns | 59.2 ns | 1.8 ns | +9% (within noise) | 72 B | 72 B |
| WithAnAwaitingRule | 1,325.9 ns | 2,188.7 ns | 562.6 ns | +65% (within noise) | 831 B | 831 B |
| WholePayload | 267.6 ns | 269.8 ns | 9.7 ns | +1% (within noise) | 528 B | 528 B |
| WholePayloadWithFailures | 503.8 ns | 546.5 ns | 23.6 ns | +8% (within noise) | 2,360 B | 2,360 B |

## `ChainCostBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 0 chains | 10.3 ns | 10.6 ns | 0.1 ns | +3% (within noise) | 72 B | 72 B |
| 1 chain | 18.7 ns | 17.6 ns | 0.1 ns | -6% (faster) | 72 B | 72 B |
| 2 chains | 20.7 ns | 20.2 ns | 0.1 ns | -2% (within noise) | 72 B | 72 B |
| 4 chains | 27.1 ns | 26.6 ns | 0.3 ns | -2% (within noise) | 72 B | 72 B |
| 8 chains | 38.8 ns | 38.9 ns | 0.6 ns | +0% (within noise) | 72 B | 72 B |
| 16 chains | 64.0 ns | 65.1 ns | 1.3 ns | +2% (within noise) | 72 B | 72 B |

## `FloorBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| Populated | 199.9 ns | 204.3 ns | 3.5 ns | +2% (within noise) | 72 B | 72 B |
| MostlyAbsent | 196.8 ns | 217.3 ns | 1.9 ns | +10% (slower) | 72 B | 72 B |

## `ConditionBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 0 conditions, on the chain | 18.7 ns | 18.1 ns | 0.2 ns | -3% (within noise) | 72 B | 72 B |
| 0 conditions, in blocks | new | 17.8 ns | 0.1 ns | — | — | 72 B |
| 1 condition, on the chain | 19.7 ns | 18.7 ns | 0.1 ns | -5% (within noise) | 72 B | 72 B |
| 1 condition, in blocks | new | 18.9 ns | 0.1 ns | — | — | 72 B |
| 3 conditions, on the chain | 21.9 ns | 20.0 ns | 0.1 ns | -9% (faster) | 72 B | 72 B |
| 3 conditions, in blocks | new | 19.7 ns | 0.2 ns | — | — | 72 B |

## `GroupBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| Ungrouped, 4 chains | 27.3 ns | 26.3 ns | 0.4 ns | -4% (within noise) | 72 B | 72 B |
| GroupedAndSelected, 4 chains | 34.6 ns | 35.5 ns | 0.3 ns | +3% (within noise) | 72 B | 72 B |
| GroupedAndSkipped, 4 chains | 13.9 ns | 10.9 ns | 0.4 ns | -21% (faster) | 72 B | 72 B |
| GroupedUnderAll, 4 chains | 40.1 ns | 27.2 ns | 0.2 ns | -32% (faster) | 72 B | 72 B |
| Exclusive, 4 chains | new | 35.0 ns | 0.2 ns | — | — | 72 B |
| ExclusiveOverMixed, 4 chains | new | 26.7 ns | 0.3 ns | — | — | 72 B |
| DefaultTagged, 4 chains | new | 26.3 ns | 0.5 ns | — | — | 72 B |
| Ungrouped, 16 chains | 63.4 ns | 63.1 ns | 0.9 ns | -0% (within noise) | 72 B | 72 B |
| GroupedAndSelected, 16 chains | 97.9 ns | 102.7 ns | 0.9 ns | +5% (within noise) | 72 B | 72 B |
| GroupedAndSkipped, 16 chains | 20.1 ns | 10.5 ns | 0.1 ns | -48% (faster) | 72 B | 72 B |
| GroupedUnderAll, 16 chains | 125.4 ns | 65.0 ns | 0.4 ns | -48% (faster) | 72 B | 72 B |
| Exclusive, 16 chains | new | 102.3 ns | 0.5 ns | — | — | 72 B |
| ExclusiveOverMixed, 16 chains | new | 62.9 ns | 4.1 ns | — | — | 72 B |
| DefaultTagged, 16 chains | new | 63.3 ns | 1.1 ns | — | — | 72 B |

## `CollectionValidationBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 0 entries | 18.2 ns | 17.4 ns | 0.3 ns | -4% (within noise) | 72 B | 72 B |
| 10 entries | 524.9 ns | 510.2 ns | 11.0 ns | -3% (within noise) | 1,640 B | 1,640 B |
| 100 entries | 4,618.2 ns | 4,705.4 ns | 165.0 ns | +2% (within noise) | 14,600 B | 14,600 B |
| 1,000 entries | 45.7 µs | 47.7 µs | 244.7 ns | +4% (within noise) | 144,200 B | 144,200 B |

## `ComparisonBenchmark` (NValidation rows)

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 4 chains, valid | 58.1 ns | 55.9 ns | 0.6 ns | -4% (within noise) | 72 B | 72 B |
| 4 chains, invalid | 340.7 ns | 341.8 ns | 1.5 ns | +0% (within noise) | 2,264 B | 2,264 B |

## `PayloadComparisonBenchmark` (NValidation rows)

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| nested payload, valid | 266.8 ns | 253.1 ns | 1.8 ns | -5% (within noise) | 528 B | 528 B |
| nested payload, invalid | 539.3 ns | 527.3 ns | 14.7 ns | -2% (within noise) | 2,360 B | 2,360 B |

## `WideComparisonBenchmark` (NValidation rows)

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| 34 chains, valid | 198.7 ns | 204.0 ns | 2.7 ns | +3% (within noise) | 72 B | 72 B |
| 34 chains, invalid | 656.0 ns | 685.0 ns | 11.9 ns | +4% (within noise) | 3,200 B | 3,200 B |

## `ValidatorResolutionBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| Scoped | 10.3 µs | 13.9 µs | 206.7 ns | +35% (slower) | 29,320 B | 37,136 B |
| Singleton | 22.0 ns | 25.2 ns | 1.3 ns | +15% (within noise) | 128 B | 128 B |
| ScopedWithSafePromotion | 25.4 ns | 25.3 ns | 1.2 ns | -1% (within noise) | 128 B | 128 B |

## `ReachThroughBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| DefaultGroup | new | 49.0 ns | 1.0 ns | — | — | 144 B |
| BothGroups | new | 91.1 ns | 0.6 ns | — | — | 144 B |
| NestedGroupAlone | new | 85.9 ns | 5.0 ns | — | — | 144 B |

## `ValidationDataBenchmark`

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| NoData | new | 23.5 ns | 1.7 ns | — | — | 72 B |
| DataReused | new | 28.4 ns | 0.6 ns | — | — | 72 B |
| DataFresh | new | 59.1 ns | 5.2 ns | — | — | 248 B |
| WhenAbsent | new | 25.0 ns | 0.3 ns | — | — | 72 B |
| WhenPresent | new | 48.0 ns | 2.4 ns | — | — | 72 B |

## `GroupSelectionComparisonBenchmark` (NValidation rows)

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| Create on top | new | 47.9 ns | 3.7 ns | — | — | 72 B |
| All | new | 28.2 ns | 0.1 ns | — | — | 72 B |
| default group | new | 20.0 ns | 0.2 ns | — | — | 72 B |
| Only Create | new | 34.1 ns | 1.2 ns | — | — | 72 B |

## `ValidationDataComparisonBenchmark` (NValidation rows)

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| options reused | new | 36.9 ns | 1.9 ns | — | — | 72 B |
| options per call | new | 68.4 ns | 1.0 ns | — | — | 248 B |

## `ConditionBlockComparisonBenchmark` (NValidation rows)

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| condition holds | new | 41.0 ns | 0.4 ns | — | — | 72 B |
| condition fails | new | 26.1 ns | 0.3 ns | — | — | 72 B |

## `PartialValidationComparisonBenchmark` (NValidation rows)

| Case | Time before | Time now | ± (StdDev now) | Change | Allocated before | Allocated now |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| two properties | new | 55.7 ns | 0.7 ns | — | — | 72 B |
| every property | new | 34.9 ns | 0.4 ns | — | — | 72 B |

## What moved the numbers

- **A call that names no group walks the default group alone.** The chains of the default group are frozen
  into an array of their own, so a validator whose every chain is grouped costs a call selecting none what
  a validator with no chain costs: 20.1 ns fell to 10.5 ns at sixteen grouped chains.
- **Selecting everything needs no gate.** `ValidationGroups.All` is walked by the loop without one, so
  sixteen grouped chains under it fell from 125.4 ns to 65.0 ns — what the same chains cost ungrouped.
- **The options are resolved once per call.** A pass used to walk the whole ladder — the call, the
  registration, `Default` — for itself; the call now resolves it once, and a pass lays its validator's own
  word over that. What a call without options resolves to is kept, and so are options handed over twice.
- **A composed validator keeps what it resolves.** Every entry of a collection brings its validator the
  same settings, so from the second on an entry reads what the first resolved. That is what holds the
  collection rows where they were while the selection, the data and the properties travel with every pass.
- **Nothing on the validation path allocates more.** 72 B a pass and 144 B a collection entry, as before:
  data and properties ride in the reference the selection already took.
- **Building a validator graph allocates 7.8 KB more**, nearly all of it the sample: `CarValidator` gained
  the five chains of its listing check. Built outside the container, the previous `CarValidator` allocates
  13.1 KB against the previous library and 13.4 KB against this one; the current one allocates 19.3 KB. The
  two promoted rows allocate the same 128 B they did.

## What to distrust

- `FloorBenchmark.MostlyAbsent`, +10%: a full run an hour earlier measured 199.2 ns (+1%), differing only
  in the element loop of `ForEach`, which the benchmark's validator does not have; a run of exactly this
  code with six launches measured 209 ns (+6%). Its `Populated` twin is within 2%.
- The awaiting rule: 2,189 ns ± 563 ns here, 1,066 ns ± 40 ns in that earlier run. What it measures is
  mostly the thread pool resuming it.
- `SingleObject`, +9%, measured +4% in the earlier run of the same code; `WholePayloadWithFailures`, +8%,
  measured -3% there, though its validator has a collection and so did change in between. Both are within
  three standard deviations.
- The singleton resolution row, +15%: the container returns an instance it already built, nothing of the
  library runs, and the promoted row doing the same work moved by -1%.
