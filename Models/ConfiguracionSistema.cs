using System;
using System.ComponentModel.DataAnnotations;

namespace CUChatNet.Api.Models
{
    public class ConfiguracionSistema
    {
        [Key] // 🔥 SOLUCIONA EL ERROR DE EF CORE
        public int ConfiguracionId { get; set; }

        public string NombreAplicacion { get; set; } = "CUChatNet";
        public int MaxGrupo { get; set; }
        public int TimeoutMensajesMinutos { get; set; }
        public int MaxArchivoMB { get; set; }
        public string? ServidorSmtp { get; set; }
        public int? PuertoSmtp { get; set; }
        public string? UsuarioSmtp { get; set; }
        public byte[]? ClaveSmtpEncriptada { get; set; }
        public string? ProveedorSms { get; set; }
        public byte[]? ApiKeySmsEncriptada { get; set; }
        public string? ApiEndpoint { get; set; }
        public byte[]? ApiTokenEncriptado { get; set; }
        public bool ModoMantenimiento { get; set; }
        public bool E2ERequerido { get; set; }
        public int AutoArchivarInactividadDias { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public bool Activo { get; set; }
    }
}