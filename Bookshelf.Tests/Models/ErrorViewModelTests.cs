using Bookshelf.Models;

namespace Bookshelf.Tests.Models;

public class ErrorViewModelTests
{
    [Fact]
    public void ShowRequestId_NonEmptyRequestId_ReturnsTrue()
    {
        var vm = new ErrorViewModel { RequestId = "trace-abc" };

        Assert.True(vm.ShowRequestId);
    }

    [Fact]
    public void ShowRequestId_NullRequestId_ReturnsFalse()
    {
        var vm = new ErrorViewModel { RequestId = null };

        Assert.False(vm.ShowRequestId);
    }

    [Fact]
    public void ShowRequestId_EmptyRequestId_ReturnsFalse()
    {
        var vm = new ErrorViewModel { RequestId = string.Empty };

        Assert.False(vm.ShowRequestId);
    }
}
