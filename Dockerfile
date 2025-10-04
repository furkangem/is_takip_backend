# Doğru çalışma dizini
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# .csproj dosyasını doğru konumdan kopyalayın
COPY is_takip/*.csproj ./
RUN dotnet restore

# Tüm dosyaları kopyalayın
COPY is_takip/. ./
RUN dotnet publish -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "is_takip.dll"]