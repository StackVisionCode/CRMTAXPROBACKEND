FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SignDocuTax/SignDocuTax.csproj", "SignDocuTax/"]
RUN dotnet restore "SignDocuTax/SignDocuTax.csproj"
COPY . .
WORKDIR "/src/SignDocuTax"
RUN dotnet build "SignDocuTax.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SignDocuTax.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "SignDocuTax.dll"]