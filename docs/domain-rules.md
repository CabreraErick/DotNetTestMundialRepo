# Reglas de dominio

Estas decisiones completan los detalles no especificados en la prueba técnica. El sistema no agrega soporte para múltiples torneos, transferencias, autogoles ni edición de resultados finalizados.

## Identidad y calendario

- Nombre y abreviatura identifican de forma única a cada equipo, sin distinguir mayúsculas o minúsculas según la intercalación de SQL Server.
- Un dorsal queda reservado para un solo jugador dentro del equipo, incluso cuando el jugador está inactivo.
- Un jugador puede activarse o desactivarse sin cambiar su equipo ni liberar su dorsal.
- Un equipo no puede participar en dos partidos no cancelados durante el mismo día calendario, sin importar la hora o si figura como local o visitante.
- Cancelar un partido libera ese día para que sus equipos puedan ser programados nuevamente.

## Resultados y goles

- Un partido comienza como Scheduled y puede pasar a Played o Cancelled.
- Cada gol identifica un jugador activo perteneciente a uno de los equipos participantes.
- El minuto permitido se encuentra entre 1 y 120.
- Un mismo identificador de gol no puede agregarse dos veces.
- Goles diferentes pueden compartir el mismo minuto oficial.
- Antes de finalizar se registran todos los goles.
- El marcador debe coincidir exactamente con la cantidad de goles de cada equipo.
- Un resultado 0–0 sin goles es válido.
- Los intentos inválidos conservan el estado anterior.
- Los partidos finalizados o cancelados no admiten nuevos goles ni nuevos resultados.
- Las tablas de posiciones y goleadores consideran exclusivamente partidos Played.

Application obtiene mediante Dapper los datos necesarios para validar existencia y reconstruir el estado. Domain aplica las invariantes. EF Core prepara goles y resultado, y Unit of Work los confirma dentro de una transacción.

## Errores

Las operaciones de negocio devuelven Result o Result<T>. Los errores contienen código, mensaje y categoría Validation, NotFound o Conflict, sin depender de HTTP.

La API traduce esas categorías a HTTP 400, 404 y 409. Las excepciones quedan reservadas para fallos técnicos o uso incorrecto de contratos.

## Eventos

Crear un equipo genera TeamCreatedEvent. Finalizar correctamente un partido genera MatchResultRegisteredEvent. Los intentos fallidos no producen eventos.

Unit of Work captura los eventos de las entidades rastreadas, confirma primero la transacción y después los entrega a IDomainEventDispatcher. Infrastructure los registra con fecha UTC y el contexto de correlación HTTP. La colección se limpia después del despacho para evitar duplicados.

## Verificación

~~~powershell
dotnet test tests/DotNetTestMundial.Domain.Tests/DotNetTestMundial.Domain.Tests.csproj --configuration Release
~~~

Las reglas también se comprueban desde los handlers de Application, las pruebas de persistencia y los métodos HTTP expuestos en Swagger.
