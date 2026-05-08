using Bookshelf.Repositories;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Bookshelf.Extensions;

internal static class ModelStateDictionaryExtensions
{
    internal static void AddRepositoryErrors(this ModelStateDictionary modelState, RepositoryResult result)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(result);

        foreach (var error in result.ValidationErrors)
        {
            modelState.AddModelError(error.Key, error.Message);
        }
    }
}
