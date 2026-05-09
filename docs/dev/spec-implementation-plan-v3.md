# Spec Implementation Plan (v3)

> Supersedes [`spec-implementation-plan.md`](./spec-implementation-plan.md) (v2). The v2 plan accepted "we'll likely write more than [the CC floor] for edge cases beyond branch coverage," which broke the original intent: a strict 1:1 mapping between cyclomatic complexity and test count, intended as a verifiable progress metric for an AI assistant to grade itself against. v3 restates that contract and lays out what it takes to implement it.

## The contract

**For every method in [`cyclomatic-complexity-report.md`](./cyclomatic-complexity-report.md), the test count equals that method's CC score. No more, no less.**

- A `[Theory]` with N `[InlineData]` rows counts as N test cases.
- A `[Fact]` counts as 1 test case.
- Each test corresponds to one linearly independent path through the method.
- Tests are named `<Method>_<Scenario>_<Expected>` so the per-method count is grep-able.

This is mechanically checkable. For any method `M` in class `C` with CC score `n`, scope to the matching test file (`CTests.cs`) and count tests for that method:

```sh
grep -cE "public.*\b${M}_" Bookshelf.Tests/path/to/CTests.cs
# expected: n (counts every [Fact] / [Theory] method named ${M}_*)
```

The test file is the scoping mechanism — tests for class `C` live only in `CTests.cs`, so file-scoped grep gives per-method counts without needing the class name in every test name.

The global sum across PR 1's 35 methods is 128. That number is the ceiling and the floor — it cannot be exceeded by extra boundary cases, and it cannot be undershot by bundled assertions.

## Why this is the contract

The point of the code-quality work (parent: issue #3) is to give an AI assistant metrics it can use to know whether it is improving the codebase. Cyclomatic complexity is the foundational metric: it counts decisions, which gives the floor of independent paths needing coverage. If "tests written" maps 1:1 to "CC paths," then:

- An AI can count tests and check progress against a fixed target.
- An AI cannot game the metric by writing extra assertions that don't correspond to real paths.
- An AI cannot under-deliver by bundling multiple paths into one test with shared assertions.

Without 1:1, "test count" is a soft signal and the metric loses its verification power. v2's overage acceptance softened that signal.

## What 1:1 requires

### 1. Private helpers must be testable directly

CC counts every method individually, including private helpers. To honor 1:1, every method must have its own tests — including helpers that today are tested only through their public callers.

The mechanism is already in place: `[assembly: InternalsVisibleTo("Bookshelf.Tests")]` is wired in `Bookshelf/Properties/AssemblyInfo.cs`, and three statics on `ImageUpload` already use it. Extending the pattern: every private method with CC > 1 in the report gets promoted to `internal`. Estimated scope: ~10 methods across PR 1.

### 2. Test naming maps to the production method

Convention: `<Method>_<Scenario>_<Expected>` (same as v2). Examples:

- `DeleteAsync_InvalidPath_IsNoOp`
- `ResolveExtension_FilenameHasExtension_PreservesIt`
- `GetResizedAsync_CacheMiss_PersistsToDisk`

This is the standard xUnit / Microsoft test-naming convention. Tests for `LocalFileStorage` live in `LocalFileStorageTests.cs`; tests for `ImageUpload` live in `ImageUploadInstanceTests.cs` / `ImageUploadStaticTests.cs`. The test file scopes tests to one production class, so the production class name does not need to appear inside each test method name. Same-named methods on different classes (e.g., `SaveAsync` on both `LocalFileStorage` and `ImageUpload`) stay disambiguated by their test files.

### 3. One assertion stance per path

Each test exercises one path. Assertions inside the test verify the contract for that path — return value, side effect, thrown exception. No bundling multiple paths into a single test via multiple assertions.

Strictness within a test: assert on actual return values, what landed in the DB, what got written to disk. Loose-stance assertions (just check `IActionResult` type) make it harder to know whether the path was actually exercised correctly.

## Per-test-class targets for PR 1

Pulled from `cyclomatic-complexity-report.md`. The total is the test count target for that file.

| Test file | Methods | CC sum |
|---|---|---:|
| `FormFileTagHelperTests` | Process(2), BuildPreviewContainer(2), BuildFileInput(2), BuildHiddenInput(2), BuildHint(2) | 10 |
| `FormSelectTagHelperTests` | GenerateInput(3), ShouldSelectPlaceholder(3) | 6 |
| `FormTextTagHelperTests` | IsNumericType(8), ResolveInputType(3) | 11 |
| `ImageUploadTagHelperTests` | Process(4), BuildSource(5) | 9 |
| `SortableColumnTagHelperTests` | Process(11) | 11 |
| `UploadStoragePathsTests` | .ctor(2), NormalizeStoredPath(7), ResolveUploadAbsolutePath(2), BuildCachePath(5), EnumerateCacheVariantPaths(5), NormalizeUploadsPath(3) | 24 |
| `ImageUploadStaticTests` | IsValidDimension(3), ResolveFormat(3), GetContentTypeFromPath(2) | 8 |
| `LocalFileStorageTests` | SaveAsync(2), GetAsync(2), DeleteAsync(5), GetUrl(2), ResolveExtension(2) | 13 |
| `ImageSharpImageProcessorTests` | ResizeAsync(4), NormalizeFormat(2) | 6 |
| `OrphanedUploadCleanupJobTests` | RunAsync(5), ResolveGracePeriod(2) | 7 |
| `ImageUploadInstanceTests` | SaveAsync(4), GetAsync(7), GetOriginalAsync(2), GetResizedAsync(7) | 20 |
| `HangfireDashboardAuthorizationFilterTests` | Authorize(3) | 3 |
| **Total** | **35 methods** | **128** |

## What changes from the existing branch

The current branch (`task/issue-7-write-specs`) has 185 test cases distributed unevenly:

- Some files are over their CC target (`ImageUploadStaticTests` 30 vs 8 — heavy `[Theory]` boundary inflation).
- Some files are under (`LocalFileStorageTests` 12 vs 13 — `ResolveExtension` has no direct tests; its branches are covered through `SaveAsync` tests).

The 185 tests are a useful **content** starting point — assertions, fixtures, builders, scaffolding, and most of the scenario coverage is already written. v3 restructures them rather than rewriting from scratch:

1. Promote remaining private helpers to `internal`.
2. Split or merge tests so each production method has exactly cc tests.
3. Rename tests to `<Method>_<Scenario>_<Expected>` form where they don't already follow it.
4. Add direct tests for previously-untested helpers (their branches are already exercised, but not under their own name).
5. Drop redundant `[InlineData]` rows that test the same path with different boundary values — keep one row per path; if a boundary case adds value, put it in a different test for a different path.

End state: 128 test cases, organized so each maps to one CC-counted path, each named for the method it tests.

## Implementation order

Smallest file first, to validate the pattern before applying broadly:

1. `HangfireDashboardAuthorizationFilterTests` (3 tests)
2. `ImageUploadStaticTests` (8 tests)
3. `ImageSharpImageProcessorTests` (6 tests)
4. `OrphanedUploadCleanupJobTests` (7 tests)
5. `FormSelectTagHelperTests` (6 tests)
6. `ImageUploadTagHelperTests` (9 tests)
7. `FormFileTagHelperTests` (10 tests)
8. `FormTextTagHelperTests` (11 tests)
9. `SortableColumnTagHelperTests` (11 tests)
10. `LocalFileStorageTests` (13 tests)
11. `ImageUploadInstanceTests` (20 tests)
12. `UploadStoragePathsTests` (24 tests)

After (1), pause for review of the resulting pattern before continuing.

## What this plan does not cover

- **Coverage percentage.** v3 specifies *test count*, not branch coverage. Coverlet should still be run alongside; if 128 tests achieve 100% branch coverage, the contract is fully realized. If they do not, that's a finding — either the test count target needs to be revisited, or the missing branches indicate a path the report didn't account for.
- **PRs 2–4.** The parent plan's split into 4 PRs (skeleton/unit, repos, controllers, integration) still applies. v3 redefines the test-counting rule; it doesn't redefine the PR boundaries. Repos, controllers, and integration tests will use the same 1:1 contract, applied to their respective methods.
- **Razor views.** CC doesn't measure them. Coverage of Razor templates is tracked separately via `WebApplicationFactory` integration tests (PR 4).

## Open issue: production-code visibility changes are out of scope

**Status: blocking. To be revisited before continuing the refactor.**

As written above, v3 requires promoting ~10 private helpers in `Bookshelf/` (production code) from `private` to `internal` (or `protected internal` for `override` members) so the test assembly can call them directly. This is what Option A demands and what makes strict 1:1 with the per-method CC report achievable.

The user's goal for this work was strictly to rewrite specs — *not* to modify production code. The visibility flips, while purely metadata changes (no logic touched), still constitute production-code edits. They were implied by the Option A choice but were not made prominent in the plan, and the scale (10 files, ~14 method edits, plus one collateral subclass change in `FormCheckboxTagHelper` to keep the base-class abstract signature consistent) was not surfaced before the refactor began.

**What this means for the contract:**

The strict per-method 1:1 (test count == cc score for every method including private helpers) is only achievable if private methods are reachable from the test assembly. Without visibility changes, private methods cannot be tested directly — full stop. So if production code is off-limits, strict per-method 1:1 is also off-limits, and we need a different shape of metric.

**Two paths to evaluate when revisiting:**

1. **Restrict the metric to public methods only.** Rebuild the CC count using only public/protected (externally testable) members. The total CC sum drops below 128 — likely ~80–90 for PR 1 scope. Tests are written against that smaller number. Private helpers contribute zero to the target and are covered organically through their public callers. No production code changes. This is the cleanest design-wise.

2. **Keep the CC sum at 128 but allocate it to public test surfaces.** Each public method's "test budget" equals the CC of itself plus all the private helpers it transitively calls. Tests still live at the public method level. The per-test-file count equals the CC sum of every method in that class (public + private) because the public surface exercises all of them. No production code changes. Test count contract becomes per-class instead of per-method.

Either of these rolls back the 10 production files already changed in this branch. The test files written under v3 that touch newly-internal members (e.g., `LocalFileStorageTests.ResolveExtension_*`, `FormSelectTagHelperTests.ShouldSelectPlaceholder_*`, `FormTextTagHelperTests.IsNumericType_*`, `ImageUploadTagHelperTests.BuildSource_*`, `ImageSharpImageProcessorTests.NormalizeFormat_*`, `OrphanedUploadCleanupJobTests.ResolveGracePeriod_*`, `FormFileTagHelperTests.BuildPreviewContainer_*`/`BuildFileInput_*`/`BuildHiddenInput_*`/`BuildHint_*`) would need to be rewritten to test those behaviors via their public callers instead.

**Decision needed before continuing.**
