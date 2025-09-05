FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CommLinkService/CommLinkService.csproj", "CommLinkService/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
RUN dotnet restore "CommLinkService/CommLinkService.csproj"
COPY . .
WORKDIR "/src/CommLinkService"
RUN dotnet build "CommLinkService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CommLinkService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "CommLinkService.dll"]