using Bookshelf.Helpers;

namespace Bookshelf.Tests.Helpers;

public class PaginatedListTests
{
    [Fact]
    public void HasPreviousPagePageIndexOneReturnsFalse()
    {
        var list = new PaginatedList<string>(
            items: new List<string>(), count: 50, pageIndex: 1, pageSize: 10, sortColumn: null, sortDirection: null);

        Assert.False(list.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPagePageIndexGreaterThanOneReturnsTrue()
    {
        var list = new PaginatedList<string>(
            items: new List<string>(), count: 50, pageIndex: 2, pageSize: 10, sortColumn: null, sortDirection: null);

        Assert.True(list.HasPreviousPage);
    }

    [Fact]
    public void HasNextPagePageIndexLessThanTotalPagesReturnsTrue()
    {
        var list = new PaginatedList<string>(
            items: new List<string>(), count: 50, pageIndex: 2, pageSize: 10, sortColumn: null, sortDirection: null);

        Assert.True(list.HasNextPage);
    }

    [Fact]
    public void HasNextPageOnLastPageReturnsFalse()
    {
        var list = new PaginatedList<string>(
            items: new List<string>(), count: 50, pageIndex: 5, pageSize: 10, sortColumn: null, sortDirection: null);

        Assert.False(list.HasNextPage);
    }
}
