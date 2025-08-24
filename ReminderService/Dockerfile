FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ReminderService/ReminderService.csproj", "ReminderService/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
RUN dotnet restore "ReminderService/ReminderService.csproj"
COPY . .
WORKDIR "/src/ReminderService"
RUN dotnet build "ReminderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReminderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "ReminderService.dll"]