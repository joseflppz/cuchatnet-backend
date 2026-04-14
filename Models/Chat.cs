using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CUChatNet.Api.Models
{
    public class Chat
    {
        [Key]
        public long ChatId { get; set; }

        public string? Nombre { get; set; }
        public string? FotoUrl { get; set; }
        public string? Descripcion { get; set; }
        public string? CodigoConversacion { get; set; }
        public string? TipoChat { get; set; } // "Individual" o "Grupo"
        public bool EsGrupo { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public long? CreadoPorUsuarioId { get; set; }

        // --- Campos de Permisos y Reglas ---
        public string? Reglas { get; set; }
        public string? PermisoEnviarMensajes { get; set; }
        public string? PermisoEditarInfo { get; set; }

        // --- Relaciones (Navegación) ---
        public ICollection<ChatUsuario> ChatUsuarios { get; set; } = new List<ChatUsuario>();
        public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();
        public ICollection<ChatParticipante> Participantes { get; set; } = new List<ChatParticipante>();
    }
}