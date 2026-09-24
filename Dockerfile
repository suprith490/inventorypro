# =============================================================================
# InventoryPro API - multi-stage Docker build
#
# Stage 1 (build):   restore + publish the app with the full .NET SDK.
# Stage 2 (runtime): a small ASP.NET Core image that only contains the published
#                    output. This keeps the final image lean and AWS-friendly.
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first so NuGet restore is cached between builds.
COPY InventoryPro.sln ./
COPY src/InventoryPro.Api/InventoryPro.Api.csproj src/InventoryPro.Api/
RUN dotnet restore src/InventoryPro.Api/InventoryPro.Api.csproj

# Copy the rest of the source and publish.
COPY . .
RUN dotnet publish src/InventoryPro.Api/InventoryPro.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# ASP.NET Core 8 containers listen on 8080 by default. AWS App Runner / ECS
# will route traffic to this port.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InventoryPro.Api.dll"]
