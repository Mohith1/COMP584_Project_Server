# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY FleetManagement.Api/FleetManagement.Api.csproj FleetManagement.Api/
COPY FleetManagement.Services/FleetManagement.Services.csproj FleetManagement.Services/
COPY FleetManagement.Data/FleetManagement.Data.csproj FleetManagement.Data/

RUN dotnet restore FleetManagement.Api/FleetManagement.Api.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/FleetManagement.Api
RUN dotnet build -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FleetManagement.Api.dll"]

