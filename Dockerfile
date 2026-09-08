# ── Build stage ──
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/DigitalBank.Api/DigitalBank.Api.csproj", "src/DigitalBank.Api/"]
COPY ["src/DigitalBank.Application/DigitalBank.Application.csproj", "src/DigitalBank.Application/"]
COPY ["src/DigitalBank.Domain/DigitalBank.Domain.csproj", "src/DigitalBank.Domain/"]
COPY ["src/DigitalBank.Infrastructure/DigitalBank.Infrastructure.csproj", "src/DigitalBank.Infrastructure/"]
RUN dotnet restore "src/DigitalBank.Api/DigitalBank.Api.csproj"

COPY . .
WORKDIR /src/src/DigitalBank.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ──
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DigitalBank.Api.dll"]
