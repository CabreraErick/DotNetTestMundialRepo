// Responsabilidad del archivo: Expone por HTTP la tabla de posiciones y la clasificación de goleadores.
// Relación en el sistema: Convierte parámetros de Swagger en Queries y publica las páginas calculadas por Dapper.
using DotNetTestMundial.Application.Tournament.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DotNetTestMundial.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class TournamentController(
    GetStandingsQueryHandler standingsHandler,
    GetScorersQueryHandler scorersHandler) : ControllerBase
{
    /* 
    <summary>
        Returns team statistics calculated from completed matches.
        Regresa estadisticas de equipos calculado a partir de partidos jugados (completados)
    </summary>
    */
    [HttpGet("standings")]
    public async Task<IActionResult> GetStandings(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "points",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var result = await standingsHandler.HandleAsync(new(
            search, pageNumber, pageSize, sortBy, sortDirection), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /*
        <summary>
        Returns goal scorers from completed matches with optional team filtering.
        Retorna total de goles por partidos jugados con opcion de filtros por equipo
        </summary>
    */
    [HttpGet("scorers")]
    public async Task<IActionResult> GetScorers(
        [FromQuery] string? search,
        [FromQuery] Guid? teamId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "goals",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var result = await scorersHandler.HandleAsync(new(
            search, teamId, pageNumber, pageSize, sortBy, sortDirection), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
