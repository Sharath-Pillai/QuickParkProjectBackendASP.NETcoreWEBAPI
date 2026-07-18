# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj first and restore (layer-cache friendly)
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and publish in Release mode
COPY . ./
RUN dotnet publish -c Release -o /app/out

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/out .

# Railway injects PORT env var; default to 8080
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "QuickParkAPI.dll"]
