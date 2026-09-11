# Arquitectura del sistema

## Dependencias entre capas

~~~mermaid
flowchart LR
    Client["Cliente web / Swagger"] --> Api["DotNetTestMundial.Api"]
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
~~~
