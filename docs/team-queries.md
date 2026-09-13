# Consulta paginada de equipos

`GET /api/teams` implementa el primer flujo Query de CQRS. `TeamsController` transforma los parámetros HTTP en `GetTeamsQuery`; `GetTeamsQueryHandler` valida esos valores y crea `TeamPageSpecification`; `ITeamReadRepository` mantiene Application independiente de la tecnología; `SqlTeamReadRepository` completa el flujo mediante Dapper y SQL Server.

La Query no utiliza EF Core, no materializa entidades del dominio, no modifica estado y no llama Unit of Work. Devuelve `TeamListItem`, una proyección con las tres columnas necesarias. EF Core permanece reservado para los Commands de escritura.

## Parámetros

- `search`: fragmento opcional que se busca en `Name` o `ShortName`. Los caracteres especiales de `LIKE` se tratan literalmente.
- `pageNumber`: comienza en 1.
- `pageSize`: admite de 1 a 100 registros.
- `sortBy`: admite `id`, `name` y `shortName` sin distinguir mayúsculas.
- `sortDirection`: admite `asc` y `desc`.

Application convierte orden y dirección a enums. Infrastructure genera `ORDER BY` únicamente a partir de esos enums; ningún texto arbitrario del cliente se concatena al SQL. Filtro, desplazamiento y tamaño se envían como parámetros Dapper.

El repositorio ejecuta un lote con `COUNT_BIG` y la página mediante `OFFSET/FETCH`. Así `totalRecords` corresponde al filtro completo, mientras `data` contiene sólo la página pedida. Para nombres o abreviaturas repetidos se agrega `Id ASC` como desempate y se conserva una paginación estable.

## Respuesta

```json
{
  "data": [
    { "id": "guid", "name": "Argentina", "shortName": "ARG" }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 1,
  "totalPages": 1
}
```

Los parámetros inválidos producen HTTP 400 antes de abrir una conexión. Una página posterior al final produce HTTP 200 con `data` vacío y conserva los totales del filtro.
