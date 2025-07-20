FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["signature/signature.csproj", "signature/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]
RUN dotnet restore "signature/signature.csproj"
COPY . .
WORKDIR "/src/signature"
RUN dotnet build "signature.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "signature.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "signature.dll"]