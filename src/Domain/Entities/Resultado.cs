public class Resultado
{
    public Guid Id { get; set; }
    public Guid PartidoId { get; set; }
    public Partido Partido{get;set;}
    public int GolesLocal { get; set; }
    public int GolesVisitante { get; set; }
}
