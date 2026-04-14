namespace CUChatNet.Api.Models;

public class CodigoRecuperacionAdmin
{
    public long CodigoRecuperacionAdminId { get; set; }
    public long UsuarioId { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? TokenRecuperacion { get; set; }
    public bool Verificado { get; set; }
    public bool Usado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaVerificacion { get; set; }
    public DateTime? FechaUso { get; set; }

    public Usuario? Usuario { get; set; }
}