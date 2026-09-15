// Responsabilidad: valida el host HTTP, observabilidad y superficie OpenAPI sin depender de SQL Server.
// Relación: protege el contrato público que consumen Postman y el frontend Next.js.
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DotNetTestMundial.Api.Tests.Contracts;

public sealed class ApiContractTests : IClassFixture<ApiContractFactory>
{
    private readonly HttpClient _client;

    public ApiContractTests(ApiContractFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOkAndPreservesCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", "qa-contract-001");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("qa-contract-001", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenApi_PublishesTheExpectedOperationsAndCorrelationHeader()
    {
        using var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        var expected = new (string Path, string Method)[]
        {
            ("/api/teams", "get"), ("/api/teams", "post"),
            ("/api/teams/{id}", "get"), ("/api/teams/{id}", "put"),
            ("/api/teams/{id}", "patch"), ("/api/teams/{id}", "delete"),
            ("/api/players", "get"), ("/api/players", "post"),
            ("/api/players/{id}", "get"), ("/api/players/{id}", "put"),
            ("/api/players/{id}", "patch"), ("/api/players/{id}", "delete"),
            ("/api/matches", "get"), ("/api/matches", "post"),
            ("/api/matches/{id}", "get"), ("/api/matches/{id}", "put"),
            ("/api/matches/{id}", "patch"), ("/api/matches/{id}", "delete"),
            ("/api/matches/{id}/goals", "get"), ("/api/matches/{id}/goals", "post"),
            ("/api/matches/{id}/result", "put"),
            ("/api/standings", "get"), ("/api/scorers", "get")
        };

        foreach (var (path, method) in expected)
        {
            Assert.True(paths.TryGetProperty(path, out var pathItem), $"Missing OpenAPI path {path}");
            Assert.True(pathItem.TryGetProperty(method, out var operation), $"Missing {method.ToUpperInvariant()} {path}");
            Assert.Contains(operation.GetProperty("parameters").EnumerateArray(), parameter =>
                parameter.GetProperty("name").GetString() == "X-Correlation-ID" &&
                parameter.GetProperty("in").GetString() == "header");
        }

        var operationCount = paths.EnumerateObject()
            .Sum(path => path.Value.EnumerateObject().Count(operation =>
                operation.Name is "get" or "post" or "put" or "patch" or "delete"));
        Assert.Equal(expected.Length, operationCount);
    }
}

public sealed class ApiContractFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(
            "ConnectionStrings:Tournament",
            "Server=localhost;Database=ContractTests;User Id=sa;Password=NotUsed1!;TrustServerCertificate=True");
        builder.UseSetting("Database:ApplyMigrations", "false");
        builder.UseSetting("Http:UseHttpsRedirection", "false");
    }
}
