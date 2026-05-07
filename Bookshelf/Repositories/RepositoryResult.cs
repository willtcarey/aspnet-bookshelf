namespace Bookshelf.Repositories;

/// <summary>
/// Outcome of a repository write operation, including validation errors that
/// callers can map onto their presentation layer.
/// </summary>
public sealed class RepositoryResult
{
    private static readonly IReadOnlyList<RepositoryValidationError> EmptyErrors =
        Array.Empty<RepositoryValidationError>();

    private RepositoryResult(bool succeeded, bool isNotFound, IReadOnlyList<RepositoryValidationError> validationErrors)
    {
        Succeeded = succeeded;
        IsNotFound = isNotFound;
        ValidationErrors = validationErrors;
    }

    public bool Succeeded { get; }
    public bool IsNotFound { get; }
    public bool HasValidationErrors => ValidationErrors.Count > 0;
    public IReadOnlyList<RepositoryValidationError> ValidationErrors { get; }

    public static RepositoryResult Success { get; } = new(succeeded: true, isNotFound: false, EmptyErrors);
    public static RepositoryResult NotFound { get; } = new(succeeded: false, isNotFound: true, EmptyErrors);

    public static RepositoryResult ValidationFailed(string key, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return ValidationFailed([new RepositoryValidationError(key, message)]);
    }

    public static RepositoryResult ValidationFailed(IReadOnlyList<RepositoryValidationError> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        return new RepositoryResult(
            succeeded: false,
            isNotFound: false,
            validationErrors.ToArray());
    }
}

public sealed record RepositoryValidationError(string Key, string Message);
