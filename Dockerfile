# Билд-стадия
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Копируем ТОЛЬКО файл проекта сначала
COPY ["EmailService/EmailService.csproj", "EmailService/"]
RUN dotnet restore "EmailService/EmailService.csproj"

# 2. Копируем остальные файлы
COPY . .

# 3. Билд и публикация
WORKDIR "/src/EmailService"
RUN dotnet build "EmailService.csproj" -c Release -o /app/build
RUN dotnet publish "EmailService.csproj" -c Release -o /app/publish

# Финальный образ
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS="http://+:5000"
EXPOSE 5000
ENTRYPOINT ["dotnet", "EmailService.dll"]