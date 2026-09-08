// Responsabilidad del archivo: Modelo de respuesta perteneciente al endpoint de plantilla.
// Relación en el sistema: Sólo es consumido por WeatherForecastController y no forma parte del dominio del torneo.
namespace DotNetTestMundial.Api;

public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}
