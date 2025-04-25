FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/NavigationPlatform.API/NavigationPlatform.API.csproj", "NavigationPlatform.API/"]
COPY ["src/NavigationPlatform.Application/NavigationPlatform.Application.csproj", "NavigationPlatform.Application/"]
COPY ["src/NavigationPlatform.Domain/NavigationPlatform.Domain.csproj", "NavigationPlatform.Domain/"]
COPY ["src/NavigationPlatform.Infrastructure/NavigationPlatform.Infrastructure.csproj", "NavigationPlatform.Infrastructure/"]
RUN dotnet restore "NavigationPlatform.API/NavigationPlatform.API.csproj"
COPY src/ .
WORKDIR "/src/NavigationPlatform.API"
RUN dotnet build "NavigationPlatform.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NavigationPlatform.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NavigationPlatform.API.dll"] 