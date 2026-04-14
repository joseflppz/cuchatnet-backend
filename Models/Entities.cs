using System.ComponentModel.DataAnnotations.Schema;

namespace CUChatNet.Api.Models;


public class Rol
{
    public int RolId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

public class Usuario
{
    public long UsuarioId { get; set; }
    public string ExtensionPais { get; set; } = "";
    public string NumeroTelefono { get; set; } = "";
    [Column("TelefonoCompleto")] // <--- Esto asegura que use el nombre real de SQL
    public string? TelefonoCompleto { get; set; }
    public string? Email { get; set; }
    public string Nombre { get; set; } = "";
    public string? FotoUrl { get; set; }
    public string? Descripcion { get; set; }
    public string EstadoPerfil { get; set; } = "available";
    public bool Verificado { get; set; }
    public bool Activo { get; set; }
    public bool Eliminado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaUltimoAcceso { get; set; }

    public CuentaAcceso? CuentaAcceso { get; set; }
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

public class CuentaAcceso
{
    public long CuentaAccesoId { get; set; }
    public long UsuarioId { get; set; }
    public string? NombreUsuario { get; set; }
    public string? HashContrasena { get; set; }
    public string? Sal { get; set; }
    public bool UsaOtp { get; set; }
    public bool DebeCambiarContrasena { get; set; }
    public string EstadoCuenta { get; set; } = "active";
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public bool Activo { get; set; }
    public Usuario Usuario { get; set; } = null!;
}

public class UsuarioRol
{
    public long UsuarioRolId { get; set; }
    public long UsuarioId { get; set; }
    public int RolId { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}



public class ClaveDispositivo
{
    public long ClaveDispositivoId { get; set; }
    public long DispositivoId { get; set; }
    public string ClavePublica { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public int VersionClave { get; set; }
    public bool Activa { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public DateTime? FechaUltimaRotacion { get; set; }
}

public class VerificacionSms
{
    public long VerificacionId { get; set; }
    public long? UsuarioId { get; set; }
    public long? DispositivoId { get; set; }
    public string ExtensionPais { get; set; } = "";
    public string NumeroTelefono { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? ProveedorSms { get; set; }
    public string Estado { get; set; } = "pendiente";
    public int Intentos { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public DateTime? FechaVerificacion { get; set; }
}

public class ContactoUsuario
{
    public long ContactoRelacionId { get; set; }
    public long UsuarioId { get; set; }
    public long ContactoUsuarioId { get; set; }
    public string? Alias { get; set; }
    public bool SincronizadoAgenda { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaAgregado { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Usuario Contacto { get; set; } = null!;
}



public class ChatParticipante
{
    public long ChatParticipanteId { get; set; }
    public long ChatId { get; set; }
    public long UsuarioId { get; set; }
    public string Rol { get; set; } = "member";
    public bool Fijado { get; set; }
    public bool Archivado { get; set; }
    public bool Silenciado { get; set; }
    public DateTime FechaUnion { get; set; }
    public DateTime? FechaSalida { get; set; }
    public DateTime? FechaUltimaLectura { get; set; }
    public DateTime? FechaUltimaLimpieza { get; set; }
    public bool Activo { get; set; }
    public Chat Chat { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

public class Mensaje
{
    public long MensajeId { get; set; }
    public long ChatId { get; set; }
    public long RemitenteUsuarioId { get; set; }
    public long? DispositivoId { get; set; }
    public long? EsRespuestaA { get; set; }
    public string TipoMensaje { get; set; } = "text";
    public string? Contenido { get; set; }
    public bool Encriptado { get; set; }
    public bool Editado { get; set; }
    public bool EliminadoParaTodos { get; set; }
    public string EstadoServidor { get; set; } = "sent";
    public string? IpOrigen { get; set; }
    public DateTime FechaEnvio { get; set; }
    public DateTime? FechaEdicion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public Chat Chat { get; set; } = null!;
    public Usuario RemitenteUsuario { get; set; } = null!;
    public ICollection<MensajeAdjunto> Adjuntos { get; set; } = new List<MensajeAdjunto>();
    public ICollection<MensajeEstado> Estados { get; set; } = new List<MensajeEstado>();
}

public class MensajeAdjunto
{
    public long AdjuntoId { get; set; }
    public long MensajeId { get; set; }
    public string TipoAdjunto { get; set; } = "";
    public string UrlArchivo { get; set; } = "";
    public string? NombreArchivo { get; set; }
    public string? MimeType { get; set; }
    public long? TamanoBytes { get; set; }
    public int? DuracionSegundos { get; set; }
    public int? Ancho { get; set; }
    public int? Alto { get; set; }
    public DateTime FechaCreacion { get; set; }
    public Mensaje Mensaje { get; set; } = null!;
}

public class MensajeEstado
{
    public long MensajeEstadoId { get; set; }
    public long MensajeId { get; set; }
    public long UsuarioId { get; set; }
    public string Estado { get; set; } = "sent";
    public DateTime? FechaEntrega { get; set; }
    public DateTime? FechaVista { get; set; }
    public bool EliminadoParaMi { get; set; }
    public DateTime? FechaEliminadoParaMi { get; set; }
    public Mensaje Mensaje { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

public class EstadoUsuario
{
    public long EstadoUsuarioId { get; set; }
    public long UsuarioId { get; set; }
    public string TipoContenido { get; set; } = "text";
    public string Contenido { get; set; } = "";
    public string? MediaUrl { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public bool EliminadoManual { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public bool Activo { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public ICollection<EstadoVisualizacion> Visualizaciones { get; set; } = new List<EstadoVisualizacion>();
}

public class EstadoVisualizacion
{
    public long EstadoVisualizacionId { get; set; }
    public long EstadoUsuarioId { get; set; }
    public long UsuarioId { get; set; }
    public DateTime FechaVisualizacion { get; set; }
    public EstadoUsuario EstadoUsuario { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}




public class PoliticaSeguridad
{
    public int PoliticaSeguridadId { get; set; }
    public int BloquearTrasIntentosFallidos { get; set; }
    public int DuracionBloqueoMinutos { get; set; }
    public int RotacionClavesDias { get; set; }
    public bool DeteccionCambioDispositivo { get; set; }
    public bool VerificacionIdentidad { get; set; }
    public bool RequiereE2E { get; set; }
    public int TimeoutLoginSospechosoMin { get; set; }
    public int MaxDispositivosPorUsuario { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public bool Activa { get; set; }
}


public class SendOrCreateMessageRequest
{
    public long SenderId { get; set; }
    public long ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
}
