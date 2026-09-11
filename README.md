# Sistema Gestor de Fútbol

API y aplicación de torneo desarrolladas para la prueba técnica .NET. El backend administra equipos, jugadores, calendario, goles, resultados, tabla de posiciones y goleadores.

## Arquitectura y tecnologías

La solución utiliza .NET 8, ASP.NET Core, Entity Framework Core para escrituras, Dapper para lecturas y SQL Server. La separación entre Domain, Application, Infrastructure y API está representada en el [diagrama de arquitectura](docs/architecture.md).

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

## Pruebas

~~~powershell
dotnet test DotNetTestMundial.sln --configuration Release
~~~

La solución contiene pruebas de Domain, Application, Infrastructure y API. SQL Server y los métodos HTTP también se verificaron manualmente mediante SSMS y Swagger.

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

Quedan como entregables posteriores el frontend Next.js, Docker y la colección Postman.
