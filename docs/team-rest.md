# REST de equipos

El recurso `api/teams` ofrece POST, GET, PUT, PATCH y DELETE. `TeamsController` se limita a convertir HTTP en Commands o Queries y a traducir `Result` a códigos HTTP; las reglas se ejecutan en Domain y la coordinación vive en Application.

## Operaciones

| Método | Ruta | Resultado exitoso | Errores esperados |
|---|---|---:|---|
| POST | `/api/teams` | 201 | 400, 409 |
| GET | `/api/teams` | 200 | 400 |
| GET | `/api/teams/{id}` | 200 | 404 |
| PUT | `/api/teams/{id}` | 200 | 400, 404, 409 |
| PATCH | `/api/teams/{id}` | 200 | 400, 404, 409 |
| DELETE | `/api/teams/{id}` | 204 | 404, 409 |

PUT exige `name` y `shortName` válidos y reemplaza ambos valores. PATCH acepta uno o ambos campos; un cuerpo `{}` produce `Teams.PatchEmpty`. DELETE devuelve conflicto si SQL Server impide eliminar un equipo relacionado con jugadores o partidos.

Nombre y abreviatura son únicos. POST, PUT y PATCH consultan primero mediante Dapper y devuelven `Teams.NameAlreadyExists` o `Teams.ShortNameAlreadyExists` con HTTP 409. Los índices únicos `UX_Teams_Name` y `UX_Teams_ShortName` mantienen la garantía ante concurrencia y clientes externos.

## Flujo de una escritura

1. API construye el Command.
2. El handler obtiene con Dapper la proyección escalar actual y responde `Teams.NotFound` si no existe.
3. `Team.Restore` reconstruye el agregado sin emitir un evento de creación.
4. `Team.Update` aplica las mismas invariantes del dominio para PUT y PATCH.
5. `IWriteRepository<Team>` marca con EF Core la actualización o eliminación.
6. `IUnitOfWork.CommitAsync` abre la transacción, ejecuta un único `SaveChanges`, confirma y traduce restricciones o concurrencia a un `Result` de conflicto.

Esta separación conserva la regla CQRS de la prueba: Dapper realiza todas las lecturas y EF Core todas las escrituras. Los handlers no llaman `SaveChanges` directamente.

El modelo todavía no contiene una columna `rowversion`; por eso dos actualizaciones válidas simultáneas se resuelven con la última escritura. `DbUpdateConcurrencyException` ya se traduce a HTTP 409 cuando EF puede detectarla, y el control optimista con versión puede añadirse en un bloque posterior si la prueba exige detectar actualizaciones concurrentes del mismo registro.
