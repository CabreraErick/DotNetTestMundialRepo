# Sistema Gestor de Fútbol

Sistema web para la gestión de un torneo de fútbol, desarrollado con **.NET 8** y **Next.js**.

La aplicación permite administrar equipos, jugadores, calendario de partidos, goles, resultados, tabla de posiciones y clasificación de goleadores.

## Documentación de entrega y defensa

- [Manual teórico y aplicado](docs/defensa-tecnica.md): conceptos, implementación, decisiones y preguntas de defensa.
- [Demostración y casos de uso](docs/guia-demostracion.md): ejemplos HTTP/PowerShell para el sistema.
- [Rúbrica y cierre de alcance](docs/rubrica-final.md): objetivos implementados y deuda de pruebas aceptada.
- [Evidencias de QA](docs/qa-final.md): resultados históricos y bloqueo local de Application.

El alcance de desarrollo está preparado para integración desde `Desarrollo` hacia `main`, con QA final pospuesto por decisión del responsable. No se eliminan tests ni se desactiva CI; no se declara una ejecución integral final aprobada.

## Arquitectura y tecnologías

La solución utiliza las siguientes tecnologías:

* **.NET 8**
* **ASP.NET Core Web API**
* **Entity Framework Core** para operaciones de escritura
* **Dapper** para operaciones de lectura
* **SQL Server**
* **Next.js** para el frontend

La organización general de la solución se encuentra documentada en el [diagrama de arquitectura](docs/architecture.md).

La aplicación aplica una separación entre operaciones de lectura y escritura:

* Los **Commands** gestionan las operaciones que modifican el estado del sistema utilizando Entity Framework Core.
* Las operaciones se confirman mediante el patrón **Unit of Work**.
* Las **Queries** realizan consultas y generan proyecciones mediante Dapper.
* Las operaciones de negocio utilizan un patrón **Result** para representar resultados exitosos y errores.
* La API transforma estos resultados en las respuestas HTTP correspondientes, incluyendo `400 Bad Request`, `404 Not Found` y `409 Conflict`.

## Preparación del entorno local

### Requisitos

Para ejecutar el proyecto se requiere:

* **.NET SDK 8**
* **SQL Server**
* **Node.js 22.13 o superior**
* **pnpm 11.19.0**
* Herramientas de Entity Framework Core configuradas mediante `dotnet tool restore`

### Configuración de la API

Clone el repositorio y acceda al directorio raíz del proyecto:

```powershell
git clone <repository-url>
cd DotNetTestMundialRepo
```

Configure una cadena de conexión local mediante una variable de entorno.

Ejemplo utilizando autenticación integrada de Windows:

```powershell
$env:ConnectionStrings__Tournament = 'Server=.;Database=DotNetTestMundial;Trusted_Connection=True;TrustServerCertificate=True'
```

> La cadena anterior es únicamente un ejemplo para desarrollo local. No almacene credenciales, contraseñas ni cadenas de conexión de producción dentro del repositorio.

Restaure las herramientas y dependencias:

```powershell
dotnet tool restore
dotnet restore DotNetTestMundial.sln
```

La API aplica las migraciones y carga el seed automáticamente al iniciar (`Database:ApplyMigrations=true`). Si necesita administrarlas manualmente:

```powershell
dotnet ef database update --project src/DotNetTestMundial.Infrastructure --context TournamentDbContext
```

Ejecute la API:

```powershell
dotnet run --project src/DotNetTestMundial.Api
```

Durante el entorno de desarrollo, la documentación interactiva de la API puede consultarse mediante **Swagger** utilizando la URL indicada por ASP.NET Core al iniciar la aplicación.

## Frontend Next.js

Para arrancar y verificar el stack completo de QA con SQL Server real, consulte [procesos de QA](docs/procesos-qa.md): `scripts/Start-Qa.ps1` prepara `.env` local y `scripts/Test-Qa.ps1` ejecuta los controles.

Con la API en ejecución, abra otra terminal y acceda al proyecto frontend:

```powershell
cd frontend
Copy-Item .env.example .env.local
pnpm install
pnpm dev
```

El frontend utiliza la configuración definida en `.env.local` para comunicarse con la API mediante el proxy configurado en Next.js.

> Los archivos `.env.local` y otros archivos que contengan configuración específica del entorno no deben almacenarse en el repositorio.

Por defecto, el servidor de desarrollo de Next.js utiliza:

```text
http://localhost:3000
```

## Ejecución con Docker

Docker Compose ejecuta SQL Server, la API y el frontend en una red privada. Copie
la plantilla de configuración y reemplace la contraseña local antes de iniciar:

```powershell
Copy-Item .env.docker.example .env.docker
docker compose --env-file .env.docker config --quiet
docker compose --env-file .env.docker up --detach --build
docker compose --env-file .env.docker ps
```

El frontend queda disponible en `http://localhost:3000`, Swagger en
`http://localhost:5164/swagger` y SQL Server en el puerto local `14330`. Las
migraciones y los datos iniciales se aplican cuando la API inicia después de que
SQL Server está saludable. Consulte la [guía Docker](docs/docker.md) para la
configuración segura, verificación, persistencia y diagnóstico.

## Pruebas

Para ejecutar la suite completa de pruebas:

```powershell
dotnet test DotNetTestMundial.sln --configuration Release
```

La solución incluye pruebas automatizadas para las capas:

* Domain
* Application
* Infrastructure
* API

Las operaciones relacionadas con SQL Server y los endpoints HTTP también pueden verificarse manualmente mediante herramientas de administración de SQL Server y Swagger.

## Documentación

La documentación técnica y funcional se encuentra en el directorio `docs`:

* [Reglas de dominio](docs/domain-rules.md)
* [Persistencia con EF Core y Unit of Work](docs/persistence.md)
* [Idempotencia HTTP](docs/idempotency.md)
* [Consultas paginadas](docs/team-queries.md)
* [REST de equipos](docs/team-rest.md)
* [REST de jugadores](docs/player-rest.md)
* [Calendario de partidos](docs/match-scheduling.md)
* [Goles y resultados](docs/match-results.md)
* [Posiciones y goleadores](docs/tournament-queries.md)
* [Datos iniciales FIFA 2026](docs/world-cup-2026-seed.md)
* [Observabilidad](docs/observability.md)
* [Frontend Next.js](docs/frontend.md)
* [Docker y Docker Compose](docs/docker.md)
* [QA final](docs/qa-final.md)

## Próximos entregables

El repositorio incluye una colección Postman, pruebas de contrato HTTP y un flujo de
integración continua. La validación final se encuentra en la [guía de QA](docs/qa-final.md).
