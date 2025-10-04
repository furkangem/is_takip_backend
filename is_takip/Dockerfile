# 1. Aşama: Projeyi build etme (Derleme)
# Bu bölüm, kodunuzu çalıştırılabilir hale getirmek için .NET SDK'sını kullanır.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Önce .csproj dosyasını kopyalayıp bağımlılıkları yüklüyoruz. Bu işlem derlemeyi hızlandırır.
COPY *.csproj ./
RUN dotnet restore

# Projenin geri kalan tüm dosyalarını kopyalayıp yayınlanabilir hale getiriyoruz.
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Aşama: Yayınlanan uygulamayı çalıştırma
# Bu bölüm, sadece uygulamayı çalıştırmak için gereken daha küçük bir .NET sürümünü kullanır.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "is_takip.dll"]