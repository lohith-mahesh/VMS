FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json RRVMS.sln ./
COPY src/RRVMS.Domain/RRVMS.Domain.csproj src/RRVMS.Domain/
COPY src/RRVMS.Application/RRVMS.Application.csproj src/RRVMS.Application/
COPY src/RRVMS.Infrastructure/RRVMS.Infrastructure.csproj src/RRVMS.Infrastructure/
COPY src/RRVMS.Api/RRVMS.Api.csproj src/RRVMS.Api/
RUN dotnet restore src/RRVMS.Api/RRVMS.Api.csproj
COPY src ./src
RUN dotnet publish src/RRVMS.Api/RRVMS.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RRVMS.Api.dll"]
