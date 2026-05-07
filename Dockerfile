FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY aspnet-core/ ./
RUN dotnet restore ./PrivateCloudDrive.slnx
RUN dotnet publish ./src/PrivateCloudDrive.HttpApi.Host/PrivateCloudDrive.HttpApi.Host.csproj -c Release -o /app/api --no-restore
RUN dotnet publish ./src/PrivateCloudDrive.DbMigrator/PrivateCloudDrive.DbMigrator.csproj -c Release -o /app/migrator --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/api ./api
COPY --from=build /app/migrator ./migrator

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "/app/api/PrivateCloudDrive.HttpApi.Host.dll"]
