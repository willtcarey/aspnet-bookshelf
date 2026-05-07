# Spec Implementation Plan (v2)

> Planning notes for Step 3 of the code-quality plan (parent: issue #3). Translates the per-method complexity scores from [`cyclomatic-complexity-report.md`](./cyclomatic-complexity-report.md) into a categorized testing plan with PR-sized work units. This revision incorporates review feedback from 2026-05-06.

## Test count floor

Cyclomatic complexity = the minimum number of test cases needed to cover every path through a method. The complexity report flags 73 methods. One of those — `Program.cs <Main>$` (cc=5) — is startup wiring and is covered implicitly by integration tests rather than directly.

That leaves **72 methods actively tested, with a floor of 249 test cases**. We'll likely write more than that for edge cases beyond branch coverage, but never fewer if we want full path coverage.

## The 5 categories

The 72 actively-tested methods fall into 5 natural groups. Each has a "right" testing style in .NET — they aren't all the same kind of test.

### 1. Pure-logic helpers — 21 methods, 78 test cases

**Members**: TagHelpers (`FormFile`, `FormSelect`, `FormText`, `ImageUpload`, `SortableColumn`), all of `UploadStoragePaths.*`, the `ImageUpload` static helpers (`IsValidDimension`, `ResolveFormat`, `GetContentTypeFromPath`).

**Why grouped**: no dependencies, no DB, no filesystem, no DI container.

**Test style**: pure unit tests with xUnit `[Fact]` / `[Theory]`.

**Tooling**: xUnit only. No mocking needed.

**Why start here**: cheapest tests in the codebase, but the cyclomatic counts are real (`SortableColumnTagHelper.Process` at 11, `FormTextTagHelper.IsNumericType` at 8). Highest ROI for proving the test infrastructure.

### 2. Services with I/O — 14 methods, 50 test cases

**Members**: `LocalFileStorage.*`, `ImageSharpImageProcessor.*`, `OrphanedUploadCleanupJob.*`, the `ImageUpload` instance methods (`SaveAsync`, `GetAsync`, `GetOriginalAsync`, `GetResizedAsync`), and `HangfireDashboardAuthorizationFilter.Authorize` (reads `HttpContext` + env vars, so it's not pure).

**Why grouped**: touch the filesystem, do image processing, or read ambient request/env state — but not the database.

**Test style**: unit tests, but the filesystem needs to be faked or sandboxed. Easiest path is a temp dir per test (`Path.GetTempFileName()` / `Path.GetTempPath()` + cleanup) rather than mocking. ImageSharp can run on real `byte[]` inputs in-process. `HangfireDashboardAuthorizationFilter` gets a stubbed `HttpContext` via `DefaultHttpContext`.

**Tooling**: xUnit + temp dirs. No mocking library required for the simple cases.

### 3. Repository tests — 12 methods, 28 test cases

**Members**: `BookRepository.*` (7 methods), `AuthorRepository.*` (5 methods).

**Why grouped**: talk to EF Core / database.

**Test style**: still unit tests in xUnit terms, but the DbContext is real — pointed at an in-memory provider instead of Postgres. Each test gets a fresh DbContext.

**Tooling**: `Microsoft.EntityFrameworkCore.InMemory` package.

**Cascade-delete carve-out**: the in-memory provider doesn't enforce relational constraints (no real cascades, no real FK checks). The 1–2 tests that exercise `Author → Book` cascade-delete behavior need to run against a real Postgres in a testcontainer. The rest stay in-memory. This keeps the testcontainer scope small enough to not gate the whole category.

### 4. Controller tests — 24 methods, ~95 test cases written

**Members**: every controller action across `BooksController`, `AuthorsController`, `AccountController`, `HomeController`, `ImagesController`, `Areas/Admin/Controllers/AdminCrudController`, `Areas/Admin/Controllers/UsersController`.

**Two sub-styles, depending on the controller**:

- **Mocked unit-style** (default for ~18 of the 24 methods): mock the repository dependencies with **Moq**, instantiate the controller directly, call the action, assert on the returned `IActionResult`.
- **Integration-style by default** (~6 methods — `AccountController.Register`, `AccountController.Login`, and all of `UsersController.*`): these depend on `UserManager<>` / `SignInManager<>` / `RoleManager<>`, which are painful and brittle to mock. They use `WebApplicationFactory<Program>` with a test Identity store from the start.

**Plus a 20% integration overlap** on the 18 mocked methods: golden-path integration tests on top of the mocked unit tests, to catch wiring issues that mocks hide.

**Why ~95 cases written for a 79-case floor**: 79 is the path-coverage floor. The integration overlap (~20% of the mocked-style methods) adds roughly 16 more written tests, bringing the actual written count to ~95.

### 5. `ApplicationDbContext.SaveChangesAsync` (complexity 14) — 1 method, 14 test cases

**Why singled out**: orchestrates EF change-tracking AND filesystem cleanup. Mocking it away would defeat the point.

**Test style**: integration test. Real (test) Postgres + real filesystem in a temp dir. 14 test cases covering its branches.

## Coverage target

**100% line coverage and 100% branch coverage, no exclusions.**

This project is a sandbox: the point is to learn whether these metrics are realistically achievable on a working .NET app before applying them to the asset-calc project. So we don't carve out the awkward bits. Anything we end up unable to cover is a documented research finding, not a silent exclusion in `coverlet.runsettings`.

Expected pressure points (worth flagging up front so we can compare reality to expectation):

- **Razor views** are the most likely limit. Coverlet's reporting on `.cshtml` files is historically uneven — view code compiles to C# but line/branch mapping back to the view source isn't always clean. We'll cover them via `WebApplicationFactory` (every page rendered at least once via integration tests) and report what the tooling actually says about them.
- **`Program.cs <Main>$`** (complexity 5) — startup wiring. Booted by `WebApplicationFactory`, so 100% should be real, not faked.
- **EF migrations** — auto-generated. Exercised by running them in test setup. Trivially 100%, listed for completeness.

## What the complexity report doesn't flag (but still gets tested)

- **ViewModel validation** — no methods complex enough to flag, but the `[Required]` / `[Display]` / etc. attributes get tested via `Validator.TryValidateObject`. Cheap, mechanical, catches whole classes of bugs, and required for branch coverage on the validation paths.
- **Razor views** — see above. Covered via `WebApplicationFactory` integration tests rather than direct testing.

## Tooling

The full stack for a new `Bookshelf.Tests` xUnit project:

- `xunit` — test framework
- `xunit.runner.visualstudio` — VS / `dotnet test` runner
- `Moq` — mocking library, for controller tests
- `Microsoft.EntityFrameworkCore.InMemory` — in-memory DB provider, for the bulk of repository tests
- `Microsoft.AspNetCore.Mvc.Testing` — `WebApplicationFactory<TEntryPoint>` for integration tests
- `Testcontainers.PostgreSql` — real Postgres for cascade-delete tests and `SaveChangesAsync`
- `coverlet.collector` — coverage collector, integrates with `dotnet test`
- *(optional)* `FluentAssertions` — assertion readability

## Test infrastructure conventions (set in PR 1a)

- **Test data builders**: an ObjectMother / builder pattern for `Book` and `Author` lives in `Bookshelf.Tests/Builders/`. With ~107 tests across repos and controllers all needing `Book`/`Author` instances, hand-rolling construction inline blows up the line count and makes refactors painful. Builders go in early so every later PR uses them.
- **Test invocation**: tests run via `docker compose run --rm --no-deps -w /app web dotnet test` to match how the rest of the build runs. A `make test` target (or equivalent script) wraps this so contributors don't have to memorize the invocation. CI is not yet established — when it lands, it calls the same docker-compose invocation.
- **Naming convention**: `Method_Scenario_Expected` (e.g., `SaveAsync_WhenFileIsTooLarge_Throws`). Picked because it makes test failures easy to scan.
- **When a test surfaces a bug**: small, obvious fixes land in the same PR with a note in the commit message. Anything bigger gets pulled out into a separate issue and the failing test is marked `[Fact(Skip = "tracking issue #N")]` so the PR doesn't stall.

## Assertion strictness

Default is **moderately loose**: assertions should match what the test name promises. A test named `SaveAsync_WhenFileIsTooLarge_Throws` asserts the throw and nothing else. If you want to also verify a side effect, that's a second test with its own name. One assertion per "logical thing," not one per property.

**Stricter in Categories 1, 3, 5** (pure logic, repos, `SaveChangesAsync`) — assert on actual return values, what landed in the DB, what got written to disk. These tests exist to verify exact behavior.

**Looser in Categories 2 and 4** (filesystem services, controllers) — assert on result type plus the one or two properties the test name promises. Don't recheck the whole model graph or every tempdata key. Strict controller assertions are the #1 source of "every harmless refactor breaks 50 tests."

This pairs cleanly with the 100% coverage goal: coverage measures whether a line ran, assertions measure whether the behavior was right. Loose assertions don't dent coverage — they just mean each test is narrower, which matches the one-test-per-branch shape the complexity floor already pushes us toward.

## PR sizing estimate

Numbers assume ~10–15 lines per test (Arrange / Act / Assert), ~20 for controller tests (mock setup is heavier), ~30 for integration tests (full HTTP pipeline + DB). Real numbers will vary ±30%.

| Category | Test files | Test methods | Approx lines |
|---|---|---|---|
| 1. Pure-logic helpers | 7 | 78 | ~700 |
| 2. Services with I/O | 5 | 50 | ~800 |
| 3. Repositories | 2 + fixture | 28 | ~550 |
| 4. Controllers | 7 | ~95 | ~2,000 |
| 5. `SaveChangesAsync` + integration | 3 + fixture | 14 + integration smoke | ~1,000 |

The lever on volume is **assertion strictness**. A test like "did `Edit` return a `RedirectToActionResult` with action='Index'?" is 5 lines. A test that *also* verifies the model passed to the view, temp-data flash messages, and repository call args is 25 lines. The complexity score gives the number of test cases; assertion strictness sets the lines per case.

## Recommended PR breakdown

The work ships as **4 PRs**, each with a unifying test-infrastructure theme so the reviewer can lock onto one pattern per PR.

### PR 1 — Test project skeleton + all non-DB, non-HTTP unit tests

**Theme**: "xUnit only, no DB, no HTTP." Boring and fast to review.

Stand up `Bookshelf.Tests.csproj`, install xUnit + Coverlet, add the `Book`/`Author` test data builders in `Bookshelf.Tests/Builders/`, document the docker-compose `dotnet test` invocation, add the `make test` wrapper, write one smoke test.

Then the unit tests for everything that doesn't touch a database or run through ASP.NET routing:

- **TagHelpers** — `FormFile`, `FormSelect`, `FormText`, `ImageUpload`, `SortableColumn`
- **`UploadStoragePaths`** — all path-computation methods
- **`ImageUpload` static helpers** — `IsValidDimension`, `ResolveFormat`, `GetContentTypeFromPath`
- **Filesystem services** — `LocalFileStorage.*`, `ImageUpload` instance methods (`SaveAsync`, `GetAsync`, `GetOriginalAsync`, `GetResizedAsync`) using temp directories
- **Image processing** — `ImageSharpImageProcessor.*` against real `byte[]` inputs in-process
- **Background job** — `OrphanedUploadCleanupJob.*`
- **Hangfire dashboard authz filter** — `HangfireDashboardAuthorizationFilter.Authorize` against a stubbed `DefaultHttpContext`

**Size**: ~1,750 lines, ~128 tests, ~12 test files plus builders and skeleton.

### PR 2 — Repository tests

**Theme**: "EF Core in-memory, with a tiny bit of real Postgres for cascades."

Tests for `BookRepository` (7 methods) and `AuthorRepository` (5 methods) against `Microsoft.EntityFrameworkCore.InMemory`. The 1–2 cascade-delete tests in `AuthorRepository` run against a real Postgres testcontainer because the in-memory provider doesn't enforce relational constraints. Keeps the testcontainer scope small and contained.

**Size**: ~600 lines, 28 tests, 2 test files plus a fixture.

### PR 3 — Mocked controller tests

**Theme**: "mock the repository, instantiate the controller, assert on the result." One pattern, repeats.

Tests for the controllers that don't depend on ASP.NET Identity:

- `BooksController`
- `AuthorsController`
- `HomeController`
- `ImagesController`
- `Areas/Admin/Controllers/AdminCrudController`

Repositories mocked with Moq, controller instantiated directly, action called, assertions on the returned `IActionResult` (type + the one or two key properties the test name promises — see assertion-strictness conventions above).

**Size**: ~1,400 lines, ~80 tests, 5 test files.

### PR 4 — Integration tests (identity controllers + `SaveChangesAsync`)

**Theme**: "all the integration-heavy stuff." `WebApplicationFactory<Program>` and Postgres testcontainer plumbing both ship here, since both groups of tests need them.

Sets up `WebApplicationFactory<Program>` with a test Identity store and a Postgres testcontainer fixture, then writes:

- **Identity-dependent controllers** — `AccountController` (Register, Login) and `Areas/Admin/Controllers/UsersController.*`. These use `UserManager`/`SignInManager`/`RoleManager`, which are painful and brittle to mock — integration tests are the right tool by default.
- **`ApplicationDbContext.SaveChangesAsync`** — 14 tests against real Postgres + a real filesystem temp dir, since this method orchestrates EF change-tracking AND filesystem cleanup and mocking it away would defeat the point.
- **Integration smoke tests** — ~16 golden-path integration tests on top of the mocked controller suites from PR 3, to catch wiring issues mocks hide.

**Size**: ~1,500 lines, ~45 tests, 3+ test files plus fixtures.

### Wing-it option

Per Jace's suggestion: open just PR 1 first and decide on the rest after seeing how it lands. PR 1 is ~1,750 lines / 128 tests, which is the largest of the four — if it gets through review cleanly, the rest will too. If it's too big, downsize the remaining PRs accordingly (split each in half, etc.).
