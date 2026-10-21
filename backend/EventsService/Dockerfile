FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY Avra.ServiceDefaults/Avra.ServiceDefaults.csproj Avra.ServiceDefaults/
COPY Avra.EventBus/Avra.EventBus.csproj Avra.EventBus/
COPY EventsService/EventsService.csproj EventsService/
RUN dotnet restore EventsService/EventsService.csproj
COPY . .
RUN dotnet publish EventsService/EventsService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventsService.dll"]
