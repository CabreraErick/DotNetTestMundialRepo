# Persistencia con EF Core y Unit of Work

La capa Infrastructure implementa el lado de escritura y lectura del sistema sin trasladar dependencias técnicas a Domain o Application.

## Responsabilidades

- **Domain** contiene entidades y reglas sin referencias a EF Core.
- **Application** define repositorios, Unit of Work, consultas, idempotencia y despacho de eventos.
- **Infrastructure** implementa escrituras EF Core, consultas Dapper, SQL Server, migraciones y logging.
- **API** configura las dependencias y obtiene la conexión desde configuración.

La solución usa EF Core 9.0.19 con SQL Server y destino .NET 8. SQLite se utiliza únicamente en pruebas de integración aisladas.

## Modelo y migraciones

Las migraciones crean Teams, Players, Matches, Goals e IdempotencyRecords. El seed agrega cuatro selecciones, veinte jugadores, seis partidos y tres resultados demostrativos.

Las relaciones y restricciones protegen dorsales, minutos, marcadores, pertenencia de jugadores y referencias entre equipos, partidos y goles. Los eventos de dominio no se almacenan como columnas.

## Escrituras y transacciones

Los repositorios preparan Add, Update y Remove sin confirmar. Cada Command llama una sola vez a IUnitOfWork.CommitAsync.

La secuencia de commit es:

1. Capturar entidades rastreadas y eventos pendientes.
2. Abrir la transacción.
3. Ejecutar el guardado de EF Core sin aceptar todavía los estados.
4. Confirmar la transacción.
5. Aceptar los estados del contexto.
6. Registrar los eventos confirmados y limpiar sus colecciones.

Si la persistencia falla antes del commit, Unit of Work revierte la transacción, limpia eventos y descarta el seguimiento. Restricciones y concurrencia conocidas regresan como Result de conflicto. Los fallos técnicos se propagan.

Las sobrecargas públicas de SaveChanges están bloqueadas para impedir escrituras fuera de Unit of Work.

## CQRS

Los repositorios Dapper ejecutan todas las lecturas de negocio. Devuelven proyecciones y páginas sin usar el seguimiento de EF Core. Los filtros, totales, orden y paginación se calculan en SQL Server con parámetros.

EF Core queda reservado para Commands y Dapper para Queries.

## Configuración

En PowerShell:

~~~powershell
$env:ConnectionStrings__Tournament = 'Server=.;Database=DotNetTestMundial_DevCodex;Trusted_Connection=True;TrustServerCertificate=True'
~~~

Aplicar migraciones:

~~~powershell
dotnet tool restore
dotnet ef database update --project src/DotNetTestMundial.Infrastructure --context TournamentDbContext
~~~

Ejecutar API:

~~~powershell
dotnet run --project src/DotNetTestMundial.Api --urls http://localhost:5164
~~~

Ejecutar pruebas:

~~~powershell
dotnet test DotNetTestMundial.sln --configuration Release
~~~

La migración, el seed y los flujos REST fueron comprobados sobre la base DotNetTestMundial_DevCodex. Las pruebas automatizadas cubren reglas, handlers, transacciones SQLite, esquema SQL Server sin conexión y observabilidad HTTP.

## Límite conocido

El modelo no incluye rowversion. Una actualización simultánea sobre una fila existente puede resolverse con última escritura; las filas eliminadas concurrentemente y las restricciones reconocidas sí se traducen a conflicto.
