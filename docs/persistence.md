# Bloque 2: persistencia y Unit of Work

Implementado en el worktree `DotNetTestMundialRepo-DevCodex`, rama `DevCodex`. Este bloque prepara la escritura transaccional; los Commands, Queries, endpoints del torneo y seed se incorporarán en los siguientes bloques.

## Responsabilidades

- **Domain** conserva sus entidades y reglas sin referencias a EF Core.
- **Application** define `IWriteRepository<TEntity>`, `IUnitOfWork` y errores de persistencia con `Result`. No referencia EF Core.
- **Infrastructure** implementa contexto, mapeos, repositorios, transacciones, migraciones y traducción de errores del proveedor.
- **API** registra Infrastructure mediante `AddInfrastructure` y obtiene la conexión de configuración. No crea ni migra automáticamente la base al iniciar.

Se utiliza EF Core **9.0.19** con proveedor SQL Server, compatible con el destino `net8.0` de la solución. La herramienta local `dotnet-ef` tiene la misma versión. SQLite se utiliza exclusivamente en pruebas.

## Modelo y migración inicial

`InitialPersistence` crea `Teams`, `Players`, `Matches` y `Goals` con identificadores Guid asignados por el dominio, relaciones, índices y restricciones. Las colecciones privadas se materializan mediante sus campos de respaldo. Los eventos de dominio no se mapean ni se vuelven a emitir al leer una entidad persistida.

- Jugador → equipo: obligatorio; se restringe borrar un equipo con jugadores.
- Partido → dos equipos: obligatorios y distintos; se restringe borrar equipos con partidos.
- Gol → partido: obligatorio, con eliminación en cascada cuando se elimina el partido.
- Gol → jugador/equipo: clave foránea compuesta que impide atribuir un gol a un equipo distinto del registrado para su autor. Se restringe borrar un jugador con goles.
- Restricciones para dorsal positivo, minuto 1–120 y coherencia entre estado y presencia de marcador no negativo.
- Índices para equipos participantes, fecha y estado/fecha del partido, además de las relaciones de jugadores y goles.

La pertenencia del equipo del goleador al partido y la igualdad entre marcador y goles se validan en el dominio; no son restricciones entre tablas implementadas en SQL. No se introducen límites de longitud ni unicidad de nombres o dorsales que el dominio no exija.

## Escrituras y transacción

Los repositorios sólo preparan cambios. `Add` incorpora una entidad nueva y su grafo nuevo; no debe usarse con un grafo que contenga entidades existentes. `Update` marca las propiedades escalares de la entidad raíz; los hijos nuevos se agregan explícitamente con su repositorio. `Remove` marca la entidad para eliminarla. Ninguno confirma cambios ni realiza consultas.

El futuro Command debe validar el negocio, preparar todos sus cambios y llamar explícitamente a `IUnitOfWork.CommitAsync` una sola vez. Por ejemplo, agregar los goles nuevos y actualizar el resultado del partido antes de ese commit permite confirmarlos juntos.

Secuencia de `CommitAsync`:

1. Comprueba la cancelación e inicia una transacción de base de datos.
2. Guarda las entidades pendientes sin aceptar todavía sus estados en memoria.
3. Confirma la transacción.
4. Acepta los estados de EF y devuelve `Result<int>` exitoso. El número corresponde a entradas guardadas por EF, no a comandos SQL.

Si falla la operación antes de confirmar, intenta revertir la transacción y descarta el seguimiento de cambios. La limpieza usa un token independiente para que la cancelación de la solicitud no impida el rollback. Un error de relación, restricción o concurrencia reconocido devuelve un `Result` de conflicto; los fallos técnicos no reconocidos y las cancelaciones se propagan para que posteriormente los gestione el middleware.

Las cuatro sobrecargas públicas de `SaveChanges`/`SaveChangesAsync` están bloqueadas. El contexto expone un puente interno utilizado exclusivamente por Unit of Work para invocar el guardado de EF. Los repositorios, Commands y API deben utilizar el contrato de Unit of Work.

`Rollback()` descarta cambios pendientes del contexto. No deshace una transacción previamente confirmada ni restaura los valores de objetos que el llamador todavía conserve: después de un fallo deben descartarse esos objetos y cargarse nuevamente si se reintenta. Contexto y Unit of Work se comparten dentro del mismo scope; no deben utilizarse simultáneamente desde varios hilos ni anidarse transacciones.

No se habilitaron reintentos automáticos: cuando se incorporen, deben coordinarse con la transacción completa del Command. Véase [transacciones de EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

## Errores y límites actuales

`Persistence.ConcurrentChange` representa que EF no pudo actualizar/eliminar la fila esperada. `Persistence.ConstraintViolation` representa conflictos reconocidos de restricciones o relaciones. Ambos son `Conflict`; la API los mapea a HTTP 409. El detalle interno de SQL Server no se devuelve al cliente.

Este bloque no implementa un token de versión para detectar dos actualizaciones simultáneas sobre una fila que sigue existiendo. Esa protección debe resolverse al implementar los Commands de modificación y registro de resultados. La prueba de concurrencia actual comprueba el caso de una fila eliminada por otra operación.

Los eventos continúan en memoria después de un commit exitoso. Su publicación, registro y vinculación con TraceId/CorrelationId siguen pendientes. Las consultas de negocio se implementarán con Dapper; el bloque 3B ya utiliza Dapper para consultar respuestas idempotentes. Las lecturas EF incluidas en las pruebas son comprobaciones de persistencia, no Queries de la aplicación.

## Configuración y comandos

Ejecutar desde la raíz del worktree de DevCodex. `ConnectionStrings: Tournament` está vacío por diseño: configurar una conexión explícita para esta instancia. En PowerShell, este ejemplo usa autenticación de Windows y una base con nombre separado de Developer; debe ajustarse al servidor disponible:

```powershell
$env:ConnectionStrings__Tournament = 'Server=localhost;Database=DotNetTestMundial_DevCodex;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

`TrustServerCertificate=True` corresponde al entorno local del ejemplo. La configuración de despliegue deberá usar la conexión y certificados de ese entorno. No guardar credenciales reales en archivos versionados.

Para mantener los artefactos de compilación fuera del repositorio, que actualmente contiene bin/obj versionados:

```powershell
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = Join-Path $env:TEMP 'DotNetTestMundial-DevCodex-artifacts'
$efIntermediatePath = Join-Path $env:ArtifactsPath 'obj/DotNetTestMundial.Infrastructure'
dotnet tool restore
dotnet restore DotNetTestMundial.sln
dotnet build DotNetTestMundial.sln --configuration Release --no-restore
dotnet test DotNetTestMundial.sln --configuration Release --no-build --no-restore
```

Generar un script de migración para revisión, sin conectarse a SQL Server:

```powershell
dotnet ef migrations script --idempotent --project src/DotNetTestMundial.Infrastructure --startup-project src/DotNetTestMundial.Infrastructure --configuration Release --no-build --msbuildprojectextensionspath $efIntermediatePath --output (Join-Path $env:ArtifactsPath 'initial-persistence.sql')
```

Cuando se haya preparado la instancia dedicada y verificado la conexión, este comando aplica el esquema. **No fue ejecutado durante este bloque.**

```powershell
dotnet ef database update --project src/DotNetTestMundial.Infrastructure --startup-project src/DotNetTestMundial.Infrastructure --configuration Release --no-build --msbuildprojectextensionspath $efIntermediatePath
```

Iniciar la API con la conexión configurada:

```powershell
dotnet run --project src/DotNetTestMundial.Api --configuration Release --no-build --no-restore
```

La API todavía conserva los endpoints de plantilla; arrancar o abrir Swagger no valida el acceso a la base ni completa las operaciones del torneo. El carácter idempotente del script de esquema no implementa el requisito HTTP `Idempotency-Key`.

## Verificación del bloque

- Compilación Release de la solución completa: 0 errores, 0 advertencias.
- 50 pruebas de dominio y 18 de Infrastructure aprobadas.
- Pruebas relacionales SQLite: persistencia después de commit, carga de entidades/colecciones sin eventos nuevos, goles y resultado en una sola transacción, restricciones de relaciones, rollback después de escribir pero antes de confirmar, cancelación, descarte de pendientes, repetición de commit sin nuevos cambios y conflicto al actualizar una fila eliminada.
- Pruebas sin conexión SQL Server: bloqueo del guardado directo, aislamiento de scopes, traducción de fallos, generación del esquema y coincidencia de la migración con el modelo actual.
- Script SQL Server generado desde la migración, sin aplicarlo a una base.
- Arranque de la API con conexión configurada y respuesta HTTP 200 de Swagger; se detuvo el proceso al finalizar la comprobación. No se abrió una conexión SQL Server.

Las pruebas SQLite no sustituyen la ejecución sobre SQL Server: queda pendiente aplicar la migración y verificar las operaciones contra una instancia dedicada. Microsoft explica esta diferencia entre proveedores en su [guía de pruebas de EF Core](https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database).

## Siguiente objetivo

Implementar CQRS en Application empezando por equipos: Commands que usen estos repositorios y commit explícito, lecturas con Dapper y pruebas de casos de uso. Los endpoints POST deberán incorporar la idempotencia persistente requerida antes de considerarlos entregables. Continuarán jugadores, partidos/resultados, posiciones/goleadores, seed y el resto de la entrega.
