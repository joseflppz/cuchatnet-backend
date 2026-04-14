using System.ComponentModel.DataAnnotations;

namespace CUChatNet.Api.Models;

public class BitacoraEvento
{
    [Key] // 🔥 ESTO TAMBIÉN LO SOLUCIONA
    public long EventoId { get; set; }

    public string Categoria { get; set; } = "system";
    public long? UsuarioId { get; set; }
    public long? DispositivoId { get; set; }
    public string Accion { get; set; } = "";
    public string? Detalles { get; set; }
    public string Severidad { get; set; } = "info";
    public string? DireccionIp { get; set; }
    public DateTime FechaEvento { get; set; }
}