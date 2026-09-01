# syntax=docker/dockerfile:1
#
# Build context is the repository root (run as `docker build -f Vespera.Api/Dockerfile .`, or via
# docker-compose.yml at the root) because Vespera.Api references its sibling projects. Debian-based
# (not -alpine) images throughout: the Tesseract OCR wrapper (Vespera.Infrastructure/Services) ships
# native binaries built against glibc, not musl, so alpine risks a runtime failure that's easy to
# miss since TesseractEngine is only constructed lazily when an OCR request actually runs.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Solution + csproj files first, so `dotnet restore` is cached across rebuilds that only change
# .cs files, not project references or package versions. The root .editorconfig has to come along
# too — it's where several analyzer severities (CA1711/CA1716/CA1822 among them) are deliberately
# downgraded for this codebase's own naming conventions; without it in the build context, Roslyn
# falls back to full default severity and /warnaserror-equivalent (TreatWarningsAsErrors) fails a
# clean build on things the repo's own rules already say are fine.
COPY Directory.Build.props Directory.Packages.props Vespera.sln .editorconfig ./
COPY Vespera.Domain/Vespera.Domain.csproj Vespera.Domain/
COPY Vespera.Application/Vespera.Application.csproj Vespera.Application/
COPY Vespera.Infrastructure/Vespera.Infrastructure.csproj Vespera.Infrastructure/
COPY Vespera.Api/Vespera.Api.csproj Vespera.Api/
RUN dotnet restore Vespera.Api/Vespera.Api.csproj

COPY Vespera.Domain/ Vespera.Domain/
COPY Vespera.Application/ Vespera.Application/
COPY Vespera.Infrastructure/ Vespera.Infrastructure/
COPY Vespera.Api/ Vespera.Api/
RUN dotnet publish Vespera.Api/Vespera.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is only for the HEALTHCHECK below; the aspnet runtime image doesn't ship it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system --gid 1000 vespera \
    && useradd --system --uid 1000 --gid vespera vespera

COPY --from=build /app/publish .
RUN chown -R vespera:vespera /app
USER vespera

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=30s --retries=5 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Vespera.Api.dll"]
