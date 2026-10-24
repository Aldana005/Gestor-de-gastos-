# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Stage 2: Build image with SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for layer caching during restore
COPY ["Gastito.Entities/Gastito.Entities.csproj", "Gastito.Entities/"]
COPY ["Gastito.DataAccess/Gastito.DataAccess.csproj", "Gastito.DataAccess/"]
COPY ["Gastito.BusinessLogic/Gastito.BusinessLogic.csproj", "Gastito.BusinessLogic/"]
COPY ["Gastito.Presentation/Gastito.Presentation.csproj", "Gastito.Presentation/"]
COPY ["Gastito.Api/Gastito.Api.csproj", "Gastito.Api/"]

# Restore dependencies
RUN dotnet restore "Gastito.Api/Gastito.Api.csproj"

# Copy full solution source code
COPY . .

# Build application
WORKDIR "/src/Gastito.Api"
RUN dotnet build "Gastito.Api.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "Gastito.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Gastito.Api.dll"]
