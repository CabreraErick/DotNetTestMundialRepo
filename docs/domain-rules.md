# Reglas de dominio y alcance inicial

Estas decisiones completan los detalles no especificados en la prueba técnica. No agregan soporte para múltiples torneos, transferencias de jugadores, autogoles ni edición de resultados finalizados.

## Resultados y goles

- Un partido comienza como `Scheduled` y puede pasar a `Played` o `Cancelled`.
- Cada gol debe identificar un jugador activo de uno de los dos equipos participantes. `Goal.Create` recibe un `Player`, y captura su identidad y equipo; `Match.AddGoal` valida la participación del equipo.
- La futura capa de aplicación debe cargar ese jugador desde el repositorio. No debe fabricar un jugador a partir de identificadores proporcionados por el cliente. El dominio no verifica existencia en base de datos.
- El minuto admitido es de 1 a 120, conservando el alcance original. No se representa por separado el tiempo de descuento.
- Un mismo identificador de gol no se agrega dos veces. Dos goles distintos del mismo jugador en el mismo minuto sí son posibles. La idempotencia de solicitudes HTTP deberá evitar duplicados por reintentos con identificadores nuevos.
- Antes de finalizar se agregan todos los goles. El marcador local y visitante debe coincidir exactamente con la cantidad de goles de cada equipo. Un 0–0 sin goles es válido.
- Un intento inválido de finalizar conserva estado, marcador y eventos. Puede corregirse mientras el partido siga programado.
- Los partidos finalizados o cancelados no admiten nuevos goles ni resultados. Las estadísticas futuras deberán considerar exclusivamente partidos finalizados.
- El futuro caso de uso de registro guardará goles y resultado en una sola transacción mediante Unit of Work. El bloque 2 ya proporciona y prueba esa persistencia atómica; falta conectarla con el Command.

## Errores

Las operaciones de negocio devuelven `Result` o `Result<T>`. Un fallo contiene código, mensaje y categoría (`Validation`, `NotFound`, `Conflict`) sin depender de HTTP. La API deberá mapear dichas categorías a 400, 404 y 409, y los éxitos a 200/201 según la operación.

El consumidor debe comprobar `IsSuccess` antes de acceder a `Value`. Las excepciones se reservan para uso incorrecto del contrato por el programador (por ejemplo, leer Value de un fallo o definir un error sin mensaje), no para entradas inválidas del negocio.

## Eventos

Crear un equipo genera un `TeamCreatedEvent`; finalizar correctamente un partido genera un `MatchResultRegisteredEvent`. Los eventos tienen fecha UTC y quedan en una colección tipada. Los intentos fallidos no emiten eventos.

La publicación, los logs y la vinculación con TraceId/CorrelationId se implementarán con Application/Infrastructure y Unit of Work. Este bloque no afirma completar todavía el requisito de observabilidad. `ClearDomainEvents` permite limpiar eventos cuando el procesamiento correspondiente haya concluido.

## Compatibilidad y verificación

Las fábricas de entidades ahora devuelven `Result<T>` y las operaciones mutables `Result`. `Goal.Create` recibe un `Player` en lugar de un Guid de jugador. No existen consumidores de negocio en Application/Api en el commit base `a87240c`; cualquier nuevo consumidor deberá manejar estos contratos.

Pruebas del bloque:

```powershell
dotnet test tests/DotNetTestMundial.Domain.Tests/DotNetTestMundial.Domain.Tests.csproj --configuration Release
```

El bloque 2 incorpora EF Core, migraciones y Unit of Work transaccional, descritos en [persistencia](persistence.md). Su integración con Commands y la ejecución sobre una instancia dedicada de SQL Server siguen pendientes.

Pendientes del PDF: posiciones/puntos/diferencia de gol, CQRS, Dapper, REST e idempotencia, observabilidad, seed, Next.js, Docker, Postman, diagrama y las pruebas correspondientes. La idempotencia de una asociación en memoria no satisface la idempotencia HTTP exigida.
