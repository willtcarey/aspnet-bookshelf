using Bookshelf.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Moq;

namespace Bookshelf.Tests.TestSupport;

public static class TestUploadPaths
{
    public static UploadStoragePaths Create(
        string webRoot,
        string? contentRoot = null,
        string? uploadsConfig = null)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupProperty(e => e.WebRootPath, webRoot);
        environment.SetupProperty(e => e.ContentRootPath, contentRoot ?? webRoot);
        environment.SetupProperty(e => e.EnvironmentName, "Test");
        environment.SetupProperty(e => e.ApplicationName, "Bookshelf.Tests");
        environment.SetupProperty(e => e.WebRootFileProvider, new NullFileProvider());
        environment.SetupProperty(e => e.ContentRootFileProvider, new NullFileProvider());

        var configValues = new Dictionary<string, string?>
        {
            ["FileStorage:UploadsPath"] = uploadsConfig
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new UploadStoragePaths(environment.Object, configuration);
    }
}
