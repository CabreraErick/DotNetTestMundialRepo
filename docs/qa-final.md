# QA final

Este bloque convierte la revisión final en comprobaciones reproducibles para backend,
frontend, contratos HTTP y contenedores.

## Verificación automatizada local

Desde la raíz del repositorio:

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

La ejecución completa contiene 27 solicitudes y 82 aserciones. Un solo fallo
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
