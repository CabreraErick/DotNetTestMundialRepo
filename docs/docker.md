<!-- Responsabilidad: documenta la construcción, operación y diagnóstico de los contenedores. -->
<!-- Relación: coordina SQL Server, la API .NET y el frontend Next.js sin exponer secretos. -->
# Docker y Docker Compose

## Servicios

| Servicio | Dirección interna | Puerto local | Responsabilidad |
|---|---|---:|---|
| `sqlserver` | `sqlserver:1433` | `14330` | Persistencia SQL Server 2022 |
| `api` | `api:8080` | `5164` | REST, migraciones, EF Core y Dapper |
| `frontend` | `frontend:3000` | `3000` | Next.js standalone y proxy HTTP |

Los servicios comparten `tournament-network`. SQL Server conserva sus archivos en
`sistema-gestor-futbol-sqlserver-data`.

## Requisitos

- Docker Desktop con contenedores Linux y motor WSL 2.
- Docker Compose.
- Puertos locales `3000`, `5164` y `14330` disponibles.

Compruebe el entorno:

```powershell
docker --version
docker compose version
docker info
```

## Configuración privada

Opción automatizada: ejecute `.\scripts\Start-Qa.ps1` desde la raíz. Prepara `.env` sin publicar credenciales, reutiliza `.env.docker` si existe y espera a que los tres servicios estén saludables. Después puede usar `docker compose up --detach --wait` sin `--env-file`. La opción manual siguiente sigue disponible.

Genere el archivo local desde la plantilla:

```powershell
Copy-Item .env.docker.example .env.docker
```

Reemplace `REPLACE_WITH_A_STRONG_PASSWORD` en `.env.docker`. Este archivo está
excluido de Git y de las imágenes. No publique su contenido ni ejecute
`docker compose config` sin `--quiet`, porque la salida resuelve variables.

Valide sin imprimir secretos:

```powershell
docker compose --env-file .env.docker config --quiet
```

## Construcción e inicio

```powershell
docker compose --env-file .env.docker up --detach --build
docker compose --env-file .env.docker ps
```

El orden de disponibilidad es SQL Server, API y frontend. Los health checks
impiden que un servicio dependiente inicie antes de que el anterior responda.

La primera ejecución crea `DotNetTestMundial_Docker`, aplica las migraciones de EF
Core y carga los datos FIFA 2026. Los siguientes inicios consultan el historial y
no repiten migraciones aplicadas.

## Direcciones

- Frontend: `http://localhost:3000`
- API health: `http://localhost:5164/health`
- Swagger: `http://localhost:5164/swagger`
- SQL Server desde Windows: `localhost,14330`

El navegador nunca utiliza la dirección interna de la API. Next.js recibe
`API_BASE_URL=http://api:8080` en tiempo de ejecución y reenvía `/api/backend/*`.

## Logs y diagnóstico

```powershell
docker compose --env-file .env.docker ps
docker compose --env-file .env.docker logs --tail 100 sqlserver
docker compose --env-file .env.docker logs --tail 100 api
docker compose --env-file .env.docker logs --tail 100 frontend
```

Pruebas básicas:

```powershell
Invoke-WebRequest http://localhost:5164/health -UseBasicParsing
Invoke-WebRequest http://localhost:5164/swagger/index.html -UseBasicParsing
Invoke-WebRequest http://localhost:3000 -UseBasicParsing
```

Si un servicio no está saludable, revise primero sus logs. No elimine el volumen
para resolver errores de configuración o credenciales.

## Detención y persistencia

Detenga y retire contenedores conservando la base:

```powershell
docker compose --env-file .env.docker down
```

Inicie nuevamente:

```powershell
docker compose --env-file .env.docker up --detach
```

Los datos permanecen en el volumen nombrado. El siguiente comando también elimina
la base y es destructivo; utilícelo solo cuando se requiera reiniciar todos los
datos de prueba:

```powershell
docker compose --env-file .env.docker down --volumes
```

## Reconstrucción y verificación

Después de cambios en .NET o Next.js:

```powershell
docker compose --env-file .env.docker up --detach --build
dotnet test DotNetTestMundial.sln --configuration Release
pnpm --dir frontend lint
pnpm --dir frontend build
```

Los `.dockerignore` excluyen dependencias, resultados de compilación, pruebas,
configuración local y secretos. Las imágenes finales ejecutan los usuarios `app`
y `nextjs`, no `root`.
