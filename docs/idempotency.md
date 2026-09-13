# Idempotencia de POST de equipos

`POST /api/teams` exige el encabezado `Idempotency-Key`. Una clave vacía o mayor de 200 caracteres produce HTTP 400. La operación calcula SHA-256 sobre el nombre y nombre corto recibidos.

Antes de escribir, Infrastructure consulta con Dapper la combinación `POST:/api/teams` + clave. Si el hash coincide, devuelve el estado y cuerpo JSON almacenados sin crear otro equipo ni confirmar otra transacción. Si la misma clave corresponde a otro contenido, devuelve HTTP 409.

Una solicitud nueva prepara el equipo y `IdempotencyRecords` en el mismo contexto. Unit of Work confirma ambas filas dentro de una transacción. La clave primaria compuesta impide que dos solicitudes concurrentes ganen: tras perder la restricción, la segunda operación lee y reproduce la respuesta ya confirmada. De este modo no queda un equipo sin respuesta idempotente ni una respuesta sin su equipo.

La respuesta exitosa es HTTP 201, contiene `{ "id": "..." }` y establece `Location: /api/teams/{id}`. Un reintento devuelve el mismo código, cuerpo, identificador y ubicación.

La migración `AddIdempotencyRecords` debe aplicarse antes de utilizar el endpoint. Todavía no se define expiración o purga automática de claves; para esta prueba las respuestas se conservan. La consulta general de equipos, sus filtros y paginación forman el siguiente subbloque Dapper.
