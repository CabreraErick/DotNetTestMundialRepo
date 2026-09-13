// Responsabilidad del archivo: Define la forma uniforme de las colecciones paginadas.
// Relación en el sistema: Los repositorios Dapper proporcionan datos y totales; API serializa este contrato.
namespace DotNetTestMundial.Application.Common;

/// <summary>
/// Stable response contract shared by collection Queries and exposed by the API.
/// The repository calculates TotalRecords in the database; this type derives TotalPages.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Data,
    int PageNumber,
    int PageSize,
    long TotalRecords,
    int TotalPages)
{
    public static PagedResult<T> Create(
        IReadOnlyList<T> data, int pageNumber, int pageSize, long totalRecords) =>
        new(data, pageNumber, pageSize, totalRecords,
            totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize));
}
