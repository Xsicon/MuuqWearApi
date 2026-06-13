# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies (layer cache when only app code changes)
COPY MuuqWear.API/MuuqWear.API.csproj MuuqWear.API/
COPY MuuqWear.Application/MuuqWear.Application.csproj MuuqWear.Application/
COPY MuuqWear.Model/MuuqWear.Model.csproj MuuqWear.Model/
RUN dotnet restore MuuqWear.API/MuuqWear.API.csproj

COPY . .
RUN dotnet publish MuuqWear.API/MuuqWear.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Default for local `docker run`; Render overrides via ASPNETCORE_URLS=http://+:${PORT}
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MuuqWear.API.dll"]
