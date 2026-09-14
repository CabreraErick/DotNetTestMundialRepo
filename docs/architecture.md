# Arquitectura del sistema

## Dependencias entre capas

~~~mermaid
flowchart LR
    Browser["Navegador"] --> Next["Frontend Next.js"]
    Next --> Proxy["Route Handler / proxy"]
    Proxy --> Api["DotNetTestMundial.Api"]
    Swagger["Swagger"] --> Api
    Api --> Application["DotNetTestMundial.Application"]
    Api --> Infrastructure["DotNetTestMundial.Infrastructure"]
    Infrastructure --> Application
    Infrastructure --> Domain["DotNetTestMundial.Domain"]
    Application --> Domain

    Domain -. "sin dependencias de otras capas" .-> Rules["Entidades, Result y eventos"]
~~~

- **Domain** contiene entidades, invariantes, errores, Result y eventos. No conoce HTTP, EF Core, Dapper ni SQL Server.
- **Application** coordina casos de uso y define puertos de persistencia, lectura, idempotencia y eventos.
- **Infrastructure** implementa esos puertos con EF Core, Dapper, SQL Server y logging.
- **API** recibe HTTP, ejecuta handlers, convierte errores a códigos REST y configura las dependencias.
- **Frontend** presenta los flujos funcionales y reenvía HTTP mediante un proxy configurable; no contiene reglas de torneo ni accede a SQL Server.

## Flujos CQRS

~~~mermaid
flowchart TB
    Http["Solicitud HTTP"] --> Controller["Controller"]

    Controller --> Command["Command handler"]
    Command --> DomainRules["Entidades de Domain"]
    Command --> Ef["Repositorio EF Core"]
    Ef --> Uow["Unit of Work"]
    Uow --> SqlWrite[("SQL Server")]
    Uow --> Events["Eventos confirmados / logging"]

    Controller --> Query["Query handler"]
    Query --> Dapper["Repositorio Dapper"]
    Dapper --> SqlRead[("SQL Server")]
    Dapper --> Projection["DTO / página"]
~~~

Las escrituras se agrupan en una transacción y se confirman una sola vez. Los eventos se registran después del commit. Las consultas no materializan entidades mediante EF Core y realizan filtros, orden y paginación directamente en SQL.

## Estructura

~~~text
src/
├── DotNetTestMundial.Domain
├── DotNetTestMundial.Application
├── DotNetTestMundial.Infrastructure
└── DotNetTestMundial.Api

tests/
├── DotNetTestMundial.Domain.Tests
├── DotNetTestMundial.Application.Tests
├── DotNetTestMundial.Infrastructure.Tests
└── DotNetTestMundial.Api.Tests

frontend/
├── src/app
├── src/components
├── src/hooks
├── src/lib/api
└── src/types
~~~

## Flujo del frontend

~~~mermaid
sequenceDiagram
    participant Browser as Navegador
    participant Next as Next.js
    participant Api as API .NET
    participant App as Application
    participant Sql as SQL Server

    Browser->>Next: /api/backend/api/... + filtros/headers
    Next->>Api: API_BASE_URL + solicitud preservada
    Api->>App: Command o Query
    App->>Sql: EF Core write o Dapper read
    Sql-->>App: Resultado
    App-->>Api: Result / proyección
    Api-->>Next: HTTP + Correlation ID
    Next-->>Browser: respuesta sin exponer URL interna
~~~

## Despliegue con Docker Compose

~~~mermaid
flowchart LR
    HostBrowser["Navegador :3000"] --> Frontend["frontend:3000"]
    HostSwagger["Swagger :5164"] --> Api["api:8080"]
    Frontend -->|"API_BASE_URL=http://api:8080"| Api
    Api -->|"EF Core / Dapper"| Sql["sqlserver:1433"]
    Sql --> Volume[("sqlserver-data")]

    subgraph Network["tournament-network"]
        Frontend
        Api
        Sql
    end
~~~

Compose inicia la API cuando SQL Server acepta conexiones e inicia el frontend
cuando `/health` responde correctamente. La API aplica migraciones solo cuando
`Database__ApplyMigrations=true`; la ejecución local conserva el flujo manual. Las
imágenes finales ejecutan usuarios sin privilegios y la configuración privada se
mantiene en `.env.docker`, excluido de Git y de los contextos de compilación.
