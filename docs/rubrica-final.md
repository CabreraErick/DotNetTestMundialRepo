# Cierre de alcance y rúbrica

Fecha: 15 de septiembre de 2026. Rama de entrega: `Desarrollo`; integración propuesta: `main` mediante pull request.

Este documento contrasta la implementación con la prueba técnica entregada por el examinador. Los porcentajes son pesos de evaluación, no puntos obtenidos ni garantía de aprobación.

## Criterios ponderados

| Criterio | Peso | Implementación / evidencia | Estado de alcance |
|---|---:|---|---|
| Diseño y Clean Architecture | 15% | Domain sin otras capas; Application define puertos; Infrastructure los implementa; API compone y adapta HTTP. | Implementado |
| CQRS | 10% | Handlers de Commands y Queries separados; lectura de negocio con Dapper y escritura con EF Core. | Implementado |
| EF Core write | 10% | Repositorios, configuraciones, migraciones, índices y compatibilidad con triggers SQL Server. | Implementado |
| Dapper read | 10% | Consultas parametrizadas, DTOs, agregados SQL y páginas sin traer toda la colección. | Implementado |
| Unit of Work | 10% | Commit explícito, transacción entre repositorios, rollback y eventos después de confirmar. | Implementado |
| REST e idempotencia | 10% | Verbos, Result a estados HTTP, claves POST persistidas y respuesta reproducible. | Implementado; ver decisiones REST |
| Observabilidad | 10% | ILogger JSON, scopes, duración, CorrelationId, TraceId W3C y eventos trazables. | Implementado |
| Lógica del torneo | 10% | Calendario, resultados coherentes con goles, puntos, desempates y goleadores. | Implementado |
| Filtros y paginación | 10% | Colecciones paginadas en SQL; orden permitido y marcador independiente de la página. | Implementado; ver interpretación GET |
| Tests | 5% | Unitarias, integración SQLite/SQL Server, contratos HTTP y Newman. | Cierre incompleto, aceptado como pendiente |

No se declara 100% de cumplimiento verificado: el PDF califica las pruebas unitarias como obligatorias aunque les asigne 5%. Posponerlas no elimina ese requisito ni su posible efecto en la evaluación.

## Objetivos obligatorios adicionales

| Objetivo | Resultado |
|---|---|
| Result y Result<T> | Éxito/fallo con Error; Validation, NotFound y Conflict traducidos a 400, 404 y 409. |
| Equipos | CRUD, identidad única, filtros, orden y paginación. |
| Jugadores | Registro por equipo, dorsal reservado, filtros, paginación y baja lógica. |
| Partidos | Calendario por fecha/equipo/estado, programación, edición, cancelación y resultado. |
| Estadísticas | Posiciones y goleadores calculados automáticamente con Dapper. |
| Seed | Cuatro equipos, cinco jugadores por equipo, seis partidos y tres Played; inicialización en Infrastructure. |
| Next.js desacoplado | Equipos, jugadores, partidos, resultados, posiciones, goleadores y manejo de errores. |
| Docker | Dockerfiles API/frontend, Compose con SQL Server, red, volumen y healthchecks. |
| Diagrama | Capas, CQRS, EF, Dapper, UnitOfWork, almacenamiento idempotente, eventos, logs, traza, frontend y Docker. |
| Postman | Colección organizada, variables, idempotencia, páginas, filtros y conflictos. |

## Decisiones que deben explicarse en la defensa

- GET: la expresión del PDF «todos los GET» se interpreta para colecciones. Los detalles `/api/teams/{id}`, `/api/players/{id}` y `/api/matches/{id}` retornan un recurso, no una página. Health y Swagger tampoco son colecciones de negocio. Si el examinador exige una lectura literal diferente, habría que acordar el contrato.
- UnitOfWork: todas las escrituras de Commands que modifican estado pasan por él. Un replay idempotente o una baja ya realizada no necesita otro commit. Migraciones y seed son bootstrap de infraestructura, no Commands de negocio; usan las operaciones de migración y su transacción, sin guardar negocio directamente desde un handler.
- REST: PUT de equipos/jugadores reemplaza los campos editables; pertenencia del jugador es inmutable. DELETE de jugadores desactiva; DELETE de partidos cancela. Repetirlos conserva el efecto. Equipos usa eliminación física sujeta a relaciones; un DELETE posterior puede responder 404 sin violar la idempotencia del efecto.
- Resultado: PUT `/result` finaliza un partido una vez. Una repetición después de Played responde conflicto y no cambia estado; no implementa replay 200 ni edición del resultado finalizado. Esa semántica debe exponerse, no prometer respuesta idéntica para todos los PUT.
- Orden estadístico: la posición se calcula sobre la selección filtrada antes de paginar; el orden visual alternativo no renumera ese resultado. No se promete un ranking global independiente de filtros.
- Arranque: Compose lee `.env` tras ejecutar el bootstrap. Sin credenciales configuradas no puede arrancar; no se incluyen secretos en Git.

## QA aceptado como pendiente

Evidencia previa: ejecución completa inicial con 211 pruebas .NET aprobadas; frontend lint/build; imágenes Docker; 27 solicitudes y 110 aserciones de Newman. La repetición más reciente aprobó Domain 57, Infrastructure 37 (incluye 9 SQL reales) y API 10, pero Windows bloqueó la DLL de Application. Su TRX registra cero pruebas y el script rechaza ese resultado.

Pendientes dentro del apartado Tests:

1. Repetir las 107 pruebas Application en un entorno autorizado y completar una ejecución integral del estado final.
2. Revisar la correspondencia estricta del mínimo unitario del PDF: puntos, diferencia y desempates tienen comprobaciones de integración SQL reales; no se presentan como unitarias puras.

La decisión del responsable es cerrar el alcance de desarrollo con esta deuda explícita y posponer QA. No se modifica la política de Windows, no se eliminan tests y no se fuerza un verde en CI. Crear un PR no equivale a merge ni a entrega certificada por QA.

## Lecturas para la defensa

- [Manual teórico y aplicado](defensa-tecnica.md).
- [Demostración y casos de uso](guia-demostracion.md).
- [Arquitectura](architecture.md).
- [Evidencia histórica de QA](qa-final.md).
- [Procesos y comandos de QA](procesos-qa.md).
