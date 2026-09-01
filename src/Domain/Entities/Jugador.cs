public class Jugador
{
    public Guid Id { get; set; }
    public string Nombre { get; set; }
    public int NumeroCamiseta { get; set; }
    public Guid EquipoId { get; set; }
    public Equipo Equipo { get; set; }
}
