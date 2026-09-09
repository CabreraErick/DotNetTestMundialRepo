# REST de jugadores

El recurso `/api/players` sigue la misma separación CQRS utilizada en equipos: Dapper resuelve detalle y colecciones, mientras EF Core prepara las escrituras y `IUnitOfWork` realiza el único commit transaccional.

## Operaciones

| Método | Ruta | Resultado exitoso | Errores esperados |
|---|---|---:|---|
| POST | `/api/players` | 201 | 400, 404, 409 |
| GET | `/api/players` | 200 | 400 |
| GET | `/api/players/{id}` | 200 | 404 |
| PUT | `/api/players/{id}` | 200 | 400, 404, 409 |
| PATCH | `/api/players/{id}` | 200 | 400, 404, 409 |
| DELETE | `/api/players/{id}` | 204 | 404, 409 |

POST exige `Idempotency-Key`, comprueba mediante Dapper que el equipo exista y guarda al jugador junto con la respuesta reproducible dentro de la misma transacción. Repetir la misma clave y solicitud devuelve exactamente el `201` original; reutilizar la clave con otro cuerpo devuelve `409`.

PUT reemplaza nombre y dorsal. PATCH conserva el campo omitido y `{}` devuelve `Players.PatchEmpty`. En ambos casos permanecen inmutables el equipo y el estado activo.

DELETE ejecuta una baja lógica mediante `Player.Deactivate()`. El registro continúa disponible para preservar goles y estadísticas históricas, pero aparece con `isActive=false`. Repetir DELETE devuelve `204` sin emitir otra escritura.

## Consulta paginada

`GET /api/players` acepta:

- `search`: fragmento literal del nombre.
- `teamId`: equipo opcional.
- `isActive`: estado opcional.
- `pageNumber`: entero mayor que cero.
- `pageSize`: de 1 a 100.
- `sortBy`: `id`, `teamId`, `name`, `jerseyNumber` o `isActive`.
- `sortDirection`: `asc` o `desc`.

Application convierte los campos de orden a enums antes de llamar Infrastructure. Dapper parametriza filtros, búsqueda, desplazamiento y tamaño; únicamente los enums validados seleccionan las columnas de `ORDER BY`. La respuesta mantiene el contrato `data`, `pageNumber`, `pageSize`, `totalRecords` y `totalPages`.

El bloque usa el esquema `Players` ya contenido en `InitialPersistence`, por lo que no requiere una migración adicional.
