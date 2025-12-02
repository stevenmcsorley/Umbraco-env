FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build React frontend
FROM node:20-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ .
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MyDockerProject/MyDockerProject/MyDockerProject.csproj", "MyDockerProject/MyDockerProject/"]
RUN dotnet restore "MyDockerProject/MyDockerProject/MyDockerProject.csproj"
COPY MyDockerProject/MyDockerProject/ MyDockerProject/MyDockerProject/
# Copy built React app from frontend-build stage to wwwroot
COPY --from=frontend-build /src/frontend/dist/booking-engine.iife.js MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js
WORKDIR "/src/MyDockerProject/MyDockerProject"
RUN dotnet build "MyDockerProject.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/MyDockerProject/MyDockerProject"
# Ensure booking-engine.js exists before publish (wwwroot is copied automatically)
RUN test -f wwwroot/scripts/booking-engine.js || (echo "ERROR: booking-engine.js not found!" && exit 1)
RUN dotnet publish "MyDockerProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# We need to make sure that the user running the app has write access to the umbraco folder and wwwroot, in order to write logs and other files.
# Since these are volumes they are created as root by the docker daemon.
USER root
RUN mkdir -p umbraco umbraco/Logs wwwroot/css wwwroot/media
RUN chown -R $APP_UID:$APP_UID umbraco wwwroot
RUN chmod -R 755 wwwroot
USER $APP_UID
ENTRYPOINT ["dotnet", "MyDockerProject.dll"]
