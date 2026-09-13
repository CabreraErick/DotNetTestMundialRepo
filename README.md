# Sistema Gestor de Fútbol

API y frontend de torneo desarrollados para la prueba técnica .NET. El sistema administra equipos, jugadores, calendario, goles, resultados, tabla de posiciones y goleadores.

## Arquitectura y tecnologías

La solución utiliza .NET 8, ASP.NET Core, Entity Framework Core para escrituras, Dapper para lecturas, SQL Server y un frontend desacoplado en Next.js. La separación está representada en el [diagrama de arquitectura](docs/architecture.md).

Los Commands preparan escrituras con EF Core y las confirman mediante Unit of Work. Las Queries obtienen proyecciones con Dapper. Los resultados de negocio usan Result y la API los traduce a HTTP 400, 404 o 409.

## Preparación local

Requisitos:

- SDK de .NET 8.
- SQL Server accesible mediante autenticación de Windows o una cadena equivalente.
- Herramientas de Entity Framework restauradas con dotnet tool restore.

Desde PowerShell, configure una base exclusiva para DevCodex:

~~~powershell
cd C:\Users\Erick\source\GitHub\DotNetTestMundialRepo-DevCodex
$env:ConnectionStrings__Tournament = 'Server=.;Database=DotNetTestMundial_DevCodex;Trusted_Connection=True;TrustServerCertificate=True'
dotnet tool restore
dotnet restore DotNetTestMundial.sln
dotnet ef database update --project src/DotNetTestMundial.Infrastructure --context TournamentDbContext
dotnet run --project src/DotNetTestMundial.Api --urls http://localhost:5164
~~~

Swagger estará disponible en http://localhost:5164/swagger.

## Frontend Next.js

Con la API activa, ejecute desde otra terminal PowerShell:

~~~powershell
cd C:\Users\Erick\source\GitHub\DotNetTestMundialRepo-DevCodex\frontend
Copy-Item .env.example .env.local
pnpm install
pnpm dev
~~~

El frontend estará disponible en http://localhost:3000. Requiere Node.js 20.9 o superior y usa la variable privada `API_BASE_URL` mediante un proxy de Next.js.

## Pruebas

~~~powershell
dotnet test DotNetTestMundial.sln --configuration Release
~~~

La solución contiene 189 pruebas de Domain, Application, Infrastructure y API. SQL Server y los métodos HTTP también se verificaron manualmente mediante SSMS y Swagger.

## Documentación funcional

- [Reglas de dominio](docs/domain-rules.md)
- [Persistencia con EF Core y Unit of Work](docs/persistence.md)
- [Idempotencia HTTP](docs/idempotency.md)
- [Consultas paginadas](docs/team-queries.md)
- [REST de equipos](docs/team-rest.md)
- [REST de jugadores](docs/player-rest.md)
- [Calendario de partidos](docs/match-scheduling.md)
- [Goles y resultados](docs/match-results.md)
- [Posiciones y goleadores](docs/tournament-queries.md)
- [Datos iniciales FIFA 2026](docs/world-cup-2026-seed.md)
- [Observabilidad](docs/observability.md)
- [Frontend Next.js](docs/frontend.md)

Quedan como entregables posteriores Docker, la colección Postman y la revisión final.
