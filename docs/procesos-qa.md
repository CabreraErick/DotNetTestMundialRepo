# Procesos del cierre de rúbrica

## Pruebas reales y aislamiento

Una prueba unitaria comprueba una regla con dependencias controladas; una prueba de integración comprueba que las piezas reales colaboran. `SqlServerTournamentTests` ejecuta EF Core, Dapper, migraciones y UnitOfWork contra SQL Server, sin sustituirlos por mocks. Verifica puntos, diferencia de goles, desempates, exclusión de partidos no jugados, páginas y seed repetible.

El fixture crea una base exclusiva `DotNetTestMundial_QA_<GUID>` y solo elimina esa base al terminar. No borra la base de la aplicación. La cuenta necesita permisos de creación de bases; úsela únicamente en el SQL Server local de QA. Sin `QA_SQLSERVER_CONNECTION`, estas pruebas se marcan omitidas: un resultado verde con omisiones no equivale a QA real completo.

## Paginación y consistencia

SQL aplica filtros, cuenta filas y limita resultados con OFFSET/FETCH. Un orden estable necesita desempate por ID. Los parámetros evitan inyección; las columnas de orden provienen de una lista permitida. El marcador completo se consulta aparte: una página vacía o filtrada nunca cambia el resultado del partido. El frontend consume el nuevo sobre, por lo que este cambio requiere actualizar clientes que esperaban un arreglo.

## Observabilidad

CorrelationId agrupa la intención del cliente; TraceId representa la ejecución técnica W3C y respeta `traceparent`. Ambos aparecen en scopes estructurados. Debug registra el inicio, Information termina respuestas exitosas, Warning termina 4xx y Error termina 5xx o registra excepciones. Los eventos se publican después del commit, no antes. No se registran cuerpos ni credenciales. Esto no instala un colector OpenTelemetry ni garantiza entrega duradera de eventos: no hay outbox.

## Arranque reproducible

`Start-Qa.ps1` conserva `.env`, reutiliza `.env.docker` si existe o genera una contraseña local. Nunca publique esos archivos ni cambie la contraseña de un volumen existente sin coordinación. La API aplica migraciones por defecto y si no hay equipos carga el seed conocido; el initializer es infraestructura de arranque, no un Command de negocio. `Database:ApplyMigrations=false` permite desactivarlo. Un despliegue con varias réplicas debería coordinar migraciones en un único proceso.

Desde la raíz, con PowerShell 7, Docker Desktop y las herramientas .NET/Node/pnpm disponibles:

~~~powershell
.\scripts\Start-Qa.ps1
.\scripts\Test-Qa.ps1 -SkipStart
# Después del bootstrap, Compose lee .env automáticamente:
docker compose up --detach --wait
~~~

El segundo script comprueba HTTP, formato, pruebas .NET incluyendo SQL real, lint/build del frontend, Newman y whitespace. Valida además los cuatro reportes TRX: no acepta suites vacías ni omisiones, incluso si el runner devuelve código 0. Los reportes quedan en TestResults (ignorado por Git). Un fallo detiene el proceso con error; el stack queda disponible para revisión. Los diagramas de capas y ejecución están en `architecture.md`.
