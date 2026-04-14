using System;
using System.ComponentModel.DataAnnotations;

namespace CUChatNet.Api.Models
{
    public class ContactoSilenciadoEstado
    {
        [Key] // 🔥 ESTO SOLUCIONA EL ERROR
        public long ContactoSilenciadoId { get; set; }

        public long UsuarioId { get; set; }
        public long ContactoUsuarioId { get; set; }
        public DateTime FechaSilenciado { get; set; }
        public bool Activo { get; set; }
    }
}