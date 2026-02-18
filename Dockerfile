# Updated for .NET 8.0 compatibility - Feb 17 2026
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LaundryApp.csproj", "./"]
RUN dotnet restore "LaundryApp.csproj" --no-cache
COPY . .
RUN dotnet build "LaundryApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LaundryApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create data directory for SQLite
RUN mkdir -p /var/data && chmod 777 /var/data

ENTRYPOINT ["dotnet", "LaundryApp.dll"]
