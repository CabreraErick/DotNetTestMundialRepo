# Calendario de partidos

El recurso `/api/matches` administra partidos programados con la separación CQRS del sistema. Dapper ejecuta detalle, filtros y paginación; EF Core prepara las escrituras y `IUnitOfWork` realiza el único commit transaccional.

## Operaciones

| Método | Ruta | Resultado exitoso | Errores esperados |
|---|---|---:|---|
| POST | `/api/matches` | 201 | 400, 404, 409 |
| GET | `/api/matches` | 200 | 400 |
| GET | `/api/matches/{id}` | 200 | 404 |
| PUT | `/api/matches/{id}` | 200 | 400, 404, 409 |
| PATCH | `/api/matches/{id}` | 200 | 400, 404, 409 |
| DELETE | `/api/matches/{id}` | 204 | 404, 409 |

POST exige dos equipos existentes y diferentes, una fecha distinta de `default` y `Idempotency-Key`. El partido y la respuesta HTTP reproducible se guardan en la misma transacción.

Ninguno de los dos equipos puede ocupar otro partido no cancelado durante el mismo día calendario. POST, PUT y PATCH consultan un intervalo diario mediante Dapper y devuelven `Matches.ScheduleConflict` con HTTP 409. El trigger `TR_Matches_RejectTeamScheduleConflict` repite la protección dentro de SQL Server para cubrir concurrencia y escrituras externas, comparando local y visitante en ambas direcciones. Los partidos `Cancelled` no ocupan la fecha.

PUT reemplaza local, visitante y fecha. PATCH conserva campos omitidos. Sólo los partidos `Scheduled` se pueden editar. Si un partido ya contiene goles, su fecha todavía puede cambiar, pero sus equipos quedan bloqueados para conservar la coherencia histórica.

DELETE ejecuta `Match.Cancel()` y mantiene la fila con estado `Cancelled`. Repetir la cancelación devuelve 204 sin otra escritura. Un partido `Played` no puede cancelarse y devuelve 409. El frontend presenta esta acción únicamente para filas programadas y solicita confirmación antes de enviarla.

## Consulta del calendario

`GET /api/matches` acepta `teamId`, `status`, `from`, `to`, `pageNumber`, `pageSize`, `sortBy` y `sortDirection`. Los estados válidos son `Scheduled`, `Played` y `Cancelled`. Los campos de orden son `id`, `homeTeamId`, `awayTeamId`, `scheduledAt` y `status`.

La proyección devuelve identificadores y nombres de ambos equipos, fecha, estado, marcador y cantidad de goles. Todos los filtros son parámetros SQL; sólo enums validados seleccionan columnas del `ORDER BY`.

La migración `EnforceQaBusinessRules` agregó la protección persistente inicial. `PreventSameDayMatchesAndSupportTriggers` amplía el conflicto al día completo, excluye cancelados, limpia el solapamiento QA verificado y conserva la validación concurrente.
