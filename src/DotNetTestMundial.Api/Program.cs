// Responsabilidad del archivo: Compone el host HTTP, la configuración y el contenedor de dependencias.
// Relación en el sistema: Conecta controladores de API con handlers de Application e implementaciones de Infrastructure.
using DotNetTestMundial.Infrastructure;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Application.Teams.Mutations;
using DotNetTestMundial.Application.Players.CreatePlayer;
using DotNetTestMundial.Application.Players.GetPlayers;
using DotNetTestMundial.Application.Players.Mutations;
using DotNetTestMundial.Application.Matches.CreateMatch;
using DotNetTestMundial.Application.Matches.GetMatches;
using DotNetTestMundial.Application.Matches.Mutations;
using DotNetTestMundial.Application.Matches.Results;
using DotNetTestMundial.Application.Tournament.Queries;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("Tournament");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings: Tournament (environment variable ConnectionStrings__Tournament) for this instance.");
builder.Services.AddInfrastructure(connectionString);
// API resolves Application orchestrators; each handler depends on ports rather than on
// controllers or SQL implementations, preserving the direction of Clean Architecture.
builder.Services.AddScoped<CreateTeamCommandHandler>();
builder.Services.AddScoped<GetTeamsQueryHandler>();
builder.Services.AddScoped<GetTeamByIdQueryHandler>();
builder.Services.AddScoped<UpdateTeamCommandHandler>();
builder.Services.AddScoped<PatchTeamCommandHandler>();
builder.Services.AddScoped<DeleteTeamCommandHandler>();
builder.Services.AddScoped<CreatePlayerCommandHandler>();
builder.Services.AddScoped<GetPlayersQueryHandler>();
builder.Services.AddScoped<GetPlayerByIdQueryHandler>();
builder.Services.AddScoped<UpdatePlayerCommandHandler>();
builder.Services.AddScoped<PatchPlayerCommandHandler>();
builder.Services.AddScoped<DeletePlayerCommandHandler>();
builder.Services.AddScoped<MatchTeamValidator>();
builder.Services.AddScoped<CreateMatchCommandHandler>();
builder.Services.AddScoped<GetMatchesQueryHandler>();
builder.Services.AddScoped<GetMatchByIdQueryHandler>();
builder.Services.AddScoped<UpdateMatchCommandHandler>();
builder.Services.AddScoped<PatchMatchCommandHandler>();
builder.Services.AddScoped<DeleteMatchCommandHandler>();
builder.Services.AddScoped<CreateGoalCommandHandler>();
builder.Services.AddScoped<GetMatchGoalsQueryHandler>();
builder.Services.AddScoped<RegisterMatchResultCommandHandler>();
builder.Services.AddScoped<GetStandingsQueryHandler>();
builder.Services.AddScoped<GetScorersQueryHandler>();

// Domain enums are exposed by their stable names so Swagger and clients can read
// Scheduled, Played and Cancelled instead of depending on database integer values.
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
