using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/users/{userId:long}/contacts")]
public class ContactsController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public ContactsController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContactDto>>> GetContacts(long userId)
    {
        var contacts = await _db.ContactosUsuario
            .Where(x => x.UsuarioId == userId && x.Activo)
            .Include(x => x.Contacto)
            .Select(x => new ContactDto(
                x.Contacto.UsuarioId,
                x.Alias ?? x.Contacto.Nombre,
                x.Contacto.ExtensionPais + x.Contacto.NumeroTelefono,
                x.Contacto.FotoUrl,
                x.Contacto.Descripcion,
                x.Contacto.EstadoPerfil,
                x.SincronizadoAgenda
            ))
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(contacts);
    }

    [HttpPost]
    public async Task<IActionResult> AddContact(long userId, [FromBody] AddContactRequest request)
    {
        if (userId != request.UserId)
            return BadRequest(new { error = "El userId de la ruta no coincide con el cuerpo." });

        if (request.UserId == request.ContactUserId)
            return BadRequest(new { error = "No puedes agregarte a ti mismo." });

        var exists = await _db.ContactosUsuario.AnyAsync(x =>
            x.UsuarioId == request.UserId &&
            x.ContactoUsuarioId == request.ContactUserId &&
            x.Activo);

        if (exists)
            return BadRequest(new { error = "Ese contacto ya existe." });

        _db.ContactosUsuario.Add(new ContactoUsuario
        {
            UsuarioId = request.UserId,
            ContactoUsuarioId = request.ContactUserId,
            SincronizadoAgenda = request.FromAgenda,
            Activo = true,
            FechaAgregado = DateTime.UtcNow
        });

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "system",
            UsuarioId = request.UserId,
            Accion = "Contacto agregado",
            Detalles = $"Contacto usuario id: {request.ContactUserId}",
            Severidad = "info",
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            FechaEvento = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Contacto agregado." });
    }
}
