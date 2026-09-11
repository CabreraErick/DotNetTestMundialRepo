# Observabilidad

La API asigna un identificador a cada solicitud mediante X-Correlation-ID. Si el cliente envía un valor no vacío de hasta 128 caracteres, la API lo conserva; en otro caso genera un GUID. El mismo valor se devuelve en el encabezado de respuesta y se incorpora al alcance de logging.

RequestObservabilityMiddleware registra método HTTP, ruta, código de respuesta y duración en milisegundos. Si una excepción no controlada atraviesa el middleware, también se registra con el mismo identificador antes de continuar hacia el manejo estándar de ASP.NET Core.

El proveedor de consola integrado utiliza formato JSON, fecha UTC y scopes habilitados. Así los campos de las plantillas de ILogger y el CorrelationId pueden consultarse sin agregar una librería externa.

Las entidades mantienen eventos de dominio mientras existe una operación pendiente. UnitOfWork captura esos eventos, confirma primero la transacción y después los entrega a IDomainEventDispatcher. La implementación LoggingDomainEventDispatcher registra el tipo, fecha y contenido del evento. Finalmente limpia los eventos para que un commit posterior no los repita.

## Comprobación mediante Swagger

1. Inicie la API y abra /swagger.
2. Ejecute cualquier endpoint e indique swagger-observability-001 en el campo X-Correlation-ID.
3. Compruebe que Response headers devuelve X-Correlation-ID: swagger-observability-001.
4. Revise la terminal: el registro JSON de la solicitud debe contener Method, Path, StatusCode, ElapsedMilliseconds y el scope CorrelationId.
5. Ejecute POST /api/teams con un nombre, abreviación e Idempotency-Key nuevos. Después del commit debe aparecer DomainEventType con valor TeamCreatedEvent.
6. Registre el resultado de un partido programado. Después del commit debe aparecer DomainEventType con valor MatchResultRegisteredEvent.
