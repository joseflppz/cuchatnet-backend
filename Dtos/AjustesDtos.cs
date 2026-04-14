namespace CUChatNet.Api.Dtos;

public record ActualizarPerfilRequest(
    long UsuarioId,
    string Name,
    string? Description,
    string? PhotoUrl,
    string Status
);

public record ActualizarPreferenciasRequest(
    long UsuarioId,
    byte FotoPerfilVisible,
    byte EstadoVisible,
    byte UltimaVezVisible,
    bool ConfirmacionesLectura,
    bool NotificacionesEmail,
    bool NotificacionesPush,
    bool SonidoNotificaciones
);

public record PerfilAjustesDto(
    long UsuarioId,
    string Name,
    string? Email,
    string? Phone,
    string? Description,
    string? PhotoUrl,
    string Status
);

public record PreferenciasUsuarioDto(
    long UsuarioId,
    byte FotoPerfilVisible,
    byte EstadoVisible,
    byte UltimaVezVisible,
    bool ConfirmacionesLectura,
    bool NotificacionesEmail,
    bool NotificacionesPush,
    bool SonidoNotificaciones,
    DateTime FechaActualizacion
);

public record DispositivoSeguridadDto(
    int DispositivoId,
    string NombreDispositivo,
    string? Plataforma,
    string? SistemaOperativo,
    string? Navegador,
    string? CodigoSeguridad,
    string? UltimaIP,
    bool Verificado,
    bool Confiable,
    bool Activo,
    DateTime FechaRegistro,
    DateTime? FechaUltimoUso,
    string? FingerprintClave,
    int? VersionClave,
    bool? ClaveActiva,
    DateTime? FechaUltimaRotacion
);

public record SeguridadUsuarioDto(
    long UsuarioId,
    bool UsaOtp,
    bool DebeCambiarContrasena,
    string EstadoCuenta,
    int IntentosFallidos,
    DateTime? BloqueadoHasta,
    DateTime? UltimoLogin,
    bool Activo,
    bool BloquearTrasIntentosFallidos,
    int DuracionBloqueoMinutos,
    int RotacionClavesDias,
    bool DeteccionCambioDispositivo,
    bool VerificacionIdentidad,
    bool RequiereE2E,
    int TimeoutLoginSospechosoMin,
    int MaxDispositivosPorUsuario,
    List<DispositivoSeguridadDto> Dispositivos
);

public record AjustesUsuarioDto(
    PerfilAjustesDto Perfil,
    PreferenciasUsuarioDto Preferencias,
    SeguridadUsuarioDto Seguridad
);
