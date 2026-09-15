# Defensa técnica: Sistema Gestor de Fútbol

## 1. Resumen para el examinador

El sistema administra un Mundialito corporativo: equipos, integrantes, calendario, goles, resultados y clasificaciones. La API ASP.NET Core usa .NET 8; Next.js/React/TypeScript presenta los flujos. SQL Server conserva estado; EF Core escribe y Dapper consulta. La solución se ejecuta con Docker Compose.

La prioridad fue mantener reglas coherentes, escrituras atómicas y consultas eficientes. No se implementaron múltiples torneos, autenticación, transferencias, autogoles, edición de resultados finalizados ni un bus de eventos externo. La sofisticación visual no forma parte de la evaluación, pero el frontend funcional sí.

Estado de entrega: alcance implementado con cierre de pruebas pendiente por decisión del responsable. Consulte [rúbrica final](rubrica-final.md) para evidencias, interpretaciones y reservas. No se declara una nota ni QA integral final aprobado.

## 2. Mapa de lectura

| Tema | Documento específico |
|---|---|
| Capas y diagramas de ejecución | [architecture.md](architecture.md) |
| Reglas y estados del torneo | [domain-rules.md](domain-rules.md) |
| EF, transacciones y esquema | [persistence.md](persistence.md) |
| POST e idempotencia | [idempotency.md](idempotency.md) |
| Equipos | [team-rest.md](team-rest.md), [team-queries.md](team-queries.md) |
| Jugadores | [player-rest.md](player-rest.md) |
| Calendario y resultados | [match-scheduling.md](match-scheduling.md), [match-results.md](match-results.md) |
| Posiciones y goleadores | [tournament-queries.md](tournament-queries.md) |
| Frontend y Docker | [frontend.md](frontend.md), [docker.md](docker.md) |
| Seed | [world-cup-2026-seed.md](world-cup-2026-seed.md) |
| Trazas y QA | [observability.md](observability.md), [procesos-qa.md](procesos-qa.md) |
| Guion práctico | [guia-demostracion.md](guia-demostracion.md) |

## 3. Clean Architecture e inversión de dependencias

Clean Architecture separa reglas de negocio de mecanismos externos. La dirección de dependencias apunta hacia las capas internas; no se trata solamente de crear cuatro carpetas.

- Domain contiene `Team`, `Player`, `Match`, `Goal`, `Result`, errores y eventos. No depende de HTTP ni persistencia.
- Application contiene casos de uso y contratos como `IWriteRepository<T>`, `IMatchReadRepository`, `IUnitOfWork` e `IIdempotencyStore`. Depende de Domain.
- Infrastructure implementa contratos usando EF Core, Dapper, SQL Server y logging.
- API recibe HTTP, convierte requests en Commands/Queries, traduce Result y registra servicios en `Program.cs`. La referencia a Infrastructure pertenece a la composición del host, no a reglas del controller.

Inversión de dependencias significa que Application necesita una abstracción definida por ella, no un repositorio SQL concreto. El contenedor de inyección conecta esa abstracción con la implementación en ejecución. Los servicios scoped de escritura comparten el DbContext de la solicitud; eso permite confirmar recurso y registro idempotente juntos.

Ejemplo aplicado: `CreateTeamCommandHandler` recibe `IWriteRepository<Team>` y `IUnitOfWork`; no construye un SqlConnection ni llama SaveChanges. Para cambiar el mecanismo de almacenamiento se implementan los puertos, sin trasladar reglas a la API. Eso no significa que cambiar de motor SQL sea gratis: migraciones, triggers y SQL Dapper son específicos de SQL Server.

## 4. CQRS y handlers

CQRS separa las responsabilidades de modificar estado y de obtener información. No requiere bases distintas, mensajería ni event sourcing. Este proyecto usa una misma base con dos mecanismos de acceso.

Un Command expresa intención: crear equipo, programar partido o registrar resultado. Un Query expresa una lectura: listar partidos o consultar posiciones. Los handlers son clases explícitas; no hay dependencia obligatoria de MediatR ni cola asíncrona.

~~~text
Escritura: HTTP -> Command handler -> reglas Domain -> EF -> UnitOfWork -> SQL
Lectura:   HTTP -> Query handler -> contrato de lectura -> Dapper -> SQL -> DTO
~~~

Los Commands también leen para validar existencia y estado; esas lecturas usan Dapper. La distinción es el propósito del caso de uso, no que un Command tenga prohibido consultar. `Restore` reconstruye escalares persistidos sin emitir eventos de creación; después se aplican métodos de negocio y EF adjunta la escritura.

DTO es una proyección orientada al consumidor, no una entidad de dominio. Por ejemplo, detalle de partido contiene nombres de ambos equipos sin exponer el grafo interno de EF.

## 5. Entidades, agregados e invariantes

Una entidad tiene identidad estable aunque cambien sus atributos. Una invariante debe conservarse después de cada operación válida. Setters privados y fábricas `Create` evitan que el cliente establezca directamente un estado arbitrario.

`Match` agrupa las reglas entre participantes, goles, marcador y estado. Comienza Scheduled; puede finalizar Played o cancelarse Cancelled. El resultado debe coincidir con goles registrados; no se admiten goles de equipos ajenos ni cambios de participantes cuando ya existen goles. No es un modelo de event sourcing: el estado vive en tablas y los eventos son observación posterior.

Validar en varios niveles cumple responsabilidades diferentes:

1. API valida estructura/tipos del request.
2. Application valida existencia y unicidad mediante consultas.
3. Domain valida reglas de las entidades.
4. SQL Server protege restricciones incluso bajo carreras o escrituras externas.

Ejemplo: dos solicitudes pueden comprobar simultáneamente que un dorsal está disponible. El índice único `(TeamId, JerseyNumber)` impide confirmar ambos. La validación previa mejora el mensaje, pero no sustituye la restricción.

## 6. Result Pattern y errores

`Result` expresa éxito sin valor; `Result<T>` éxito con valor o un fallo esperado. `Error` contiene código, mensaje y categoría. No se usan excepciones para representar nombre vacío, recurso inexistente o conflicto de calendario.

Ejemplo simplificado con los contratos reales:

~~~csharp
var creation = Team.Create("Equipo Defensa", "DEF");
if (creation.IsFailure)
    return Result<CreateTeamOutcome>.Failure(creation.Error!);
var team = creation.Value;
~~~

Hay que comprobar éxito antes de acceder a Value: hacerlo sobre un fallo lanza una excepción de programación. `Restore` también puede lanzar si recibe estado persistido imposible. Fallos técnicos inesperados pueden propagarse; el patrón no exige ocultar toda excepción del proceso.

| Categoría / situación | HTTP | Significado aplicado |
|---|---:|---|
| Lectura/actualización correcta | 200 | Recurso o página disponible |
| Creación | 201 | Cuerpo con ID; Location apunta al recurso creado |
| Baja/cancelación correcta | 204 | Sin cuerpo |
| Validation | 400 | Página inválida, minuto fuera de rango, clave ausente |
| NotFound | 404 | Equipo, jugador o partido inexistente |
| Conflict | 409 | Identidad duplicada, calendario ocupado, resultado incoherente |
| Fallo técnico no controlado | 500 | No equivale a un conflicto de negocio |

Los errores de model binding de ASP.NET pueden tener formato ProblemDetails, distinto del Error de negocio; el cliente frontend contempla ambos. Consulte los códigos concretos de cada módulo, no suponga una única forma para toda respuesta HTTP.

## 7. EF Core, repositorios y UnitOfWork

EF Core es un ORM: mapea entidades a tablas y realiza escrituras parametrizadas. En este proyecto no se usa para consultas de negocio. Las configuraciones definen claves, longitudes, relaciones, índices y restricciones.

Un repositorio de escritura prepara Add/Update/Remove; no confirma por separado. UnitOfWork representa una unidad atómica de cambios y centraliza el commit explícito:

1. Captura entidades rastreadas y sus eventos.
2. Abre transacción.
3. Ejecuta `SaveFromUnitOfWorkAsync` una vez.
4. Confirma y acepta cambios.
5. Despacha eventos confirmados y limpia sus colecciones.

Ante error, revierte la transacción y limpia tracking/eventos pendientes. Restricciones reconocidas se traducen a Result; errores técnicos no reconocidos se propagan. El cleanup usa un token independiente si la solicitud ya fue cancelada.

Atomicidad aplicada: al crear un equipo se guardan Team e IdempotencyRecord dentro del mismo commit. No debe quedar la respuesta reproducible sin el equipo, ni el equipo confirmado sin su registro idempotente.

Transacción no significa que todas las consultas Dapper anteriores estén dentro del mismo snapshot. Existe tiempo entre lectura y escritura; restricciones de SQL cubren identidad/calendario, pero no hay rowversion para detectar toda actualización perdida. La solución no promete serialización completa de todas las operaciones concurrentes.

La tabla Matches tiene triggers; EF desactiva el OUTPUT incompatible mediante su configuración. Las migraciones versionan cambios de esquema. Migración aplicada no equivale a test funcional aprobado.

## 8. Dapper, SQL y proyecciones

Dapper es un micro-ORM: ejecuta SQL y mapea resultados a DTOs, sin tracking de entidades. SQL explícito facilita joins y agregados, a cambio de mantener consultas específicas del motor.

`SqlTournamentReadRepository` usa CTEs para representar cada lado de un partido y agregar estadísticas. LEFT JOIN conserva equipos que aún no jugaron. ROW_NUMBER calcula posición antes de paginar. QueryMultiple obtiene total y datos mediante un comando con varios result sets; eso reduce viajes, pero no garantiza un snapshot único entre SELECTs bajo cualquier aislamiento.

Los valores de filtros son parámetros. Columnas/direcciones de ORDER BY no se toman del texto arbitrario del cliente: Application convierte entradas a enums y Infrastructure selecciona fragmentos de una lista permitida. Para búsqueda literal LIKE se escapan comodines `%`, `_` y `[`.

No se implementó un benchmark ni se afirma un rendimiento cuantificado. Consultas explícitas e índices son una base de eficiencia, no sustituyen medir planes reales con mayor volumen. Un LIKE con prefijo `%` puede requerir escaneo.

## 9. Paginación, filtros y orden estable

La paginación limita filas en la base, no después de traer toda la colección. Los handlers aceptan página desde 1 y tamaño de 1 a 100:

~~~text
offset = (pageNumber - 1) * pageSize
totalPages = techo(totalRecords / pageSize)
~~~

SQL usa OFFSET/FETCH y el ID desempata cuando varias filas comparten el campo de orden. Esto evita un orden ambiguo, aunque inserciones concurrentes pueden desplazar filas entre páginas: no se implementó cursor/keyset ni snapshot entre solicitudes.

Ejemplo de página de goles:

~~~json
{
  "data": [],
  "pageNumber": 99,
  "pageSize": 10,
  "totalRecords": 3,
  "totalPages": 1,
  "homeGoals": 2,
  "awayGoals": 1
}
~~~

Es una respuesta ilustrativa para una página fuera de rango. El marcador usa todo el partido; totalRecords cuenta goles de la selección filtrada. Mostrar solo goles del local no convierte un resultado 2-1 en 2-0. La reconstrucción completa de goles para validar el agregado es una operación interna de un Command, no un GET de colección sin paginar.

## 10. REST e idempotencia

REST modela recursos y usa semántica HTTP. GET lee; POST crea; PUT reemplaza campos editables; PATCH aplica campos suministrados; DELETE elimina o ejecuta la baja definida por el recurso. La API no implementa JSON Patch RFC 6902: PATCH usa un DTO de campos opcionales y rechaza `{}`.

Idempotencia significa que repetir una operación no añade efectos diferentes. No exige que todos los intentos devuelvan el mismo estado HTTP. El requisito POST sí añade una garantía explícita de respuesta original.

Los POST de equipos, jugadores, partidos y goles requieren Idempotency-Key. El mecanismo:

1. Valida clave no vacía y longitud máxima 200.
2. Calcula hash SHA-256 del payload definido por el handler.
3. Busca operación + clave mediante Dapper.
4. Si el hash coincide, reproduce cuerpo/estado original sin escribir de nuevo.
5. Si cambió el payload, devuelve 409.
6. Si es nueva, prepara entidad y respuesta persistida con EF, y confirma juntas.

La restricción compuesta protege la carrera por clave; el perdedor puede leer la respuesta ganadora tras rollback. Hash no es cifrado ni autenticación. La huella de equipos usa los valores recibidos serializados: no se promete equivalencia semántica entre payloads con diferente espacio/capitalización.

El cliente conserva la clave de un intento fallido para reintentar el mismo cuerpo; al editar datos o comenzar otra intención debe usar una nueva. No debe reutilizar una única clave para todas las creaciones.

Límites: no hay expiración/purga automática; no hay versionado general del payload idempotente; no se afirma cobertura de carga concurrente para todos los endpoints. Repetir PUT resultado tras Played devuelve conflicto sin alterar el resultado, no un replay 200.

## 11. Cálculos del torneo

Solo partidos Played alimentan estadísticas oficiales. Cada partido aporta una fila para el local y otra para el visitante.

~~~text
puntos = 3 * ganados + empatados
diferencia = golesAFavor - golesEnContra
jugados = ganados + empatados + perdidos
~~~

Ejemplo: un equipo gana 2-1 y empata 0-0. Tiene 2 jugados, 1 ganado, 1 empate, 0 perdidos, GF 2, GC 1, diferencia +1 y 4 puntos.

Desempate: puntos descendentes, diferencia descendente, GF descendentes y nombre/ID para orden determinista. Goleadores agrupa goles por jugador/equipo en partidos Played; no desaparecen goles históricos al desactivar al jugador. Partidos Scheduled con goles aún pendientes no modifican posiciones ni goleadores oficiales.

La posición se calcula sobre los filtros aplicados antes de paginar. Cambiar sortBy para presentar nombres no cambia la posición calculada dentro de esa selección. Si se requiere conservar rango global al filtrar, debe cambiarse el lugar del filtro en la consulta.

## 12. Observabilidad, tracing y eventos

Observabilidad permite entender qué ocurrió sin reproducir cada caso manualmente. Aquí incluye logs estructurados y trazabilidad por solicitud; no un backend completo de métricas/trazas.

ILogger registra campos de plantilla en JSON, timestamps UTC y scopes. Un scope agrega contexto a logs posteriores dentro de la operación. El middleware mide duración con Stopwatch, usando reloj apropiado para intervalos.

| Nivel | Uso implementado |
|---|---|
| Debug | Inicio HTTP; requiere habilitar ese nivel para verlo |
| Information | Finalización con estado menor de 400 y eventos confirmados |
| Warning | Finalización 4xx |
| Error | Finalización 5xx y excepciones no controladas |

CorrelationId es la identidad suministrada por el cliente o generada para la solicitud; acepta valores no vacíos hasta 128 caracteres. TraceId es identidad técnica W3C de 32 hexadecimales. Activity de ASP.NET conserva traceparent recibido; no se sustituye por CorrelationId. Se devuelven X-Correlation-ID y X-Trace-ID; Next.js transmite traceparent/tracestate cuando los recibe.

El navegador habitual genera CorrelationId por solicitud; no se instaló un SDK de tracing que genere spans automáticamente en cada acción UI. Se demostró propagación suministrando traceparent explícito al proxy.

`TeamCreatedEvent` y `MatchResultRegisteredEvent` son hechos de dominio. Se crean al realizar la operación válida, se capturan en UnitOfWork y se registran después del commit. No hay bus externo, outbox ni reintento duradero del dispatcher: si el proceso cae después de confirmar y antes de registrar el evento, puede perderse observación. Un fallo de despacho no revierte SQL ya confirmado.

No se registran contraseñas ni cuerpos de solicitud desde el middleware. Los eventos incluyen nombres de equipos como datos de negocio; cualquier ampliación futura con datos personales debe revisar su política de logging.

## 13. Next.js, React y desacoplamiento

Next.js App Router organiza páginas y Route Handlers. React mantiene estado de formularios, filtros, página, errores y solicitudes en progreso. TypeScript describe DTOs para ayudar durante compilación; no reemplaza validación runtime en API.

El cliente usa `apiRequest` y accede a `/api/backend/...`. El Route Handler del servidor consulta API_BASE_URL; así la dirección interna de Docker no se expone al navegador y las solicitudes del navegador son del mismo origen. El proxy no implementa reglas de torneo ni es una capa de autorización.

Los componentes muestran errores de negocio/ProblemDetails, contemplan 204 sin JSON y conservan clave idempotente para reintentos. El detalle paginado usa homeGoals/awayGoals completos; un contador de secuencia evita que respuestas antiguas de filtros sobrescriban las nuevas.

No hay tests automatizados de navegación/formularios completos en navegador. Lint y build validan código/compilación; HTTP verifica contratos, no toda experiencia UI.

## 14. Docker, configuración y seed

Dockerfile define una imagen; contenedor es su instancia en ejecución. Los builds multi-stage separan compilación y runtime, y las imágenes finales de API/frontend usan usuarios sin privilegios. Compose conecta tres servicios mediante red privada y conserva SQL en un volumen.

Healthchecks y depends_on service_healthy ordenan el arranque: SQL acepta conexiones, API inicia y luego frontend. `/health` es salud del host ASP.NET; no agrega una prueba SQL permanente de readiness. Un estado healthy no garantiza que cada caso de negocio pase.

`Start-Qa.ps1` prepara `.env`: conserva uno existente, reutiliza `.env.docker` o genera contraseña fuerte desde plantilla. Ambos archivos privados están ignorados y no deben publicarse. Compose usa variables para puertos y conexión; puertos por defecto son frontend 3000, API 5164 y SQL 14330.

La API llama `TournamentDatabaseInitializer` por defecto. Aplica migraciones y, si no existen equipos, reutiliza el seed SQL versionado dentro de transacción. Migrar una base nueva ya carga el seed; repetir la inicialización no debe duplicarlo. Esta operación es bootstrap de Infrastructure, no un Command de negocio.

El seed tiene cuatro selecciones, veinte jugadores, seis partidos y tres resultados. No es un proveedor de datos deportivos actuales. Una base parcialmente poblada no recibe reparación completa automática. En múltiples réplicas conviene migrar desde un proceso coordinado; no se implementó coordinación distribuida del initializer.

SQL Developer, usuario sa, TrustServerCertificate y HTTP privado son configuración de demostración, no una recomendación de producción. Producción requeriría TLS válido, credenciales de mínimo privilegio, autenticación/autorización, secretos administrados y estrategia de backups.

## 15. Pruebas y CI

Una unitaria aísla comportamiento con dependencias controladas. Integración valida colaboración real, como SQL/triggers/transacciones. Contrato verifica interfaz HTTP/OpenAPI. Newman ejecuta la colección Postman contra servicios reales.

Hay tests de Domain, Application, Infrastructure y API. SQLite permite comprobar comportamiento transaccional aislado; no reproduce cada característica de SQL Server. El fixture SQL usa migraciones, EF/UnitOfWork y Dapper reales en una base propia GUID; nunca debe borrar la base de la aplicación.

`Test-Qa.ps1` valida cuatro reportes TRX con pruebas ejecutadas y ninguna omitida. Código de salida 0 del runner no basta: Windows llegó a bloquear una DLL y el runner informó cero tests sin fallar el proceso. Sin conexión QA_SQLSERVER_CONNECTION las pruebas SQL se omiten; el script la configura desde `.env` sin imprimir contraseña.

La pipeline `.github/workflows/qa.yml` corre en Ubuntu para pushes/PRs de main y Desarrollo: restore, formato, tests, lint/build, Compose, SQL real y Newman. Limpia el volumen de su ambiente efímero al terminar; no ejecuta esa limpieza contra el volumen local del usuario. No se desactiva CI por posponer QA local.

Evidencia histórica: 211 tests .NET, 27 requests/110 aserciones HTTP y builds aprobados. Repetición final: Application bloqueada por integridad de Windows; resto de proyectos aprobados. Cálculos SQL son tests de integración y no sustituyen nominalmente el mínimo unitario obligatorio del PDF. El cierre de QA se pospone, no se certifica verde.

## 16. Preguntas frecuentes de defensa

**¿Por qué EF y Dapper juntos?** EF gestiona cambios y relaciones; Dapper permite proyecciones SQL específicas. La prueba exige esta separación; aumenta claridad y también coste de mantener dos mecanismos.

**¿Por qué UnitOfWork si DbContext ya ofrece una unidad de trabajo?** El puerto oculta EF a Application y centraliza transacción, traducción de restricciones y despacho posterior. Evita commits parciales entre repositorios.

**¿Una consulta previa evita duplicados?** No bajo concurrencia. Se combina mensaje anticipado con índices/trigger y rollback.

**¿Por qué no borrar jugadores?** Para conservar goles históricos. Desactivación bloquea nuevas acciones pero no elimina resultados anteriores.

**¿Por qué no guardar posiciones en una tabla?** Se derivan de partidos finalizados; evita mantener un segundo estado sincronizado. Con mayor escala podría evaluarse una proyección materializada, midiendo su necesidad.

**¿Está garantizada entrega de eventos?** No; logging posterior al commit no es outbox. Se distingue consistencia de negocio de observación duradera.

**¿Toda repetición retorna lo mismo?** POST con misma clave/payload sí reproduce su respuesta exitosa persistida. Otros verbos garantizan el efecto según estado y pueden devolver otro código; el resultado finalizado responde conflicto.

**¿Por qué QA no está cerrado?** Existe evidencia previa satisfactoria, pero la repetición de Application está bloqueada por Windows. El responsable decidió posponerla y documentar la deuda; no se alteró seguridad para forzar ejecución.

**¿Qué mejoraría para producción?** Seguridad de acceso, versiones optimistas, outbox, métricas/collector, purga idempotente, pruebas UI/carga, ranking global independiente de filtros si se requiere, y despliegue/migraciones coordinados.

## 17. Guion de exposición sugerido

1. Problema y alcance: 1 minuto.
2. Diagrama de capas, dependencias y CQRS: 3 minutos.
3. Crear recurso y repetir POST: 2 minutos.
4. Goles, resultado y actualización automática de estadísticas: 3 minutos.
5. Filtros, paginación y traza del proxy: 2 minutos.
6. QA, decisiones y límites honestos: 2 minutos.

Use [guia-demostracion.md](guia-demostracion.md) para ejecutar los ejemplos. Estos tiempos son una propuesta de preparación, no una agenda exigida por el examinador.
