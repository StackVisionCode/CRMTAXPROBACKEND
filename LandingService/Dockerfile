FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["LandingService/LandingService.csproj", "LandingService/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
RUN dotnet restore "LandingService/LandingService.csproj"
COPY . .
WORKDIR "/src/LandingService"
RUN dotnet build "LandingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LandingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "LandingService.dll"]