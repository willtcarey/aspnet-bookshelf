# Code Quality Metrics Research

> Research document covering cyclomatic complexity, linting, and unit testing/code coverage for the Bookshelf ASP.NET app. Goal: understand what each concept is, how it works mechanically, what metrics it produces, and how it applies to this project.

---

## 1. Linting

### What it is

A linter is a static analysis tool that reads your source code **without running it** and flags problems. "Static analysis" means it analyzes the text/structure of the code itself, not its runtime behavior.

Think of it like a spell-checker and grammar-checker for code. It catches two categories of issues:

1. **Style/consistency issues** — naming conventions, indentation, unnecessary `using` statements, etc.
2. **Potential bugs** — unreachable code, unused variables, possible null reference errors, methods that are too long, etc.

### How it works mechanically

The linter parses your code into an AST (Abstract Syntax Tree) — a structured representation of the code. Then it walks through that tree checking a set of **rules**. Each rule looks for a specific pattern. For example:

- "Is there a variable declared but never used?" → warning
- "Is this method longer than 50 lines?" → warning
- "Does this class name start with a capital letter?" → enforced convention

Each rule has a **severity level**: `none` (disabled), `suggestion`, `warning`, or `error`. You configure which rules are active and at what severity.

### The output / metric

Linting is **not really a percentage metric** — it's more like a **count**. The output is a list of violations:

```
BookRepository.cs(45): warning CA1062: Validate parameter 'id' is non-null
BooksController.cs(78): suggestion IDE0059: Unnecessary assignment of a value
```

You can track **total number of lint warnings/errors** over time. The goal is zero errors and ideally zero warnings. It's a pass/fail gate: either the code is clean or it isn't.

### What it tells you

- **Code consistency** — is the team writing code in a uniform style?
- **Obvious mistakes** — did someone leave dead code, unused imports, or potential null dereferences?
- **Code hygiene** — it enforces discipline. Not glamorous, but prevents rot

### What it does NOT tell you

- Whether the code is **correct** (logic bugs pass linting just fine)
- Whether the code is **well-designed** (you can write perfectly linted spaghetti)
- Whether the code **works** (that's what testing is for)

### In .NET specifically

.NET has linting built into the compiler and SDK. The main tools are:

- **Roslyn Analyzers** — these ship with the .NET SDK. They include ~200+ rules covering code quality (CA rules) and code style (IDE rules). They run during `dotnet build`.
- **.editorconfig** — a configuration file where you set rule severities. Example: `dotnet_diagnostic.CA1062.severity = warning`
- **Directory.Build.props** — a project-level file where you can enable stricter analysis globally (e.g., `<EnableNETAnalyzers>true</EnableNETAnalyzers>`, `<AnalysisLevel>latest-all</AnalysisLevel>`)

There are also third-party analyzers (StyleCop, SonarAnalyzer, Roslynator) that add more rules, but the built-in ones are substantial.

### How it applies to this project

This project has 47 `.cs` files, ~1073 lines across controllers/services/repositories. Linting would catch things like:
- Unused `using` directives in any file
- Potential null reference issues (especially around navigation properties and `FindFirstValue`)
- Inconsistent naming
- Methods that could be simplified

**The metric you'd track: number of lint warnings at each severity level. Target: zero.**

### Decision: start with built-in Roslyn analyzers

The plan is to go with the **built-in Roslyn analyzers** first. They ship with the .NET SDK (no extra packages), cover 200+ rules, and are what the majority of .NET projects use. This is the standard practice — equivalent to RuboCop in Ruby. Configuration is just an `.editorconfig` file and a `Directory.Build.props` to turn up the strictness.

If the built-in analyzers aren't enough, the tier 2 options to consider are:

- **StyleCop.Analyzers** — adds stricter style/formatting rules (spacing, documentation comments, ordering of members in a class). Popular in enterprise shops that want very rigid consistency
- **Roslynator** — adds ~500 more community-driven rules and refactoring suggestions. Well-maintained, more of a "nice to have"
- **SonarAnalyzer** — from SonarSource (the SonarQube people). Adds security-focused rules and deeper complexity analysis. More relevant if you're doing a full SonarQube/SonarCloud pipeline

---

## 2. Cyclomatic Complexity

### What it is

Cyclomatic complexity is a **numeric score** that measures how many independent paths exist through a piece of code (usually a method/function). It was invented by Thomas McCabe in 1976.

In plain English: **how many different ways can execution flow through this method?**

### How it works mechanically

Start with a score of **1** (the straight-line path through the method). Then add **1 for every decision point**:

| Code construct | Adds to complexity |
|---|---|
| `if` | +1 |
| `else if` | +1 |
| `else` | +0 (it's the default path, already counted) |
| `&&` (in a condition) | +1 |
| `||` (in a condition) | +1 |
| `case` (in a switch) | +1 per case |
| `for` / `foreach` / `while` | +1 |
| `catch` | +1 |
| `?.` (null-conditional) | +1 |
| `??` (null-coalescing) | +1 |

#### Concrete example from this project

Here's a simplified version of what a controller action might look like:

```csharp
public async Task<IActionResult> Edit(int id, BookFormViewModel vm)
{
    if (!ModelState.IsValid)          // +1 → complexity = 2
        return View(vm);

    var book = await _repo.FindByIdAsync(id, CurrentUserId);

    if (book == null)                 // +1 → complexity = 3
        return NotFound();

    if (vm.CoverImage != null)        // +1 → complexity = 4
    {
        // process image
    }

    await _repo.UpdateAsync(book);
    return RedirectToAction("Index");
}
// Total cyclomatic complexity: 4
```

#### What the numbers mean

| Score | Interpretation |
|---|---|
| **1–5** | Simple, low risk. Easy to understand and test |
| **6–10** | Moderate complexity. Still manageable but worth watching |
| **11–20** | Complex. Hard to understand, hard to test thoroughly, higher bug risk |
| **21+** | Very complex. Refactoring strongly recommended. Each change here carries high risk |

> **Note:** McCabe's own official categorization (from his Department of Homeland Security presentation "Software Quality Metrics to Identify Risk") uses broader buckets: **1–10** = simple/little risk, **11–20** = more complex/moderate risk, **21–50** = complex/high risk, **>50** = untestable/very high risk. The table above is a stricter breakdown within that. The threshold of 10 was also adopted by NIST (Special Publication 500-235) which noted the figure "had received substantial corroborating evidence." See: https://en.wikipedia.org/wiki/Cyclomatic_complexity

### Why it matters — the connection to testing

Here's the key insight: **cyclomatic complexity tells you the minimum number of test cases needed to cover every path through a method.**

A method with complexity 4 has 4 independent paths. To fully test it, you need **at least 4 test cases**. A method with complexity 20 needs at least 20 test cases just for one method. This is where it becomes a practical metric — it directly predicts testing effort.

High cyclomatic complexity also correlates with:
- **More bugs** — more paths = more places for things to go wrong
- **Harder maintenance** — a developer reading the code has to hold more branches in their head
- **Harder code review** — reviewers can't easily verify all the paths mentally

### The output / metric

You get a **number per method** and can aggregate to **averages per class, per project, or overall**. You can also track the **maximum** (your most complex method) and the **distribution** (how many methods fall into each bucket).

Typical targets:
- Average complexity per method: **< 5**
- Maximum complexity for any method: **< 15** (some teams say < 10)

### How it applies to this project

Looking at the file sizes, the most complex code is likely in:
- `BookRepository.cs` (140 lines) — probably has query-building with conditionals
- `BooksController.cs` (127 lines) — CRUD actions with validation, auth checks, image handling
- `AuthorRepository.cs` (111 lines)
- `UploadStoragePaths.cs` (108 lines)
- `OrphanedUploadCleanupJob.cs` (98 lines) — background job logic likely has branching

**The metric you'd track: complexity score per method. Target: keep all methods under 10, average under 5.**

### Three practical uses of cyclomatic complexity

Once enabled, cyclomatic complexity serves three distinct purposes for us:

1. **A spec planning document** — the complexity score for every method (not just the problematic ones) tells us exactly how many test cases each method needs at minimum. A method with complexity 4 needs at least 4 test cases. This gives us a complete map for writing specs before we write a single test.

2. **A refactoring punch list** — any method over the threshold (likely 10) gets flagged. These become issues/tickets for refactoring work. We know exactly what needs to be broken apart and can prioritize it.

3. **A future guard rail** — once the codebase is clean, we promote the analyzer rule to `error` severity. From that point on, `dotnet build` will fail if anyone writes or modifies a method that exceeds the threshold. This prevents complexity from creeping back in over time.

This is all powered by a single Roslyn analyzer rule (**CA1502**), configured in `.editorconfig`. The same mechanism as linting — no separate tool needed.

---

## 3. Unit Testing & Code Coverage

### What unit testing is

A unit test is code that calls a small piece of your application code (a "unit" — usually a single method) with known inputs and asserts that the output matches what you expect.

```csharp
[Fact]
public void PaginatedList_ShouldCalculateTotalPages_Correctly()
{
    // Arrange: 25 items, 10 per page
    // Act: create the paginated list
    // Assert: TotalPages == 3
}
```

The tests run automatically (via `dotnet test`) and produce a pass/fail result. They're **deterministic** — same inputs always produce same outputs — and they run **fast** (milliseconds per test) because they don't touch databases, filesystems, or networks.

### What code coverage is

Code coverage measures **what percentage of your code is actually executed when your tests run**. It's the answer to: "how much of my code is tested?"

#### How it works mechanically

1. You run your tests with a **coverage collector** enabled
2. The collector instruments your code — it inserts tracking markers at every line/branch
3. As tests run, the collector records which markers get hit
4. After tests finish, it calculates percentages

#### Types of coverage

| Type | What it measures | Example |
|---|---|---|
| **Line coverage** | % of lines executed | 80% = 80 out of 100 lines were hit |
| **Branch coverage** | % of decision branches taken | An `if/else` has 2 branches. If tests only go through the `if`, that's 50% branch coverage for that statement |
| **Method coverage** | % of methods called at least once | If 8 out of 10 methods were called, that's 80% |

**Branch coverage is the most meaningful.** You can have 90% line coverage but miss an entire `else` branch that handles an error case. Branch coverage catches that.

#### What the numbers mean

| Coverage | Interpretation |
|---|---|
| **0-20%** | Essentially untested. No safety net |
| **20-50%** | Minimal coverage. Major paths probably tested, but lots of gaps |
| **50-70%** | Moderate. Core logic is likely covered. Reasonable for many projects |
| **70-85%** | Good. Most important code paths are tested |
| **85-95%** | Very good. This is the sweet spot most teams aim for |
| **95-100%** | Diminishing returns. The last 5% is often boilerplate, trivial getters, or framework glue that's not worth testing |

### The relationship between all three

This is where it all connects:

```
Linting ──────→ "Is the code CLEAN?"          (yes/no per rule)
Cyclomatic ───→ "Is the code SIMPLE?"          (number per method)
Coverage ─────→ "Is the code TESTED?"          (percentage)
Cyclomatic ───→ "How MUCH testing is needed?"  (minimum test count per method)
```

Cyclomatic complexity tells you **how testable** the code is. If a method has complexity 25, you need 25+ tests just for that one method — that's a sign the method should be broken apart first.

Coverage tells you **how tested** the code actually is. But 80% coverage on a method with complexity 2 is trivial. 80% coverage on a method with complexity 15 might still be missing critical edge cases.

Together they give you a complete picture:
- **High complexity + low coverage** = danger zone 🔴
- **High complexity + high coverage** = tested but fragile, consider refactoring 🟡
- **Low complexity + low coverage** = probably fine short-term, but add tests 🟡
- **Low complexity + high coverage** = ideal 🟢

### Unit testing in .NET specifically

The .NET testing ecosystem consists of:

- **Test framework**: xUnit (most popular for .NET), NUnit, or MSTest. xUnit is the modern standard
- **Mocking library**: Moq or NSubstitute — lets you create fake versions of dependencies (e.g., fake `BookRepository` that returns test data without hitting the database)
- **Assertion library**: xUnit has built-in assertions. FluentAssertions is a popular upgrade for readability
- **Coverage tool**: Coverlet (open source, integrates with `dotnet test`)

#### Project structure

A test project is a separate `.csproj` that references your main project:

```
/Bookshelf              ← the app
/Bookshelf.Tests        ← the test project (references Bookshelf)
  BookRepositoryTests.cs
  PaginatedListTests.cs
  ...
```

#### What's testable in THIS project

Looking at the codebase, here's what makes sense to test:

**High value (pure logic, no dependencies):**
- `PaginatedList.cs` — pagination math
- `RepositoryResult.cs` — result handling
- `UploadStoragePaths.cs` — path computation logic
- ViewModels — validation attributes

**Medium value (need mocking):**
- `BookRepository` / `AuthorRepository` — can test with an in-memory database
- `ImageSharpImageProcessor` — image processing logic
- `LocalFileStorage` — file operations (need to mock filesystem or use temp dirs)
- `OrphanedUploadCleanupJob` — background job logic

**Lower value (thin wrappers around framework):**
- Controllers — mostly coordinate between services. Can test but the ROI is lower since they're thin
- Tag helpers — framework integration, usually tested via integration/browser tests

### What unit testing does NOT tell you

- **That the pieces work together** — unit tests test in isolation. The controller might work in a unit test but fail when the real database is connected. That's what **integration tests** cover
- **That the UI works** — the user might see a broken page even though all unit tests pass. That's what **browser/end-to-end tests** cover
- **That the code is correct** — you can have 100% coverage with assertions that check the wrong things

This is the classic "testing pyramid":

```
        /  E2E  \          ← few, slow, expensive (browser tests)
       / Integr. \         ← some, medium speed (real DB, real HTTP)
      /   Unit    \        ← many, fast, cheap (isolated logic)
```

Unit tests are the foundation. You want lots of them because they're fast and cheap. Integration and E2E tests cover the gaps but cost more to write and maintain.

---

## 4. Summary: The Three Metrics at a Glance

| Metric | What it measures | Output format | Good target | Tool in .NET |
|---|---|---|---|---|
| **Linting** | Code cleanliness & potential bugs | Count of violations by severity | 0 errors, 0 warnings | Roslyn analyzers + .editorconfig |
| **Cyclomatic complexity** | Code simplicity / branching | Number per method | Avg < 5, max < 15 | Roslyn analyzer CA1502, or `dotnet-cccc` |
| **Code coverage** | How much code is tested | Percentage (line/branch) | 70-85% is solid | Coverlet + xUnit |

### What implementing these would look like in this project

1. **Linting**: Add an `.editorconfig` and `Directory.Build.props` to enable analyzers. Fix existing warnings. Add to CI so builds fail on new warnings. This is the **easiest** to implement — mostly configuration.

2. **Cyclomatic complexity**: Enable the CA1502 analyzer rule and set a threshold. Identify the most complex methods and refactor if needed. This is **also configuration** — the analyzer reports it, you just need to enable the rule.

3. **Unit testing + coverage**: Create a `Bookshelf.Tests` project with xUnit. Write tests starting with the most logic-dense code (repositories, services, helpers). Add Coverlet for coverage reporting. This is the **most work** — you're writing new code (tests).

The natural order of implementation: **linting first** (cheapest, immediate value), then **cyclomatic complexity** (also configuration, piggybacks on linting setup), then **unit tests** (ongoing investment).

---

## 5. Implementation Plan

The five steps below are ordered so that each step feeds into the next.

### Step 1: Add linting

Add `.editorconfig` and `Directory.Build.props` to enable the built-in Roslyn analyzers. Fix any existing warnings. This is configuration-only work and can be done as a single task. Once complete, the build will enforce code cleanliness going forward.

### Step 2: Enable cyclomatic complexity (first pass)

Enable the CA1502 analyzer rule as a **warning** (not an error yet). Run `dotnet build` to get the first report. This report gives us two things:

- **Complexity scores for every method in the app** — this becomes the spec planning document for step 3. Even methods that are within the threshold still have a score that tells us how many test cases they need.
- **A list of methods over the threshold** — these become refactoring issues/tickets for step 4.

### Step 3: Write specs

Using the complexity scores from step 2, add a test suite to this project (xUnit + Coverlet) and write tests. The scores tell us exactly how many test cases each method needs at minimum. Code coverage (via Coverlet) tracks our progress as a percentage. Writing tests before refactoring is intentional — the tests become the safety net that catches breakage during the refactoring in step 4.

### Step 4: Refactor complex methods

Work through the refactoring punch list from step 2. Any method over the threshold gets broken apart into simpler pieces. The specs from step 3 protect us here — if a refactor breaks behavior, the tests will catch it.

### Step 5: Promote the complexity guard to error

Once the codebase is clean (all methods under the threshold), change CA1502 from `warning` to `error` in `.editorconfig`. From this point forward, `dotnet build` will fail if anyone writes or modifies a method that exceeds the threshold. **The specific threshold (likely 10) should be confirmed with Will/CTO before this step** — it's a team decision that everyone needs to agree on before it becomes a hard gate.
