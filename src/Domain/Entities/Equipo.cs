public class Equipo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; }
    public string Representante { get; set; }
    public int Puntos { get; set; }
    public int GolesFavor { get; set; }
    public int GolesContra { get; set; }
    public int DiferenciaGoles => GolesFavor - GolesContra;
    public ICollection<Jugador> Jugadores { get; set; }
}
