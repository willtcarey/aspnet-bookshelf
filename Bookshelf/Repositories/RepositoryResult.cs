namespace Bookshelf.Repositories;

/// <summary>
/// Outcome of a repository write operation. When the status is
/// <see cref="ValidationFailed"/>, the repository has already written the
/// relevant errors into the caller's <c>ModelStateDictionary</c>.
/// </summary>
public enum RepositoryResult
{
    Success,
    NotFound,
    ValidationFailed
}
