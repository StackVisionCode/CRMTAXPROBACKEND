FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AuthService/AuthService.csproj", "AuthService/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
RUN dotnet restore "AuthService/AuthService.csproj"
COPY . .
WORKDIR "/src/AuthService"
RUN dotnet build "AuthService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Instalar curl para descargar GeoLite2
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Descargar GeoLite2-City.mmdb automáticamente
RUN curl -L -o /Data/GeoLite2-City.mmdb \
    "https://github.com/P3TERX/GeoLite.mmdb/raw/download/GeoLite2-City.mmdb" && \
    chmod 644 /Data/GeoLite2-City.mmdb && \
    ls -la /Data/

COPY --from=publish /app/publish .

# Verificar que el archivo GeoLite2 existe
RUN echo "GeoLite2 database size:" && du -h /Data/GeoLite2-City.mmdb

ENTRYPOINT ["dotnet", "AuthService.dll"]