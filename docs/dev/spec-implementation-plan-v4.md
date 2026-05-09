# Spec Implementation Plan (v4)

> Supersedes [`spec-implementation-plan-v3.md`](./spec-implementation-plan-v3.md). v3 interpreted "1:1 with CC" as a per-method requirement (one `[Fact]` named for each method, including private helpers), which forced production-code visibility flips so the test assembly could call private members directly. That overshot the actual goal. v4 corrects to a per-file count contract that needs no production-code changes — tests live at the public-method level, and private helpers are exercised through their public callers.

## The contract

Two checks, both mechanically verifiable:

1. **Per-file count match.** Each test file's `[Fact]` / `[InlineData]` case count equals the CC sum of all methods in the corresponding production class (public + private). Across PR 1's 12 test files, the totals sum to 128.

2. **Branch coverage.** Coverlet reports 100% branch coverage on every production file in PR 1's scope.

The count check is cheap (no execution): sum CC scores from the report, count `[Fact]` / `[InlineData]` cases per file, compare. The coverage check runs the test suite and reads Coverlet's output. Together they verify that the right *number* of tests exists *and* that those tests actually exercise every CC-counted branch.

## What v4 leaves alone

- **Production code is not modified.** Private members stay private, `protected` members stay `protected`. Tests use whatever access the production code already grants (public, or internal where it was made internal in earlier work that pre-dates this plan). The test project does not get special access via visibility flips.
- **Tests are not pinned to a per-method count.** A class's CC sum can be distributed across the file's tests however makes sense; what matters is the file's total.
- **Existing test names are kept as-is** when they already follow `<Method>_<Scenario>_<Expected>`. That convention is fine.

## Why this is the right contract

The original goal: use cyclomatic complexity as a verifiable progress metric for an AI assistant. The metric needs to be:

- Mechanical (no human judgment to check).
- Aligned with what CC measures (decision points / branches in production code).
- Hard to game with garbage tests.
- Achievable without distorting the production codebase to fit testing requirements.

Per-file count + 100% branch coverage hits all four. Per-method count (v3) hit the first three but failed the fourth — it required ~14 method-level visibility changes across 10 production files just so the tests could reach internal helpers directly. That's the production codebase being shaped by testing concerns, which is the wrong direction.

## Per-test-class targets for PR 1

Same as v3 — the per-class CC sums don't change between v3 and v4, only the rule for distributing them inside a file does.

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

Note: `ImageUploadStaticTests` covers methods that are already `internal` in the original branch (independent of v3). Those remain testable directly. They were exposed for testing reasons in an earlier commit, accepted at the time, and v4 doesn't ask to reverse that — it just doesn't add new examples of the same pattern.

## Test naming

`<Method>_<Scenario>_<Expected>` (xUnit convention). Tests for `LocalFileStorage` live in `LocalFileStorageTests.cs`; the test class name disambiguates the production class, so the production class name is not repeated inside each test method name. Same-named methods on different classes (e.g., `SaveAsync` on both `LocalFileStorage` and `ImageUpload`) stay separate by living in different test files.

## What changes from the current branch state

The branch currently has v3-era changes that need to be undone to land at v4:

**Production files to revert** (visibility flips back to original):

1. `Bookshelf/Services/ImageSharpImageProcessor.cs` — `NormalizeFormat` back to `private`
2. `Bookshelf/Services/OrphanedUploadCleanupJob.cs` — `ResolveGracePeriod` back to `private`
3. `Bookshelf/TagHelpers/FormSelectTagHelper.cs` — `GenerateInput` back to `protected override`, `ShouldSelectPlaceholder` back to `private`
4. `Bookshelf/TagHelpers/FormTagHelperBase.cs` — abstract `GenerateInput` back to `protected abstract`
5. `Bookshelf/TagHelpers/FormCheckboxTagHelper.cs` — `GenerateInput` back to `protected override` (this was a forced collateral change to keep the base-class signature consistent)
6. `Bookshelf/TagHelpers/FormTextTagHelper.cs` — `GenerateInput`, `ResolveInputType`, `IsNumericType` back to original visibility
7. `Bookshelf/TagHelpers/FormFileTagHelper.cs` — `BuildPreviewContainer`, `BuildFileInput`, `BuildHiddenInput`, `BuildHint`, `GenerateInput` back to original visibility
8. `Bookshelf/TagHelpers/ImageUploadTagHelper.cs` — `BuildSource` back to `private`
9. `Bookshelf/Services/LocalFileStorage.cs` — `ResolveExtension` back to `private static`
10. `Bookshelf/Models/ImageUpload.cs` — `GetOriginalAsync`, `GetResizedAsync` back to `private` (the three statics — `IsValidDimension`, `ResolveFormat`, `GetContentTypeFromPath` — stay `internal`; that change predates v3)

**Test files to revisit**:

The 9 test files refactored under v3 currently call newly-internal members directly. They need to be rewritten to test those branches via the public method that calls them:

- `LocalFileStorageTests.cs` — `ResolveExtension_*` tests fold into `SaveAsync_*` tests
- `FormSelectTagHelperTests.cs` — `ShouldSelectPlaceholder_*` and `GenerateInput_*` tests merge into `Process_*` tests
- `FormTextTagHelperTests.cs` — `IsNumericType_*` and `ResolveInputType_*` tests fold into `Process_*` tests
- `ImageUploadTagHelperTests.cs` — `BuildSource_*` tests merge into `Process_*` tests
- `ImageSharpImageProcessorTests.cs` — `NormalizeFormat_*` test folds into `ResizeAsync_*` tests
- `OrphanedUploadCleanupJobTests.cs` — `ResolveGracePeriod_*` tests fold into `RunAsync_*` tests
- `FormFileTagHelperTests.cs` — `BuildPreviewContainer_*`, `BuildFileInput_*`, `BuildHiddenInput_*`, `BuildHint_*` tests merge into `Process_*` tests
- `SortableColumnTagHelperTests.cs` — already at `Process_*` only (single public method); just needs to stay at 11 cases
- `HangfireDashboardAuthorizationFilterTests.cs` — already at `Authorize_*` only; stays at 3 cases

The two test files I haven't touched (`ImageUploadInstanceTests` and `UploadStoragePathsTests`) need to be brought from their current case counts (18 and 32 respectively) to their v4 targets (20 and 24).

In all cases, the per-file *case count* must equal the table's CC sum. How tests are partitioned within the file is a structural choice as long as the count holds.

## Implementation order

Smallest file first, to validate the pattern before applying broadly. Order is the same as v3:

1. `HangfireDashboardAuthorizationFilterTests` (3 — already at target)
2. `ImageUploadStaticTests` (8 — already at target with v3 work, no rollback needed since the methods were already internal)
3. `ImageSharpImageProcessorTests` (6)
4. `OrphanedUploadCleanupJobTests` (7)
5. `FormSelectTagHelperTests` (6)
6. `ImageUploadTagHelperTests` (9)
7. `FormFileTagHelperTests` (10)
8. `FormTextTagHelperTests` (11)
9. `SortableColumnTagHelperTests` (11)
10. `LocalFileStorageTests` (13)
11. `ImageUploadInstanceTests` (20)
12. `UploadStoragePathsTests` (24)

After (1) and (2), pause for review of the resulting pattern before continuing.

## What this plan does not cover

- **Coverage report wiring.** This plan specifies that Coverlet is the verification half of the contract, but does not specify the runner invocation, output format, or how the 100% branch coverage assertion is enforced (manual check vs. CI gate). Coverlet is already wired into `Bookshelf.Tests.csproj` as `coverlet.collector`; running `dotnet test --collect:"XPlat Code Coverage"` produces the report. CI integration is deferred.
- **PRs 2–4.** The parent plan's split into 4 PRs (skeleton/unit, repos, controllers, integration) still applies. v4 redefines the test-counting rule; it doesn't redefine the PR boundaries. Repos, controllers, and integration tests will use the same per-file 1:1 contract, applied to their respective methods.
- **Razor views.** CC doesn't measure them. Coverage of Razor templates is tracked separately via `WebApplicationFactory` integration tests (PR 4).

## What v4 fixes vs. earlier versions

- **v2** said: "we'll likely write more than [the CC floor] for edge cases." That softened the metric without flagging the trade-off; the implementation drifted to 185 cases against a 128 target.
- **v3** said: per-method 1:1, including private helpers, requiring visibility flips on ~10 production files. That was a stricter version of the metric than the user wanted, and it pulled production-code changes into a specs-only refactor.
- **v4** says: per-file count + 100% branch coverage. No production-code changes. Tests live at the public-method level. The metric is mechanically checkable, the coverage check is mechanically verifiable, and neither requires the production codebase to be reshaped.
