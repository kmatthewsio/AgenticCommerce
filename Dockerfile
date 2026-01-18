FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY src/AgenticCommerce.API/*.csproj src/AgenticCommerce.API/
COPY src/AgenticCommerce.Core/*.csproj src/AgenticCommerce.Core/
COPY src/AgenticCommerce.Infrastructure/*.csproj src/AgenticCommerce.Infrastructure/
COPY src/AgenticCommerce.Enterprise/*.csproj src/AgenticCommerce.Enterprise/

# Restore from API project (references others)
RUN dotnet restore src/AgenticCommerce.API/AgenticCommerce.API.csproj

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/src/AgenticCommerce.API
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Render uses port 10000
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "AgenticCommerce.API.dll"]
