# Path A Implementation Findings & Open Questions

> Captures the state of the strict per-method 1:1 implementation (Path A from issue #7's structure decision comment / `spec-implementation-plan-v3.md`) after running the suite with Coverlet. Documents what worked, what didn't, and the open questions that came out of the exercise — particularly whether the CC-to-test-count contract is the right metric to chase at all.

## Where we landed

- **12 test files refactored** to per-method 1:1 with the cyclomatic-complexity report.
- **10 production files** had `private` → `internal` (or `protected` → `protected internal`) visibility flips so the test assembly could reach helper methods directly.
- **128 tests total**, distributed exactly per the CC report's per-class sums:

| Test file | Tests | CC sum |
|---|---:|---:|
| HangfireDashboardAuthorizationFilterTests | 3 | 3 |
| ImageUploadStaticTests | 8 | 8 |
| ImageSharpImageProcessorTests | 6 | 6 |
| OrphanedUploadCleanupJobTests | 7 | 7 |
| FormSelectTagHelperTests | 6 | 6 |
| ImageUploadTagHelperTests | 9 | 9 |
| FormFileTagHelperTests | 10 | 10 |
| FormTextTagHelperTests | 11 | 11 |
| SortableColumnTagHelperTests | 11 | 11 |
| LocalFileStorageTests | 13 | 13 |
| ImageUploadInstanceTests | 20 | 20 |
| UploadStoragePathsTests | 24 | 24 |
| **Total** | **128** | **128** |

All 128 tests pass. The count contract is met exactly.

## The Coverlet result

After running `dotnet test --collect:"XPlat Code Coverage"` against the 128-test suite:

- **Overall branch coverage on PR 1 production files: ~88%** (not 100%).
- **6 of 12 production files have branch coverage gaps.**

The Path A contract was: count match *plus* Coverlet 100% branch coverage. Count is at 128/128. Coverage is not at 100%.

## The structural finding

Roslyn's cyclomatic complexity score and Coverlet's branch count don't agree on this codebase. CC undercounts branches in three specific code shapes that are common in the app:

1. **Switch expressions.** A `switch` with N arms scores cc=1 in Roslyn (or close to it). Coverlet tracks each arm as a separate branch. A 4-arm switch produces 8 branches in Coverlet (the switch decision + the 4 outcomes + a few null/safety branches), but cc=1 means the strict 1:1 contract only allows 1 test for that switch.

2. **Chained boolean operators (`||`, `&&`).** An `a || b || c` chain scores cc=3 in Roslyn. Coverlet tracks each short-circuit point separately, producing ~6 branch outcomes (a-true, a-false then b-true, a-false then b-false then c-true, ...).

3. **Null-conditional operator (`?.`).** Counts as one decision in CC. Produces two branches in IL (null vs not-null).

In code with lots of switches and short-circuit chains — which this app has — CC is structurally lower than branch count, sometimes by 4×.

## Per-gap breakdown

| Gap | Missed branch | CC count | Real branches | Closable within 1:1? |
|---|---|---:|---:|---|
| FormText.GenerateInput (line 0%) | Method body never invoked | n/a (cc=1, not in report) | 0 logic branches | YES — change `ResolveInputType` tests to call `Process()` instead; same logic gets exercised, plus `GenerateInput` body runs as a side effect |
| SortableColumn.Process (95%) | `value ?? ""` null-coalescing on query value | 11 | 12 | YES — modify one existing query-param test to include a `null` value |
| FormText.ResolveInputType (71%) | 4 of 6 switch arms unhit (url, phonenumber, password explicit, numeric-via-when) | 3 | 8 | NO — 6 switch arms, only 3 tests allowed |
| Hangfire.Authorize (75%) | `Identity == null` branch | 3 | 4 | NO — 4 distinct outcomes (Identity-null, !IsAuth, !IsAdmin, IsAdmin), 3 tests allowed |
| ImageSharp.NormalizeFormat (75%) | 2 of 4 switch arms unhit ("jpg", "png") | 2 | 8 | NO — 4 switch arms, 2 tests allowed |
| ImageSharp.ResolveEncoder (50%) | "jpg" arm and "png" arm | not in report (cc=1) | 4 | Partial — covered indirectly through `ResizeAsync` if you vary `format`, but `ResizeAsync` already used its 4-test budget on guards |
| ImageSharp.ResizeAsync (90%) | "image is smaller than target" branch (no-resize path) | 4 | 5+ | NO — 4 tests used on guards (null, neg width, neg height, happy resize); no budget left for "smaller image" |
| LocalFileStorage.ResolveExtension (30%) | 4 of 5 switch arms unhit | 2 | 8 | NO — 5 switch arms, 2 tests allowed |
| ImageUpload.GetResizedAsync (94%) | `finally` block where `File.Exists(temp)` is true (failure recovery path) | 7 | 8 | NO — would need to inject mid-write failure; 7 tests already used |

**Net:** 2 gaps closable for free, 1 partial, 6 mathematically blocked.

## What 100% branch coverage would actually cost

If we hold CC's per-method count as a floor and add tests until Coverlet hits 100%, the supplementary count is roughly:

| Method | Strict 1:1 tests | Tests for 100% branch | Delta |
|---|---:|---:|---:|
| FormText.ResolveInputType | 3 | 6 | +3 |
| Hangfire.Authorize | 3 | 4 | +1 |
| ImageSharp.NormalizeFormat | 2 | 4 | +2 |
| ImageSharp.ResolveEncoder | 0 (not in report) | ~3 | +3 |
| ImageSharp.ResizeAsync | 4 | 5 | +1 |
| LocalFileStorage.ResolveExtension | 2 | 5 | +3 |
| ImageUpload.GetResizedAsync | 7 | 8 | +1 |
| **Total extra** | — | — | **~14** |

Final count: ~142 tests for 100% branch coverage. The CC-count contract no longer matches; per-method numbers diverge from the report.

## The realization

The original framing was: *"use CC scores to determine how many tests we write, and Coverlet to verify those tests cover what they claim. The two checks together give the bot a metric to grade itself by."*

The exercise revealed that **CC scores and branch coverage are in tension** for this codebase. Hitting both at 100% is mathematically impossible without changing the production code (we already changed visibility — the only further changes would be refactoring switches into if-chains so CC counts each arm separately, which is shaping production code to fit a metric).

Strict 1:1 with CC was ultimately a fight against the way Roslyn measures complexity vs. the way Coverlet measures coverage. Reasonable people would expect those to align; in practice they don't, and the gap is bigger on switch-heavy code.

## Open questions to entertain

### 1. If a robot just needs *some* metric, is Coverlet alone enough?

Coverlet measures branch coverage. It runs tests, produces a percentage, identifies missed branches by line number. An LLM grading itself could:

- Write tests until coverage is at some target (e.g., ≥95%, =100%).
- Read Coverlet output to identify uncovered branches and target them.
- Verify pass/fail against a stable threshold.

This skips the CC-as-test-count layer entirely. Pros: Coverlet is what actually verifies coverage; CC is a planning aid that turned out to mismatch. Cons: loses the "this many tests for this many paths" planning anchor; an LLM could write 5 fat tests that hit 100% coverage with bundled assertions, which is hard to maintain.

### 2. Can Coverlet and CC be used together — and if so, how?

They can run alongside each other (and we did — the CC report exists, Coverlet runs). But they answer different questions and don't agree on counts:

- CC tells you "minimum tests to cover linearly independent paths" — a planning floor.
- Coverlet tells you "did each branch in IL get hit by some test" — a runtime fact.

Possible co-existence shapes:
- **CC for planning, Coverlet for verification.** Use CC to estimate how many tests a class roughly needs; use Coverlet's percentage as the actual contract. Drop the strict per-method 1:1 expectation. CC informs sizing; Coverlet verifies coverage.
- **CC as a floor, Coverlet as the ceiling.** "Tests >= CC sum AND branch coverage = 100%." Allows supplementary tests beyond CC count.
- **CC + Coverlet on different scopes.** CC for "how complex are our methods" (complexity reduction goal — issue #4 territory). Coverlet for "are our tests covering the code." They stop competing for the same metric slot.

### 3. Is there a version of the original goal that survives this?

The goal was: give a bot a verifiable progress metric. The metric needs to be:
- Mechanical (no human judgment).
- Not gameable by writing trivial tests.
- Aligned with what we actually want (correctness, coverage).

Coverlet branch coverage at 100% mostly hits all three. CC count match mostly hits the first two. CC count alone is gameable (write 128 garbage tests). Coverlet 100% is harder to game — you have to actually exercise the code.

If the goal is "a bot can audit its own work," Coverlet alone seems closer to what the goal needs than the strict CC-count contract. The CC report still has value as a complexity/refactoring tool (issue #4) but maybe shouldn't be the test-count metric.

## Possible next steps to consider

1. **Drop the CC-count contract.** Adopt Coverlet branch coverage as the sole verification metric. Target 100% (or a documented threshold) on PR 1 production files. CC report stays for complexity/refactoring purposes only.

2. **Soft-link CC and Coverlet.** Keep CC as a planning estimate (rough size of test suite) but don't enforce 1:1. Coverlet 100% is the actual contract.

3. **Stay with strict 1:1.** Accept that the suite has ~88% branch coverage as a documented finding. CC is the metric; coverage gaps are noted in this file. (This is the current state.)

4. **Roll back Path A entirely.** Revert the 10 production-file visibility flips. Adopt Path B (file-level 1:1, no production changes) plus Coverlet 100% as the contract. Tests live at the public-method level, helpers covered through callers.

5. **Hybrid.** Apply the 2 free fixes (FormText.GenerateInput, SortableColumn.Process) to bring coverage from ~88% to ~92%, document the rest as CC-vs-Coverlet structural mismatch, and decide between options 1–4 separately.

## Status

- Branch: `task/issue-7-write-specs`
- Tests: 128 passing, 0 failing
- Production code: 10 files have visibility flips from Path A
- Coverage: ~88% branch on PR 1 scope (full report at `Bookshelf.Tests/TestResults/.../coverage.cobertura.xml`)
- Plan docs: `spec-implementation-plan.md` (v2, deprecated), `spec-implementation-plan-v3.md` (Path A — what was implemented), `spec-implementation-plan-v4.md` (Path B alternative — not implemented)
- This file documents the open questions; no decision made yet on which path forward.
