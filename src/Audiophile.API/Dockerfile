# Build from the repo root: docker build -f src/Audiophile.API/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# The project files first, so a code change doesn't download the packages again
COPY src/Audiophile.API/Audiophile.API.csproj src/Audiophile.API/
COPY src/Audiophile.Application/Audiophile.Application.csproj src/Audiophile.Application/
COPY src/Audiophile.Domain/Audiophile.Domain.csproj src/Audiophile.Domain/
COPY src/Audiophile.Infrastructure/Audiophile.Infrastructure.csproj src/Audiophile.Infrastructure/
RUN dotnet restore src/Audiophile.API/Audiophile.API.csproj

COPY src/ src/
RUN dotnet publish src/Audiophile.API/Audiophile.API.csproj --configuration Release --no-restore --output /app

# The final image has only the runtime and the published app, and listens on 8080 as a normal user
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Audiophile.API.dll"]
