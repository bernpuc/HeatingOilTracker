FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HeatingOilTracker.Core/HeatingOilTracker.Core.csproj HeatingOilTracker.Core/
COPY HeatingOilTracker.Server/HeatingOilTracker.Server.csproj HeatingOilTracker.Server/
RUN dotnet restore HeatingOilTracker.Server/HeatingOilTracker.Server.csproj

COPY HeatingOilTracker.Core/ HeatingOilTracker.Core/
COPY HeatingOilTracker.Server/ HeatingOilTracker.Server/
RUN dotnet publish HeatingOilTracker.Server/HeatingOilTracker.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
ENV TRACKER_DATA_PATH=/data/data.json
EXPOSE 8080

ENTRYPOINT ["dotnet", "HeatingOilTracker.Server.dll"]
