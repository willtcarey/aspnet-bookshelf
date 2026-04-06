FROM mcr.microsoft.com/dotnet/sdk:8.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Expose the default ASP.NET port
EXPOSE 8080

# Default to bash — docker-compose.yml sets the actual command for the web service
CMD ["bash"]
