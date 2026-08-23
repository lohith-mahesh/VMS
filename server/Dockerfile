# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first so Docker can cache the restore layer.
COPY RRVMS.Api.csproj ./
RUN dotnet restore RRVMS.Api.csproj

# Copy the remaining source and publish.
COPY . .
RUN dotnet publish RRVMS.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=10000

EXPOSE 10000

# Run as the non-root user provided by the ASP.NET Core image.
USER $APP_UID

# The app reads the PORT environment variable injected by the hosting platform
# and binds to http://0.0.0.0:$PORT (see Program.cs).
ENTRYPOINT ["dotnet", "RRVMS.Api.dll"]
