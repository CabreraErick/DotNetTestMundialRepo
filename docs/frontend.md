<!-- Responsabilidad: registra arquitectura, alcance y flujo operativo del frontend 7B. -->
<!-- Relación: conecta las pantallas Next.js con los contratos REST y reglas de torneo existentes. -->
# Frontend Next.js - Bloque 7B

## Alcance

El frontend implementa visualización y registro de equipos e integrantes, calendario, goles, resultados, posiciones, goleadores, filtros, paginación y manejo de errores.

Se utiliza Next.js, React y TypeScript sin un framework visual adicional. HTML semántico y CSS adaptable mantienen la interfaz sencilla porque la rúbrica exige funcionalidad y desacoplamiento, no sofisticación gráfica.

## Separación de responsabilidades

~~~text
app/                  Rutas y composición de páginas
components/           Interacción y presentación por módulo
hooks/                Carga reutilizable de páginas remotas
lib/api/              Cliente HTTP y traducción uniforme de errores
types/                Contratos JSON equivalentes a Application
app/api/backend/      Proxy servidor-a-servidor hacia la API .NET
~~~

Los componentes no calculan puntos, posiciones ni goleadores. Esas reglas permanecen en Domain y en las consultas Dapper. El frontend sólo captura intención, envía contratos HTTP y presenta las proyecciones resultantes.

## Flujo HTTP

1. El navegador solicita `/api/backend/api/...` al mismo host de Next.js.
2. El Route Handler lee `API_BASE_URL`, disponible únicamente en el servidor.
3. El proxy conserva query string, cuerpo y headers relevantes.
4. La API procesa Commands con EF Core/Unit of Work o Queries con Dapper.
5. El proxy devuelve código, JSON, ubicación y correlación al navegador.

Cada POST genera una clave de idempotencia por intención y la conserva mientras la misma operación se reintente. Los errores `400`, `404`, `409`, fallos de conexión y respuestas inesperadas se convierten en mensajes visibles.

## Páginas

| Ruta | Recurso API | Función |
|---|---|---|
| `/equipos` | `/api/teams` | Registro, búsqueda, orden y paginación |
| `/jugadores` | `/api/players` | Registro, edición, activación/desactivación, búsqueda y filtros por equipo/estado |
| `/partidos` | `/api/matches` | Programación, cancelación y filtros por equipo/estado/fecha |
| `/partidos/{id}` | `/api/matches/{id}` | Goles y cierre del resultado |
| `/posiciones` | `/api/standings` | Clasificación calculada por el backend |
| `/goleadores` | `/api/scorers` | Clasificación y filtro por equipo |

## Configuración

`frontend/.env.example` define `API_BASE_URL=http://localhost:5164`. En Docker podrá cambiarse por el nombre interno del servicio sin recompilar una URL pública en el navegador.

La cancelación sólo aparece para partidos `Scheduled`, pide confirmación, bloquea acciones mientras espera el DELETE y actualiza el calendario al recibir 204. Los conflictos diarios y demás errores de negocio se muestran mediante el cliente HTTP común.
