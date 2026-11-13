FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ChallengeTrackerApi/ChallengeTrackerApi.csproj ChallengeTrackerApi/
RUN dotnet restore ChallengeTrackerApi/ChallengeTrackerApi.csproj

COPY ChallengeTrackerApi/ ChallengeTrackerApi/
RUN dotnet publish ChallengeTrackerApi/ChallengeTrackerApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ChallengeTrackerApi.dll"]
