FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# ✅ Copiar todos los .csproj necesarios antes del restore
COPY ["SignDocuTax/SignDocuTax.csproj", "SignDocuTax/"]
COPY ["SharedLibrary/SharedLibrary.csproj", "SharedLibrary/"]

# 🔧 Restaurar dependencias
RUN dotnet restore "SignDocuTax/SignDocuTax.csproj"

# ✅ Luego copiar el resto del código
COPY . .

WORKDIR "/src/SignDocuTax"
RUN dotnet build "SignDocuTax.csproj" -c Release -o /app/build
RUN dotnet tools install --global dotnet-ef --version 9.0.0 \
    && export PATH="$PATH:/root/.dotnet/tools" \
     && dotnet ef --version \
    && dotnet ef database update --project ../SignDocuTax/SignDocuTax.csproj 

FROM build AS publish
RUN dotnet publish "SignDocuTax.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SignDocuTax.dll"]
