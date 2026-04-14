namespace CUChatNet.Api.Models;

public class PreferenciasUsuario
{
    public int PreferenciaId { get; set; }
    public long UsuarioId { get; set; }

    public byte FotoPerfilVisible { get; set; }
    public byte EstadoVisible { get; set; }
    public byte UltimaVezVisible { get; set; }
    public bool ConfirmacionesLectura { get; set; }

    public bool NotificacionesEmail { get; set; }
    public bool NotificacionesPush { get; set; }
    public bool SonidoNotificaciones { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
