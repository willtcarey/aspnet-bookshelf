# Cyclomatic Complexity Report

> Baseline complexity scores for every non-trivial method in `Bookshelf/`. Generated for issue #6 / Step 2 of the code-quality plan. Feeds Step 3 (spec planning — score = minimum test cases per method) and Step 4 (refactoring punch list for the worst offenders).

## How this was generated

Roslyn's CA1502 (Avoid excessive complexity) was enabled at `severity = warning` and set to fire on methods with complexity greater than 1, so every branching method gets reported with its score. The threshold is configured in `Bookshelf/CodeMetricsConfig.txt` and wired in via `<AdditionalFiles>` in `Bookshelf.csproj` (CA1502 does **not** read its threshold from `.editorconfig`).

Then:

```
docker compose run --rm --no-deps -w /app/Bookshelf web dotnet build --no-incremental
```

The CA1502 warnings in the build output are this report.

**This config is temporary scaffolding.** When CA1502 is later promoted to a permanent guardrail (Step 5), the threshold will go up to whatever real value we agree on (10 / 15 / etc.) and `<WarningsNotAsErrors>` will be removed.

## Caveats

- Methods with cyclomatic complexity 1 (no branching at all) are **not** in this report. CA1502 only emits when complexity *exceeds* the configured threshold, and threshold = 0 isn't accepted. A complexity-1 method needs only one trivial test case, so this is fine for spec planning.
- Constructors show up as `.ctor` — those are non-trivial constructors that do conditional work.
- `Bookshelf/Migrations/` is excluded by `.editorconfig` (`generated_code = true`).

## Summary

73 methods reported. None exceed the typical 25-threshold; the highest is 14.

| Complexity | Methods |
|---:|---:|
| 14 | 1 |
| 11 | 1 |
| 8  | 1 |
| 7  | 3 |
| 5  | 10 |
| 4  | 7 |
| 3  | 22 |
| 2  | 28 |

## Top of the punch list (complexity ≥ 5)

These are the candidates worth scrutinizing first when we get to Step 4 refactoring.

| Score | Method | Location |
|---:|---|---|
| 14 | `SaveChangesAsync` | `Data/ApplicationDbContext.cs:57` |
| 11 | `Process` | `TagHelpers/SortableColumnTagHelper.cs:23` |
| 8  | `IsNumericType` | `TagHelpers/FormTextTagHelper.cs:46` |
| 7  | `GetAsync` | `Models/ImageUpload.cs:72` |
| 7  | `GetResizedAsync` | `Models/ImageUpload.cs:114` |
| 7  | `NormalizeStoredPath` | `Services/UploadStoragePaths.cs:29` |
| 5  | `ApplySort` | `Areas/Admin/Controllers/AdminCrudController.cs:140` |
| 5  | `Edit` (POST) | `Areas/Admin/Controllers/AdminCrudController.cs:84` |
| 5  | `Edit` (POST) | `Controllers/AuthorsController.cs:79` |
| 5  | `Edit` (POST) | `Controllers/BooksController.cs:93` |
| 5  | `<Main>$` | `Program.cs:1` |
| 5  | `DeleteAsync` | `Services/LocalFileStorage.cs:44` |
| 5  | `RunAsync` | `Services/OrphanedUploadCleanupJob.cs:36` |
| 5  | `BuildCachePath` | `Services/UploadStoragePaths.cs:71` |
| 5  | `EnumerateCacheVariantPaths` | `Services/UploadStoragePaths.cs:79` |
| 5  | `BuildSource` | `TagHelpers/ImageUploadTagHelper.cs:48` |

## Full report

### Complexity 4

| Method | Location |
|---|---|
| `Index` | `Areas/Admin/Controllers/UsersController.cs:24` |
| `ToggleAdmin` | `Areas/Admin/Controllers/UsersController.cs:57` |
| `Register` (POST) | `Controllers/AccountController.cs:28` |
| `Login` (POST) | `Controllers/AccountController.cs:63` |
| `SaveAsync` | `Models/ImageUpload.cs:49` |
| `ResizeAsync` | `Services/ImageSharpImageProcessor.cs:12` |
| `Process` | `TagHelpers/ImageUploadTagHelper.cs:24` |

### Complexity 3

| Method | Location |
|---|---|
| `Edit` (GET) | `Areas/Admin/Controllers/AdminCrudController.cs:69` |
| `Delete` (GET) | `Areas/Admin/Controllers/AdminCrudController.cs:116` |
| `Details` | `Controllers/AuthorsController.cs:28` |
| `Create` (POST) | `Controllers/AuthorsController.cs:48` |
| `Edit` (GET) | `Controllers/AuthorsController.cs:61` |
| `Delete` (GET) | `Controllers/AuthorsController.cs:97` |
| `Details` | `Controllers/BooksController.cs:28` |
| `Create` (POST) | `Controllers/BooksController.cs:53` |
| `Edit` (GET) | `Controllers/BooksController.cs:69` |
| `Delete` (GET) | `Controllers/BooksController.cs:112` |
| `Error` | `Controllers/HomeController.cs:30` |
| `IsValidDimension` | `Models/ImageUpload.cs:167` |
| `ResolveFormat` | `Models/ImageUpload.cs:172` |
| `.ctor` | `Repositories/AuthorRepository.cs:20` |
| `UpdateAsync` | `Repositories/AuthorRepository.cs:72` |
| `.ctor` | `Repositories/BookRepository.cs:23` |
| `UpdateAsync` | `Repositories/BookRepository.cs:82` |
| `Authorize` | `Services/HangfireDashboardAuthorizationFilter.cs:9` |
| `NormalizeUploadsPath` | `Services/UploadStoragePaths.cs:97` |
| `GenerateInput` | `TagHelpers/FormSelectTagHelper.cs:20` |
| `ShouldSelectPlaceholder` | `TagHelpers/FormSelectTagHelper.cs:45` |
| `ResolveInputType` | `TagHelpers/FormTextTagHelper.cs:27` |

### Complexity 2

| Method | Location |
|---|---|
| `Create` (GET) | `Areas/Admin/Controllers/AdminCrudController.cs:53` |
| `DeleteConfirmed` | `Areas/Admin/Controllers/AdminCrudController.cs:129` |
| `DeleteConfirmed` | `Controllers/AuthorsController.cs:110` |
| `DeleteConfirmed` | `Controllers/BooksController.cs:125` |
| `Create` | `Controllers/ImagesController.cs:21` |
| `GetOriginalAsync` | `Models/ImageUpload.cs:103` |
| `GetContentTypeFromPath` | `Models/ImageUpload.cs:184` |
| `FindAsync` | `Repositories/AuthorRepository.cs:36` |
| `FindWithBooksAsync` | `Repositories/AuthorRepository.cs:42` |
| `ExistsAsync` | `Repositories/AuthorRepository.cs:107` |
| `FindAsync` | `Repositories/BookRepository.cs:40` |
| `FindWithAuthorAsync` | `Repositories/BookRepository.cs:46` |
| `CreateAsync` | `Repositories/BookRepository.cs:59` |
| `ValidateAsync` | `Repositories/BookRepository.cs:123` |
| `IsOwnedAuthorAsync` | `Repositories/BookRepository.cs:134` |
| `NormalizeFormat` | `Services/ImageSharpImageProcessor.cs:52` |
| `SaveAsync` | `Services/LocalFileStorage.cs:12` |
| `GetAsync` | `Services/LocalFileStorage.cs:32` |
| `GetUrl` | `Services/LocalFileStorage.cs:69` |
| `ResolveExtension` | `Services/LocalFileStorage.cs:75` |
| `ResolveGracePeriod` | `Services/OrphanedUploadCleanupJob.cs:88` |
| `.ctor` | `Services/UploadStoragePaths.cs:10` |
| `ResolveUploadAbsolutePath` | `Services/UploadStoragePaths.cs:63` |
| `Process` | `TagHelpers/FormFileTagHelper.cs:33` |
| `BuildPreviewContainer` | `TagHelpers/FormFileTagHelper.cs:79` |
| `BuildFileInput` | `TagHelpers/FormFileTagHelper.cs:119` |
| `BuildHiddenInput` | `TagHelpers/FormFileTagHelper.cs:134` |
| `BuildHint` | `TagHelpers/FormFileTagHelper.cs:145` |
