# Responsabilidad: compila y empaqueta la API .NET 8 en una imagen de ejecución.
# Relación: expone el backend que utilizarán el frontend Next.js y SQL Server mediante Docker Compose.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY ["global.json", "./"]
COPY ["src/DotNetTestMundial.Domain/DotNetTestMundial.Domain.csproj", "src/DotNetTestMundial.Domain/"]
COPY ["src/DotNetTestMundial.Application/DotNetTestMundial.Application.csproj", "src/DotNetTestMundial.Application/"]
COPY ["src/DotNetTestMundial.Infrastructure/DotNetTestMundial.Infrastructure.csproj", "src/DotNetTestMundial.Infrastructure/"]
COPY ["src/DotNetTestMundial.Api/DotNetTestMundial.Api.csproj", "src/DotNetTestMundial.Api/"]

RUN dotnet restore "src/DotNetTestMundial.Api/DotNetTestMundial.Api.csproj"

FROM restore AS build

COPY ["src/", "src/"]

RUN dotnet publish "src/DotNetTestMundial.Api/DotNetTestMundial.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "DotNetTestMundial.Api.dll"]