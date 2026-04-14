using CUChatNet.Api.Data;
using CUChatNet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Servicios
{
    public class ContactosServicio : IContactosServicio
    {
        private readonly CUChatNetDbContext _db;

        public ContactosServicio(CUChatNetDbContext db)
        {
            _db = db;
        }

        public async Task<bool> EditarContactoAsync(long userId, long contactoUsuarioId, string? alias, string? ip)
        {
            var contacto = await _db.ContactosUsuario
                .FirstOrDefaultAsync(c =>
                    c.UsuarioId == userId &&
                    c.ContactoUsuarioId == contactoUsuarioId &&
                    c.Activo);

            if (contacto == null)
                return false;

            contacto.Alias = string.IsNullOrWhiteSpace(alias) ? null : alias.Trim();

            _db.BitacoraEventos.Add(new BitacoraEvento
            {
                Categoria = "message",
                UsuarioId = userId,
                Accion = "Contacto editado",
                Detalles = $"ContactoUsuarioId: {contactoUsuarioId}, Nuevo alias: {contacto.Alias ?? "(sin alias)"}",
                Severidad = "info",
                DireccionIp = ip,
                FechaEvento = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarContactoAsync(long userId, long contactoUsuarioId, string? ip)
        {
            var contacto = await _db.ContactosUsuario
                .FirstOrDefaultAsync(c =>
                    c.UsuarioId == userId &&
                    c.ContactoUsuarioId == contactoUsuarioId &&
                    c.Activo);

            if (contacto == null)
                return false;

            contacto.Activo = false;

            _db.BitacoraEventos.Add(new BitacoraEvento
            {
                Categoria = "message",
                UsuarioId = userId,
                Accion = "Contacto eliminado",
                Detalles = $"ContactoUsuarioId: {contactoUsuarioId}",
                Severidad = "info",
                DireccionIp = ip,
                FechaEvento = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }
    }
}