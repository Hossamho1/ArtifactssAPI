# Multi-stage Dockerfile for .NET 10 ASP.NET Core app
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["ArtifactsAPI/ArtifactsAPI.csproj", "ArtifactsAPI/"]
RUN dotnet restore "ArtifactsAPI/ArtifactsAPI.csproj"

# copy everything else and build
COPY . .
WORKDIR /src/ArtifactsAPI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS="http://+:80"
EXPOSE 80

ENTRYPOINT ["dotnet", "ArtifactsAPI.dll"]
