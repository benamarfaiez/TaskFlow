# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY FlowTasks.API/*.csproj ./FlowTasks.API/
COPY FlowTasks.Application/*.csproj ./FlowTasks.Application/
COPY FlowTasks.Domain/*.csproj ./FlowTasks.Domain/
COPY FlowTasks.Infrastructure/*.csproj ./FlowTasks.Infrastructure/

# Restore les dépendances
RUN dotnet restore

# Copie tout le code source
COPY FlowTasks.API/. ./FlowTasks.API/
COPY FlowTasks.Application/. ./FlowTasks.Application/
COPY FlowTasks.Domain/. ./FlowTasks.Domain/
COPY FlowTasks.Infrastructure/. ./FlowTasks.Infrastructure/

# Build & Publish le projet API
WORKDIR /src/FlowTasks.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FlowTasks.API.dll"]