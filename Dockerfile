FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Rovio.Matchmaking.slnx ./
COPY src/ ./src/
RUN dotnet restore src/Rovio.Matchmaking.Api/Rovio.Matchmaking.Api.csproj
RUN dotnet publish src/Rovio.Matchmaking.Api/Rovio.Matchmaking.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Rovio.Matchmaking.Api.dll"]
