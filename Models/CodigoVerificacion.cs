namespace CUChatNet.Api.Models;

public class CodigoVerificacion
{
    public int CodigoVerificacionId { get; set; }
    public string Email { get; set; } = null!;
    public string Codigo { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public bool Usado { get; set; }
    public int Intentos { get; set; }
    public string Tipo { get; set; } = "VERIFY";
}