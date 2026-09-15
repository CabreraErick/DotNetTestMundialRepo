# Goles y resultados

Los subrecursos de `/api/matches` completan la escritura del desarrollo de un partido. Dapper lee el partido, el jugador y los goles; las entidades `Goal` y `Match` aplican las reglas; EF Core prepara las escrituras; `IUnitOfWork` realiza el único commit.

## Operaciones

| Método | Ruta | Resultado exitoso | Errores esperados |
|---|---|---:|---|
| POST | `/api/matches/{id}/goals` | 201 | 400, 404, 409 |
| GET | `/api/matches/{id}/goals` | 200 | 400, 404 |
| PUT | `/api/matches/{id}/result` | 200 | 400, 404, 409 |

POST exige `Idempotency-Key`, un jugador activo y un minuto entre 1 y 120. El jugador debe pertenecer al equipo local o visitante y el partido debe conservar el estado `Scheduled`. El gol y la respuesta HTTP reproducible se guardan juntos. Repetir la misma clave y solicitud devuelve exactamente el mismo 201; cambiar partido, jugador o minuto con esa clave devuelve 409.

GET devuelve un sobre paginado: `data`, `pageNumber`, `pageSize`, `totalRecords`, `totalPages`, `homeGoals` y `awayGoals`. Acepta `pageNumber` (desde 1), `pageSize` (1..100), `teamId`, `search`, `sortBy` (Minute, PlayerName, TeamName, Id) y `sortDirection` (Asc, Desc). El identificador desempata el orden. Los totales del marcador incluyen todos los goles del partido, independientemente del filtro o página. Esta consulta usa Dapper y no rastrea entidades con EF Core.

PUT resultado reconstruye el agregado con el partido y todos sus goles. El marcador indicado debe coincidir exactamente con la cantidad de goles de cada equipo; una diferencia devuelve `Match.ScoreMismatch`. Al concluir, el estado pasa a `Played`; desde ese momento se rechazan nuevos goles, cambios de calendario y cancelación.

Las tablas `Matches`, `Goals`, `Players` e `IdempotencyRecords` ya contienen las relaciones necesarias. Este bloque no requiere migración.
