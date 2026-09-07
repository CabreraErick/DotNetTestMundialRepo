using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace DotNetTestMundial.Api.Controllers;

[ApiController]
[Route("api/teams")]
public sealed class TeamsController(CreateTeamCommandHandler handler) : ControllerBase
{
    public sealed record CreateTeamRequest(string? Name, string? ShortName);

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTeamRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateTeamCommand(request.Name, request.ShortName, idempotencyKey), cancellationToken);

        if (result.IsFailure)
            return result.Error!.Type switch
            {
                ErrorType.Validation => BadRequest(result.Error),
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => throw new InvalidOperationException("Unsupported error type.")
            };

        var response = result.Value;
        Response.Headers.Location = $"/api/teams/{response.TeamId}";
        return new ContentResult
        {
            StatusCode = response.StatusCode,
            ContentType = "application/json; charset=utf-8",
            Content = response.ResponseBody
        };
    }
}
