FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file and restore first (cache layer)
COPY ["UssdDevelopmentCore/UssdDevelopmentCore.csproj", "UssdDevelopmentCore/"]
RUN dotnet restore "UssdDevelopmentCore/UssdDevelopmentCore.csproj"

# Copy the entire project
COPY UssdDevelopmentCore/ UssdDevelopmentCore/

# Build
WORKDIR "/src/UssdDevelopmentCore"
RUN dotnet build "UssdDevelopmentCore.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/UssdDevelopmentCore"
RUN dotnet publish "UssdDevelopmentCore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UssdDevelopmentCore.dll"]
