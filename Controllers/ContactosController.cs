using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CUChatNet.Api.Servicios;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactosController : ControllerBase
{
    private readonly CUChatNetDbContext _db;
    private readonly IContactosServicio _contactosServicio;

    public ContactosController(CUChatNetDbContext db, IContactosServicio contactosServicio)
    {
        _db = db;
        _contactosServicio = contactosServicio;
    }

    // GET: api/contactos/{userId}
    [HttpGet("{userId:long}")]  
    public async Task<ActionResult<IEnumerable<dynamic>>> GetContactos(long userId)
    {
        try
        {
            var contactos = await _db.ContactosUsuario
                .Where(c => c.UsuarioId == userId && c.Activo)
                .Include(c => c.Contacto)
                .ToListAsync();

            var result = contactos.Select(c => new
            {
                id = c.ContactoUsuarioId,
                name = c.Alias ?? (c.Contacto != null ? c.Contacto.Nombre : "Usuario"),
                phone = c.Contacto != null ? c.Contacto.TelefonoCompleto : "",
                photo = c.Contacto?.FotoUrl,
                description = c.Contacto?.Descripcion,
                status = c.Contacto?.EstadoPerfil,
                fromAgenda = c.SincronizadoAgenda
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al obtener contactos", details = ex.Message });
        }
    }

    // POST: api/contactos
    [HttpPost]
    public async Task<IActionResult> AddContacto([FromBody] AddContactoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ContactoPhone))
            return BadRequest(new { error = "El número de teléfono es obligatorio." });

        // Limpiamos el número recibido
        string phoneToSearch = request.ContactoPhone.Trim().Replace(" ", "").Replace("-", "");

        // 1. Buscar al usuario destino
        var targetUser = await _db.Usuarios
            .FirstOrDefaultAsync(u =>
                (u.TelefonoCompleto == phoneToSearch || u.TelefonoCompleto.Contains(phoneToSearch))
                && u.Activo);

        if (targetUser == null)
        {
            return NotFound(new { error = "Número no registrado" });
        }

        // 2. Evitar agregarse a uno mismo
        if (targetUser.UsuarioId == request.UserId)
        {
            return BadRequest(new { error = "No puedes agregarte a ti mismo." });
        }

        // 3. Verificar si ya existe la relación
        var existing = await _db.ContactosUsuario
            .AnyAsync(c => c.UsuarioId == request.UserId && c.ContactoUsuarioId == targetUser.UsuarioId && c.Activo);

        if (existing)
        {
            return BadRequest(new { error = "Este usuario ya es tu contacto." });
        }

        // 4. Crear el nuevo registro
        var nuevoContacto = new ContactoUsuario
        {
            UsuarioId = request.UserId,
            ContactoUsuarioId = targetUser.UsuarioId,
            Alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias.Trim(),
            SincronizadoAgenda = false,
            Activo = true,
            FechaAgregado = DateTime.UtcNow
        };

        _db.ContactosUsuario.Add(nuevoContacto);

        // 5. Registro en Bitácora 
        // Cambiado a "message" para cumplir con el CHECK constraint de tu DB
        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "message",
            UsuarioId = request.UserId,
            Accion = "Contacto agregado",
            Detalles = $"Teléfono: {phoneToSearch}",
            Severidad = "info",
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            FechaEvento = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Contacto agregado correctamente",
            contactoId = targetUser.UsuarioId
        });
    }

    [HttpPut("{contactoUsuarioId:long}")]
    public async Task<IActionResult> EditarContacto(long contactoUsuarioId, [FromBody] EditContactoRequest request)
    {
        var ok = await _contactosServicio.EditarContactoAsync(
            request.UserId,
            contactoUsuarioId,
            request.Alias,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!ok)
            return NotFound(new { error = "Contacto no encontrado o no pertenece al usuario." });

        return Ok(new { message = "Contacto editado correctamente." });
    }

    [HttpDelete("{contactoUsuarioId:long}")]
    public async Task<IActionResult> EliminarContacto(long contactoUsuarioId, [FromQuery] long userId)
    {
        var ok = await _contactosServicio.EliminarContactoAsync(
            userId,
            contactoUsuarioId,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!ok)
            return NotFound(new { error = "Contacto no encontrado o no pertenece al usuario." });

        return Ok(new { message = "Contacto eliminado correctamente." });
    }
}



// Si esta clase ya existe en tu carpeta DTOs, puedes borrarla de aquí para evitar errores de duplicado
public class AddContactoRequest
{
    public long UserId { get; set; }
    public string ContactoPhone { get; set; } = string.Empty;

    public string? Alias { get; set; }
}