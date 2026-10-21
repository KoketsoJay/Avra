FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY Avra.ServiceDefaults/Avra.ServiceDefaults.csproj Avra.ServiceDefaults/
COPY Avra.EventBus/Avra.EventBus.csproj Avra.EventBus/
COPY CommunityService/CommunityService.csproj CommunityService/
RUN dotnet restore CommunityService/CommunityService.csproj
COPY . .
RUN dotnet publish CommunityService/CommunityService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommunityService.dll"]
