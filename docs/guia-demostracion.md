# Demostración y casos de uso

Los comandos de este documento son ejemplos para la defensa. No se ejecutaron como una nueva sesión QA durante el cierre documental. Los ejemplos de creación modifican la base local y conservan datos; use un ambiente de demostración y nombres/claves únicos.

## Preparación

PowerShell 7, Docker Desktop Linux y Git son necesarios para arrancar. Para QA completo también .NET SDK de global.json, Node y pnpm. Desde la raíz:

~~~powershell
.\scripts\Start-Qa.ps1
docker compose ps
~~~

Abra frontend `http://localhost:3000` y Swagger `http://localhost:5164/swagger`. Puertos pueden cambiar en `.env`. No comparta ese archivo. No utilice down --volumes para limpiar la demostración salvo intención explícita de borrar la base.

Compose moderno permite `docker compose up --detach --wait` después de preparar `.env`. El comando legacy docker-compose del PDF es equivalente en propósito; esta guía usa el plugin moderno.

## Caso 1: explorar el torneo precargado

~~~powershell
$api = 'http://localhost:5164'
Invoke-RestMethod "$api/api/teams?pageNumber=1&pageSize=10&sortBy=name&sortDirection=asc"
Invoke-RestMethod "$api/api/matches?status=Played&pageNumber=1&pageSize=10&sortBy=scheduledAt&sortDirection=asc"
Invoke-RestMethod "$api/api/standings?pageNumber=1&pageSize=10&sortBy=points&sortDirection=desc"
Invoke-RestMethod "$api/api/scorers?pageNumber=1&pageSize=10&sortBy=goals&sortDirection=desc"
~~~

Propósito: mostrar que seed y consultas permiten visualizar datos sin ingreso manual previo. Si la base contiene otros datos de ejecuciones anteriores, las listas no se limitan a cuatro equipos.

## Caso 2: crear dos equipos sin duplicar reintentos

~~~powershell
$run = [Guid]::NewGuid().ToString('N')
$shortHome = 'H' + $run.Substring(0,7)
$shortAway = 'A' + $run.Substring(0,7)
$teamBody = @{ name="Defensa Local $run"; shortName=$shortHome } | ConvertTo-Json
$teamHeaders = @{ 'Idempotency-Key'="defensa-team-home-$run"; 'X-Correlation-ID'="defensa-$run" }
$home = Invoke-RestMethod "$api/api/teams" -Method Post -ContentType 'application/json' -Headers $teamHeaders -Body $teamBody
$replay = Invoke-RestMethod "$api/api/teams" -Method Post -ContentType 'application/json' -Headers $teamHeaders -Body $teamBody
$home.id -eq $replay.id # True esperado
$awayBody = @{ name="Defensa Visitante $run"; shortName=$shortAway } | ConvertTo-Json
$away = Invoke-RestMethod "$api/api/teams" -Method Post -ContentType 'application/json' -Headers @{ 'Idempotency-Key'="defensa-team-away-$run" } -Body $awayBody
~~~

Resultado esperado: 201 en creaciones y replay; mismo ID del local. Para demostrar conflicto sin ocultar su código:

~~~powershell
$changedBody = @{ name="Otro Equipo $run"; shortName=$shortHome } | ConvertTo-Json
$conflict = Invoke-WebRequest "$api/api/teams" -Method Post -ContentType 'application/json' -Headers $teamHeaders -Body $changedBody -SkipHttpErrorCheck
$conflict.StatusCode # 409 esperado por clave con otro payload
$conflict.Content
~~~

Una clave nueva con identidad de equipo ya existente también puede producir 409, pero es un conflicto de unicidad, no el mismo mecanismo de idempotencia.

## Caso 3: registrar integrantes y baja lógica

~~~powershell
$playerBody = @{ teamId=$home.id; name="Goleador $run"; jerseyNumber=9 } | ConvertTo-Json
$player = Invoke-RestMethod "$api/api/players" -Method Post -ContentType 'application/json' -Headers @{ 'Idempotency-Key'="defensa-player-$run" } -Body $playerBody
Invoke-RestMethod "$api/api/players?teamId=$($home.id)&isActive=true&pageNumber=1&pageSize=10&sortBy=name&sortDirection=asc"
~~~

Registrar otro jugador del mismo equipo con dorsal 9 debe dar 409. No desactive todavía este jugador si continuará al caso de gol: solo jugadores activos pueden marcar.

Después del caso de resultado puede demostrar baja y reactivación:

~~~powershell
Invoke-WebRequest "$api/api/players/$($player.id)" -Method Delete
Invoke-RestMethod "$api/api/players/$($player.id)" # isActive false; fila conservada
Invoke-RestMethod "$api/api/players/$($player.id)" -Method Patch -ContentType 'application/json' -Body '{"isActive":true}'
~~~

El dorsal continúa reservado tras desactivar. Goles históricos permanecen en goleadores.

## Caso 4: calendario, gol y resultado

~~~powershell
$scheduled = (Get-Date).Date.AddDays(30).AddHours(10).ToString('yyyy-MM-ddTHH:mm:ss')
$matchBody = @{ homeTeamId=$home.id; awayTeamId=$away.id; scheduledAt=$scheduled } | ConvertTo-Json
$match = Invoke-RestMethod "$api/api/matches" -Method Post -ContentType 'application/json' -Headers @{ 'Idempotency-Key'="defensa-match-$run" } -Body $matchBody
$goalBody = @{ playerId=$player.id; minute=35 } | ConvertTo-Json
$goalHeaders = @{ 'Idempotency-Key'="defensa-goal-$run"; 'X-Correlation-ID'="defensa-result-$run" }
Invoke-RestMethod "$api/api/matches/$($match.id)/goals" -Method Post -ContentType 'application/json' -Headers $goalHeaders -Body $goalBody
Invoke-RestMethod "$api/api/matches/$($match.id)/goals" -Method Post -ContentType 'application/json' -Headers $goalHeaders -Body $goalBody # mismo ID; no segundo gol
~~~

Antes de terminar, un resultado 0-0 con ese gol debe devolver conflicto de marcador:

~~~powershell
$mismatch = Invoke-WebRequest "$api/api/matches/$($match.id)/result" -Method Put -ContentType 'application/json' -Body '{"homeScore":0,"awayScore":0}' -SkipHttpErrorCheck
$mismatch.StatusCode # 409 esperado
Invoke-RestMethod "$api/api/matches/$($match.id)/result" -Method Put -ContentType 'application/json' -Headers @{ 'X-Correlation-ID'="defensa-result-$run" } -Body '{"homeScore":1,"awayScore":0}'
Invoke-RestMethod "$api/api/standings?search=$run&pageNumber=1&pageSize=10&sortBy=points&sortDirection=desc"
Invoke-RestMethod "$api/api/scorers?teamId=$($home.id)&pageNumber=1&pageSize=10&sortBy=goals&sortDirection=desc"
~~~

Esperado: Played con marcador 1-0; local 3 puntos, diferencia +1; visitante 0 puntos, diferencia -1; jugador con 1 gol. Repetir PUT resultado tras finalizar devuelve 409, no replay 200. Registrar otro gol o cancelar Played también debe dar 409.

Para demostrar conflicto de calendario intente otro partido con uno de estos equipos durante el mismo día: la regla no distingue local/visitante. Cancele únicamente un partido Scheduled; cancelar libera el día pero conserva la fila.

## Caso 5: paginación sin alterar el marcador

El seed usa este partido Argentina-Brasil con tres goles:

~~~powershell
$seedMatch = '26500000-0000-0000-0000-000000000001'
$page = Invoke-RestMethod "$api/api/matches/$seedMatch/goals?pageNumber=2&pageSize=1&sortBy=minute&sortDirection=asc"
$page | ConvertTo-Json -Depth 5
~~~

Esperado en seed intacto: minuto 44 en la página, totalRecords 3, totalPages 3 y marcador homeGoals 2 / awayGoals 1. Filtrar un equipo o consultar página 99 no reduce el marcador completo.

~~~powershell
$invalid = Invoke-WebRequest "$api/api/matches/$seedMatch/goals?pageSize=101" -SkipHttpErrorCheck
$invalid.StatusCode # 400 esperado
~~~

El frontend `/partidos/<id>` ofrece búsqueda, filtro por equipo, orden de minuto y navegación paginada.

## Caso 6: seguir una traza por frontend y API

~~~powershell
$traceHeaders = @{
  'X-Correlation-ID'='defensa-trace'
  'traceparent'='00-1234567890abcdef1234567890abcdef-1234567890abcdef-01'
}
$response = Invoke-WebRequest "http://localhost:3000/api/backend/api/matches/$seedMatch/goals?pageNumber=1&pageSize=1" -Headers $traceHeaders
$response.Headers['X-Correlation-ID'] # defensa-trace
$response.Headers['X-Trace-ID'] # 1234567890abcdef1234567890abcdef
docker compose logs --tail 100 api
~~~

Busque el scope con ambos identificadores, ruta, duración y estado. Tras crear equipo/finalizar resultado aparecen TeamCreatedEvent y MatchResultRegisteredEvent. Debug no aparecerá si el nivel configurado lo filtra. No copie credenciales al compartir evidencia.

## Caso 7: colección Postman y QA pospuesto

Importe `postman/DotNetTestMundial.environment.json` y `postman/DotNetTestMundial.postman_collection.json`; seleccione el ambiente y ejecute en orden. La colección genera su propia identidad de ejecución y crea datos persistentes.

~~~powershell
npx --yes newman run postman/DotNetTestMundial.postman_collection.json --environment postman/DotNetTestMundial.environment.json --reporters cli
# Solo cuando se retome el cierre pendiente:
.\scripts\Test-Qa.ps1 -SkipStart
~~~

Hay evidencia histórica de 27 solicitudes/110 aserciones. No se garantiza ese resultado para una nueva ejecución sin verificarla. Windows bloqueó la DLL de Application; un TRX vacío no es aprobación. Las pruebas de puntos/diferencia SQL son de integración. Exponga estas reservas al examinador.

## Ficha de casos de uso

| Actor | Intención | Condición principal | Resultado |
|---|---|---|---|
| Organizador | Inscribir equipo | Identidad única y clave POST | Equipo creado sin duplicar reintentos |
| Organizador | Registrar jugador | Equipo existente y dorsal disponible | Integrante activo del equipo |
| Organizador | Programar partido | Equipos distintos, día disponible | Scheduled |
| Responsable del encuentro | Registrar gol | Partido Scheduled, jugador activo participante | Gol persistido y respuesta repetible |
| Responsable del encuentro | Cerrar resultado | Marcador igual a goles | Played; estadísticas derivadas |
| Organizador | Cancelar encuentro | Partido Scheduled | Cancelled; día liberado |
| Consulta | Revisar posiciones/goleadores | Partidos Played disponibles | Página SQL con estadísticas |
| Soporte | Diagnosticar solicitud | CorrelationId/TraceId identificables | Logs enlazados a la ejecución |

Estos actores describen responsabilidades funcionales; no hay roles de seguridad ni autenticación implementados.
