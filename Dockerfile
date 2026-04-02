FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# Expose the default ASP.NET port
EXPOSE 8080

# Use dotnet watch for hot reload in development
ENTRYPOINT ["dotnet", "watch", "run", "--project", "Bookshelf", "--urls", "http://0.0.0.0:8080"]
