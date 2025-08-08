FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/services/KanbanService/KanbanService.csproj", "src/services/KanbanService/"]
COPY ["src/shared/KanbanBoard.Shared/KanbanBoard.Shared.csproj", "src/shared/KanbanBoard.Shared/"]
COPY ["src/shared/KanbanBoard.Common/KanbanBoard.Common.csproj", "src/shared/KanbanBoard.Common/"]

RUN dotnet restore "src/services/KanbanService/KanbanService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/services/KanbanService"
RUN dotnet build "KanbanService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "KanbanService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Add curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "KanbanService.dll"]