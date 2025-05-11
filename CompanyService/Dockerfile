FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CompanyService/CompanyService.csproj", "CompanyService/"]
RUN dotnet restore "CompanyService/CompanyService.csproj"
COPY . .
WORKDIR "/src/CompanyService"
RUN dotnet build "CompanyService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CompanyService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "CompanyService.dll"]