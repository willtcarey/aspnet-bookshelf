using Bookshelf.Helpers;

namespace Bookshelf.Tests.Helpers;

public class PaginatedListTests
{
    [Fact]
    public void HasPreviousPage_PageIndexOne_ReturnsFalse()
    {
        var list = new PaginatedList<string>(
            items: new List<string>(), count: 50, pageIndex: 1, pageSize: 10, sortColumn: null, sortDirection: null);

        Assert.False(list.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_PageIndexGreaterThanOne_ReturnsTrue()
    {
        var list = new PaginatedList<string>(
            items: new List<string>(), count: 50, pageIndex: 2, pageSize: 10, sortColumn: null, sortDirection: null);

        Assert.True(list.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_PageIndexLessThanTotalPages_ReturnsTrue()
    {
        var list = new PaginatedList<string>(
            items: new List<string>(), count: 50, pageIndex: 2, pageSize: 10, sortColumn: null, sortDirection: null);

        Assert.True(list.HasNextPage);
    }

    [Fact]
    public void HasNextPage_OnLastPage_ReturnsFalse()
    {
        var list = new PaginatedList<string>(
            items: new List<string>(), count: 50, pageIndex: 5, pageSize: 10, sortColumn: null, sortDirection: null);

        Assert.False(list.HasNextPage);
    }
}
