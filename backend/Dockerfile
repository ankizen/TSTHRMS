# Builds and runs the API only - the frontend is deployed separately (Vercel), so the
# BuildAndCopyFrontend MSBuild target (TSTHRMS.Api.csproj) is skipped here on purpose.
# See docs/deployment-coolify-vercel.md for the full deployment runbook.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first (by path, not a solution file) so `dotnet restore` is cached across
# builds as long as no project reference changes - only the four projects the API actually
# depends on, never the test projects, which don't belong in a runtime image.
COPY src/TSTHRMS.Domain/TSTHRMS.Domain.csproj src/TSTHRMS.Domain/
COPY src/TSTHRMS.Application/TSTHRMS.Application.csproj src/TSTHRMS.Application/
COPY src/TSTHRMS.Infrastructure/TSTHRMS.Infrastructure.csproj src/TSTHRMS.Infrastructure/
COPY src/TSTHRMS.Api/TSTHRMS.Api.csproj src/TSTHRMS.Api/
RUN dotnet restore src/TSTHRMS.Api/TSTHRMS.Api.csproj

COPY src/ src/
RUN dotnet publish src/TSTHRMS.Api/TSTHRMS.Api.csproj \
    --configuration Release \
    --no-restore \
    -p:SkipFrontendBuild=true \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 1000 app \
    && useradd --system --uid 1000 --gid app app \
    && mkdir -p /data/uploads /data/logs \
    && chown -R app:app /data /app

# /data is where a Coolify persistent volume should be mounted - uploaded files and log files
# both need to survive a redeploy, unlike the rest of the container filesystem.
ENV FileStorage__RootPath=/data/uploads
ENV Serilog__WriteTo__1__Args__path=/data/logs/tsthrms-.log
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
USER app

ENTRYPOINT ["dotnet", "TSTHRMS.Api.dll"]
