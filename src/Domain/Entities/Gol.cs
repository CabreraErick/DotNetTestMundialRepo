namespace Domain.Entities;
public class Gol
{
    public Guid Id { get; set; }
    public Guid PartidoId { get; set; }
    public Partido Partido { get; set; }
    public Guid JugadorId { get; set; }
    public Jugador Jugador { get; set; }    
    public int Minuto { get; set; }
}
