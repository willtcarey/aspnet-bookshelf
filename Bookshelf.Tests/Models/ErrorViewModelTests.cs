using Bookshelf.Models;

namespace Bookshelf.Tests.Models;

public class ErrorViewModelTests
{
    [Fact]
    public void ShowRequestIdNonEmptyRequestIdReturnsTrue()
    {
        var vm = new ErrorViewModel { RequestId = "trace-abc" };

        Assert.True(vm.ShowRequestId);
    }

    [Fact]
    public void ShowRequestIdNullRequestIdReturnsFalse()
    {
        var vm = new ErrorViewModel { RequestId = null };

        Assert.False(vm.ShowRequestId);
    }

    [Fact]
    public void ShowRequestIdEmptyRequestIdReturnsFalse()
    {
        var vm = new ErrorViewModel { RequestId = string.Empty };

        Assert.False(vm.ShowRequestId);
    }
}
