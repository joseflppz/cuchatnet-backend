using CUChatNet.Api.Data;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = "AdminOnly")]
public class AdminRolesController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public AdminRolesController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles
            .OrderBy(r => r.FechaCreacion)
            .Select(r => new
            {
                id = r.RolId,
                codigo = r.Codigo,
                nombre = r.Nombre,
                activo = r.Activo,
                fechaCreacion = r.FechaCreacion,
                totalUsuarios = r.UsuarioRoles.Count
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRol([FromBody] RolRequest request)
    {
        if (await _db.Roles.AnyAsync(r => r.Codigo == request.Codigo))
            return BadRequest(new { message = "Ya existe un rol con ese código." });

        var rol = new Rol
        {
            Codigo = request.Codigo.Trim().ToLower(),
            Nombre = request.Nombre.Trim(),
            Activo = request.Activo,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Roles.Add(rol);

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            Accion = "Rol creado",
            Detalles = rol.Nombre,
            Severidad = "info",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return Ok(new { id = rol.RolId, success = true });
    }

    [HttpPut("{rolId:int}")]
    public async Task<IActionResult> UpdateRol(int rolId, [FromBody] RolRequest request)
    {
        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.RolId == rolId);
        if (rol is null) return NotFound();

        if (await _db.Roles.AnyAsync(r => r.Codigo == request.Codigo && r.RolId != rolId))
            return BadRequest(new { message = "Ya existe un rol con ese código." });

        rol.Codigo = request.Codigo.Trim().ToLower();
        rol.Nombre = request.Nombre.Trim();
        rol.Activo = request.Activo;

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            Accion = "Rol actualizado",
            Detalles = rol.Nombre,
            Severidad = "info",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{rolId:int}")]
    public async Task<IActionResult> DeleteRol(int rolId)
    {
        var rol = await _db.Roles
            .Include(r => r.UsuarioRoles)
            .FirstOrDefaultAsync(r => r.RolId == rolId);

        if (rol is null) return NotFound();

        if (rol.UsuarioRoles.Any())
            return BadRequest(new { message = "No se puede eliminar un rol que tiene usuarios asignados." });

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            Accion = "Rol eliminado",
            Detalles = rol.Nombre,
            Severidad = "warning",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        _db.Roles.Remove(rol);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public record RolRequest(string Codigo, string Nombre, bool Activo);