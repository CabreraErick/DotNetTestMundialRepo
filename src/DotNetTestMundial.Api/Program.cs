// Responsabilidad del archivo: Compone el host HTTP, la configuración y el contenedor de dependencias.
// Relación en el sistema: Conecta controladores de API con handlers de Application e implementaciones de Infrastructure.
using DotNetTestMundial.Infrastructure;
using DotNetTestMundial.Application.Teams.CreateTeam;
using DotNetTestMundial.Application.Teams.GetTeams;
using DotNetTestMundial.Application.Teams.Mutations;

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

builder.Services.AddControllers();
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
