# Notes

- ASP.NET MVC (`dotnet new mvc`) installs jQuery and Bootstrap by default
- `Properties/launchSettings.json` configures how the app launches through Visual Studio. We use dip for that instead
- `Bookshelf.csproj` is like a `tsconfig.json` or `package.json` — project configuration for development (target framework, compiler settings, package dependencies)
- `appsettings.json` configures the runtime of the app (logging, connection strings, etc.). Environment-specific overrides go in `appsettings.Development.json`
