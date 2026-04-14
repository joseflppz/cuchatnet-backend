using System;
using System.ComponentModel.DataAnnotations;

namespace CUChatNet.Api.Models
{
    public class DispositivoUsuario
    {
        [Key] // 🔥 SOLUCIÓN
        public long DispositivoId { get; set; }

        public long UsuarioId { get; set; }
        public string NombreDispositivo { get; set; } = "";
        public string? Plataforma { get; set; }
        public string? SistemaOperativo { get; set; }
        public string? Navegador { get; set; }
        public string HuellaDispositivo { get; set; } = "";
        public string? CodigoSeguridad { get; set; }
        public string? UltimaIp { get; set; }
        public bool Verificado { get; set; }
        public bool Confiable { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaUltimoUso { get; set; }
    }
}