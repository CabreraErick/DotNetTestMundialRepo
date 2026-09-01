public class Partido
{
    public Guid Id { get; set; }
    public Guid EquipoLocalId { get; set; }
    public Guid EquipoVisitanteId { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } // Programado, Finalizado
    public Resultado Resultado { get; set; }
}
