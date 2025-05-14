FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EmailService.csproj", "./"]
RUN dotnet restore "EmailService.csproj"
COPY . .
RUN dotnet build "EmailService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS="http://+:5000"
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 5000
ENTRYPOINT ["dotnet", "EmailService.dll"]