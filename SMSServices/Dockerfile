FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SMSServices/SMSServices.csproj", "SMSServices/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
RUN dotnet restore "SMSServices/SMSServices.csproj"
COPY . .
WORKDIR "/src/SMSServices"
RUN dotnet build "SMSServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SMSServices.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "SMSServices.dll"]