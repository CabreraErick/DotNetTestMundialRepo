# QA final

Este bloque convierte la revisión final en comprobaciones reproducibles para backend,
frontend, contratos HTTP y contenedores.

## Resultado local del bloque ampliado (14 de septiembre de 2026)

- Ejecución completa inicial: 211 pruebas .NET aprobadas, ninguna omitida (Domain 57, Application 107, Infrastructure 37 incluyendo 9 SQL reales, API 10).
- Frontend: lint y build local/Docker aprobados; tres servicios saludables.
- Newman: 27 solicitudes y 110 aserciones aprobadas.
- Proxy Next.js: preservación de traceparent y CorrelationId comprobada; página 2 con un gol conserva marcador 2–1. PageSize 101 devuelve 400.
- Repetición final después de corregir avisos de nulabilidad en tests: Domain 57, Infrastructure 37 y API 10 aprobados; Windows bloquea Application.Tests.dll (CodeIntegrity eventos 3077/3033, error 0x800711C7). No se considera QA final completamente verde. El script detectó su TRX vacío y terminó con error. Requiere repetir Application en un entorno autorizado, sin desactivar políticas de seguridad desde el script.
- Bases exclusivas de SQL de los fixtures eliminadas; base y stack de aplicación conservados. Al registrar esta evidencia el bloque estaba preparado en Desarrollo sin commit/push; la publicación y el PR posteriores no constituyen evidencia de ejecución de CI.

## Decisión de cierre (15 de septiembre de 2026)

El responsable decidió posponer la repetición pendiente y preparar commit/PR sin certificar QA final aprobado. No se eliminan tests ni se relajan controles. La repetición autorizada volvió a aprobar Domain 57, Infrastructure 37 y API 10; Application produjo TRX vacío por CodeIntegrity 3077 y el script terminó con error. Además, la correspondencia estricta del mínimo unitario de cálculos queda pendiente: su evidencia actual es integración SQL. Consulte `rubrica-final.md` y el manual `defensa-tecnica.md`.

## Verificación automatizada local

Desde la raíz del repositorio:

Para incluir SQL Server real y arrancar servicios automáticamente, ejecute `scripts/Start-Qa.ps1` y después `scripts/Test-Qa.ps1 -SkipStart`. Consulte `procesos-qa.md` para la teoría, aislamiento y requisitos. Los comandos individuales siguientes, sin `QA_SQLSERVER_CONNECTION`, omiten las pruebas reales de SQL.

~~~powershell
dotnet restore DotNetTestMundial.sln
dotnet test DotNetTestMundial.sln --configuration Release --no-restore
pnpm --dir frontend install --frozen-lockfile
pnpm --dir frontend lint
pnpm --dir frontend build
docker compose --env-file .env.docker config --quiet
~~~

`ApiContractTests` inicia la aplicación en memoria, comprueba `/health`, preservación
de `X-Correlation-ID` y las 23 operaciones HTTP publicadas por Swagger. No abre una
conexión SQL: las reglas, handlers y persistencia se mantienen cubiertos por sus
proyectos de pruebas respectivos.

`.github/workflows/qa.yml` ejecuta los mismos controles en pushes y pull requests de
`main` y `Desarrollo`, y además construye las imágenes Docker.

## Escenario Postman

Importe, en este orden:

1. `postman/DotNetTestMundial.environment.json`.
2. `postman/DotNetTestMundial.postman_collection.json`.

Seleccione el ambiente local e inicie el stack con Docker Compose. Ejecute la
colección completa en orden mediante Collection Runner. El escenario crea dos
equipos y jugadores con nombres únicos, prueba repetición idempotente, programa y
edita un partido, registra un gol y resultado, consulta clasificaciones y confirma
los conflictos que protegen el historial.

También puede ejecutarse sin interfaz mediante Newman:

~~~powershell
npx --yes newman run postman/DotNetTestMundial.postman_collection.json `
  --environment postman/DotNetTestMundial.environment.json `
  --reporters cli
~~~

La ejecución completa contiene 27 solicitudes y verifica también TraceId y el sobre paginado de goles. Un solo fallo
produce un código de salida distinto de cero tanto localmente como en CI.

La primera solicitud genera un `runId`, abreviaturas únicas y una fecha treinta días
en el futuro. Por ello, cada ejecución completa usa nuevas claves de idempotencia,
nombres y datos de calendario.

## Validación runtime de Docker

Con Docker Desktop iniciado:

~~~powershell
Copy-Item .env.docker.example .env.docker
docker compose --env-file .env.docker up --detach --build
docker compose --env-file .env.docker ps
Invoke-WebRequest http://localhost:5164/health
Invoke-WebRequest http://localhost:3000
~~~

Los tres servicios deben quedar `healthy`. Después se ejecuta el escenario Postman y
se revisan los logs de correlación:

~~~powershell
docker compose --env-file .env.docker logs api
~~~

La detención normal conserva SQL Server en el volumen. No utilice `down --volumes`
salvo que se quiera descartar deliberadamente la base de QA.
