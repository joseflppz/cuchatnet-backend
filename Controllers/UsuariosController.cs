using CUChatNet.Api.Data;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CUChatNet.Api.Dtos;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsuariosController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public UsuariosController(CUChatNetDbContext db)
    {
        _db = db;
    }

    // ===============================
    // 🔹 OBTENER TODOS (Contactos)
    // ===============================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // 1. Traemos los datos de la DB primero
        var usuariosDb = await _db.Usuarios
            .Where(u => !u.Eliminado)
            .ToListAsync();

        // 2. Formateamos en memoria para evitar errores de traducción a SQL
        var resultado = usuariosDb.Select(u => new
        {
            Id = u.UsuarioId,
            UsuarioId = u.UsuarioId,
            u.Nombre,
            u.FotoUrl,
            Telefono = u.ExtensionPais + u.NumeroTelefono,
            u.EstadoPerfil,
            u.Descripcion,
            ActivityStatus = u.Activo ? "online" : "offline" // ✅ Ahora sí funciona
        });

        return Ok(resultado);
    }

    // ===============================
    // 🔹 OBTENER POR ID
    // ===============================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var u = await _db.Usuarios
            .Where(u => u.UsuarioId == id && !u.Eliminado)
            .FirstOrDefaultAsync();

        if (u == null)
            return NotFound();

        // Formateamos la respuesta del objeto individual
        var result = new
        {
            u.UsuarioId,
            u.Nombre,
            u.FotoUrl,
            Telefono = u.ExtensionPais + u.NumeroTelefono,
            u.EstadoPerfil,
            u.Descripcion,
            u.Activo,
            ActivityStatus = u.Activo ? "online" : "offline"
        };

        return Ok(result);
    }

    // ===============================
    // 🔹 ACTUALIZAR PERFIL
    // ===============================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Usuarios.FindAsync(id);

        if (user == null || user.Eliminado)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Nombre = request.Name;

        var estadosValidos = new[] { "available", "away", "busy", "offline" };
        if (!string.IsNullOrWhiteSpace(request.Status) && estadosValidos.Contains(request.Status))
        {
            user.EstadoPerfil = request.Status;
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
            user.Descripcion = request.Description;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Usuario actualizado correctamente" });
    }

    // ===============================
    // 🔹 ELIMINAR (LÓGICO)
    // ===============================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var user = await _db.Usuarios.FindAsync(id);
        if (user == null) return NotFound();

        user.Eliminado = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Usuario eliminado" });
    }
}