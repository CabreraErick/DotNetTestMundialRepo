// Responsabilidad del archivo: Documenta el encabezado opcional de correlación en cada operación Swagger.
// Relación en el sistema: Permite probar desde el navegador el valor que procesa RequestObservabilityMiddleware.
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DotNetTestMundial.Api.Observability;

public sealed class CorrelationIdHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = RequestObservabilityMiddleware.CorrelationHeaderName,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Optional correlation identifier returned in the response and included in structured logs.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                MaxLength = 128
            }
        });
    }
}
