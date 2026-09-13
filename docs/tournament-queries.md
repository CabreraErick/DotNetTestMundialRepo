# Posiciones y goleadores

Las consultas estadísticas completan el lado de lectura del torneo. `GET /api/standings` y `GET /api/scorers` utilizan exclusivamente Dapper; no resuelven `TournamentDbContext`, no materializan entidades del dominio y no realizan escrituras.

## Tabla de posiciones

La tabla incluye todos los equipos, incluso aquellos que todavía no disputaron partidos. Sólo considera partidos con estado `Played` y calcula en SQL Server:

- partidos jugados, ganados, empatados y perdidos;
- goles a favor y en contra;
- diferencia de goles;
- puntos: tres por victoria, uno por empate y cero por derrota.

La posición oficial se calcula por puntos descendentes, diferencia descendente, goles a favor descendentes y nombre ascendente. `sortBy` admite `points`, `goalDifference`, `goalsFor`, `played`, `won` y `teamName` para presentar la página sin cambiar esa posición. También admite búsqueda por nombre o abreviatura, paginación de 1 a 100 elementos y dirección `asc` o `desc`.

## Clasificación de goleadores

La clasificación agrupa goles por jugador y equipo. Sólo contabiliza goles pertenecientes a partidos `Played`, de modo que un partido todavía programado no altera estadísticas oficiales. Conserva los goles históricos de jugadores desactivados.

La posición se calcula siempre por cantidad de goles descendente y nombre de jugador ascendente. `sortBy` admite `goals`, `playerName` y `teamName` para presentar la página sin renumerar ese rango. La consulta permite buscar por nombre de jugador o equipo, filtrar por `teamId` y paginar de 1 a 100 elementos.

Ambas consultas calculan `position`, `totalRecords` y la página solicitada dentro de SQL Server. Los handlers convierten los textos de ordenamiento a enums antes de llamar al repositorio, evitando insertar valores del cliente en `ORDER BY`.

Este bloque utiliza las tablas existentes y no requiere una migración.
