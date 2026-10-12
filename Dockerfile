# syntax=docker/dockerfile:1

# --- Build stage ---
# Build on the NATIVE host architecture and cross-publish for the target architecture. This avoids
# running the .NET SDK under QEMU emulation (which is slow and crashes), so multi-arch builds are
# fast and reliable. Only the final runtime layer is target-arch (it just copies files).
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first (layer-cached): copy only project files, then restore for the target arch.
COPY UrlShortener.slnx ./
COPY src/UrlShortener.Domain/UrlShortener.Domain.csproj src/UrlShortener.Domain/
COPY src/UrlShortener.Application/UrlShortener.Application.csproj src/UrlShortener.Application/
COPY src/UrlShortener.Infrastructure/UrlShortener.Infrastructure.csproj src/UrlShortener.Infrastructure/
COPY src/UrlShortener.Api/UrlShortener.Api.csproj src/UrlShortener.Api/
RUN dotnet restore src/UrlShortener.Api/UrlShortener.Api.csproj -a $TARGETARCH

# Copy the rest and cross-publish (framework-dependent) for the target arch.
COPY src/ src/
RUN dotnet publish src/UrlShortener.Api/UrlShortener.Api.csproj \
    -c Release -a $TARGETARCH --no-restore --no-self-contained \
    -o /app/publish /p:UseAppHost=false

# --- Runtime stage (target arch; multi-arch base image) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Listen on 8080 (non-privileged); TLS is expected to terminate at the ingress/load balancer.
# Default to a dedicated, writable SQLite path so a plain `docker run` works standalone with no
# external services (in-memory cache/events). docker-compose overrides these for the Postgres stack.
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/data/urlshortener.db"
RUN mkdir -p /data
VOLUME ["/data"]
EXPOSE 8080

ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"]
