# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Installer dotnet-ef globalt, så migrations kan køres i containeren
RUN dotnet tool install --global dotnet-ef --version 8.0.17

# Tilføj dotnet tools til PATH
ENV PATH="$PATH:/root/.dotnet/tools"

COPY . .
RUN dotnet restore "./MinaGroup.Backend.csproj"
RUN dotnet publish "./MinaGroup.Backend.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "MinaGroup.Backend.dll"]
