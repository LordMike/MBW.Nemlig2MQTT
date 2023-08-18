# https://hub.docker.com/_/microsoft-dotnet-core
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY . .
WORKDIR "/src/MBW.Nemlig2MQTT/"
RUN dotnet publish -c release -o /app

# Runtime image, use an arch-specific image
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:7.0-alpine

RUN apk --no-cache add tzdata

WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "./MBW.Nemlig2MQTT.dll"]