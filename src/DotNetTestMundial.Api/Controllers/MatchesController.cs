// Responsabilidad del archivo: Adapta partidos, goles y resultados HTTP a Commands y Queries.
// Relación en el sistema: Traduce Result a códigos REST y delega reglas, CQRS y persistencia a las capas internas.
using DotNetTestMundial.Application.Matches.CreateMatch;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Matches.Results;
using DotNetTestMundial.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace DotNetTestMundial.Api.Controllers;

[ApiController]
[Route("api/matches")]
public sealed class MatchesController(
    CreateMatchCommandHandler createHandler,
    GetMatchesQueryHandler getMatchesHandler,
    GetMatchByIdQueryHandler getByIdHandler,
    UpdateMatchCommandHandler updateHandler,
    PatchMatchCommandHandler patchHandler,
    DeleteMatchCommandHandler deleteHandler,
    CreateGoalCommandHandler createGoalHandler,
    GetMatchGoalsQueryHandler getGoalsHandler,
    RegisterMatchResultCommandHandler resultHandler) : ControllerBase
{
    public sealed record CreateMatchRequest(
        Guid HomeTeamId, Guid AwayTeamId, DateTime ScheduledAt);
    public sealed record UpdateMatchRequest(
        Guid HomeTeamId, Guid AwayTeamId, DateTime ScheduledAt);
    public sealed record PatchMatchRequest(
        Guid? HomeTeamId, Guid? AwayTeamId, DateTime? ScheduledAt);
    public sealed record CreateGoalRequest(Guid PlayerId, int Minute);
    public sealed record RegisterResultRequest(int HomeScore, int AwayScore);

    /// <summary>Schedules a match and stores a repeatable HTTP 201 response.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateMatchRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(new(
            request.HomeTeamId,
            request.AwayTeamId,
            request.ScheduledAt,
            idempotencyKey), cancellationToken);
        if (result.IsFailure)
            return MapError(result.Error!);

        var response = result.Value;
        Response.Headers.Location = $"/api/matches/{response.MatchId}";
        return new ContentResult
        {
            StatusCode = response.StatusCode,
            ContentType = "application/json; charset=utf-8",
            Content = response.ResponseBody
        };
    }

    /// <summary>Returns the filtered tournament calendar through Dapper.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] Guid? teamId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "scheduledAt",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var result = await getMatchesHandler.HandleAsync(new(
            teamId, status, from, to, pageNumber, pageSize, sortBy, sortDirection),
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Returns one match with team names, score and goal count.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getByIdHandler.HandleAsync(new(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Replaces participants and date while the match remains scheduled.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, UpdateMatchRequest request, CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(new(
            id, request.HomeTeamId, request.AwayTeamId, request.ScheduledAt), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Changes only supplied scheduling fields.</summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(
        Guid id, PatchMatchRequest request, CancellationToken cancellationToken)
    {
        var result = await patchHandler.HandleAsync(new(
            id, request.HomeTeamId, request.AwayTeamId, request.ScheduledAt), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Cancels a scheduled match without removing its historical row.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(new(id), cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error!);
    }

    /// <summary>Registers one scorer and stores a repeatable HTTP 201 response.</summary>
    [HttpPost("{id:guid}/goals")]
    public async Task<IActionResult> CreateGoal(
        Guid id,
        CreateGoalRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await createGoalHandler.HandleAsync(new(
            id, request.PlayerId, request.Minute, idempotencyKey), cancellationToken);
        if (result.IsFailure)
            return MapError(result.Error!);

        var response = result.Value;
        Response.Headers.Location = $"/api/matches/{id}/goals/{response.GoalId}";
        return new ContentResult
        {
            StatusCode = response.StatusCode,
            ContentType = "application/json; charset=utf-8",
            Content = response.ResponseBody
        };
    }

    /// <summary>Returns the chronological goal list with scorer and team names.</summary>
    [HttpGet("{id:guid}/goals")]
    public async Task<IActionResult> GetGoals(Guid id, [FromQuery] Guid? teamId,
        [FromQuery] string? search, [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10, [FromQuery] string sortBy = "minute",
        [FromQuery] string sortDirection = "asc", CancellationToken cancellationToken = default)
    {
        var result = await getGoalsHandler.HandleAsync(new(id, teamId, search,
            pageNumber, pageSize, sortBy, sortDirection), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    /// <summary>Closes a scheduled match when its score equals its registered goals.</summary>
    [HttpPut("{id:guid}/result")]
    public async Task<IActionResult> RegisterResult(
        Guid id, RegisterResultRequest request, CancellationToken cancellationToken)
    {
        var result = await resultHandler.HandleAsync(new(
            id, request.HomeScore, request.AwayScore), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    private IActionResult MapError(Error error) => error.Type switch
    {
        ErrorType.Validation => BadRequest(error),
        ErrorType.NotFound => NotFound(error),
        ErrorType.Conflict => Conflict(error),
        _ => throw new InvalidOperationException("Unsupported error type.")
    };
}
