using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Helpers;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly CUChatNetDbContext _db;
    private readonly IConfiguration _configuration;

    public AdminUsersController(CUChatNetDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsers()
    {
        var users = await _db.Usuarios
            .Where(u => !u.Eliminado)
            .Include(u => u.CuentaAcceso)
            .OrderByDescending(u => u.FechaCreacion)
            .Select(u => new AdminUserDto(
                u.UsuarioId,
                u.ExtensionPais + u.NumeroTelefono,
                u.Nombre,
                u.CuentaAcceso != null ? u.CuentaAcceso.EstadoCuenta : (u.Activo ? "active" : "inactive"),
                u.FechaCreacion,
                u.FechaUltimoAcceso,
                u.Email
            ))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request)
    {
        var user = new Usuario
        {
            ExtensionPais = request.CountryCode,
            NumeroTelefono = request.PhoneNumber,
            Email = request.Email,
            Nombre = request.Name.Trim(),
            EstadoPerfil = "available",
            Verificado = true,
            Activo = request.Status == "active",
            Eliminado = false,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        _db.CuentasAcceso.Add(new CuentaAcceso
        {
            UsuarioId = user.UsuarioId,
            NombreUsuario = request.CountryCode + request.PhoneNumber,
            EstadoCuenta = request.Status,
            UsaOtp = true,
            Activo = request.Status != "suspended"
        });

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            UsuarioId = user.UsuarioId,
            Accion = "Usuario creado desde administración",
            Detalles = user.Nombre,
            Severidad = "info",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return Ok(new { id = user.UsuarioId, success = true });
    }

    [HttpPut("{userId:long}")]
    public async Task<IActionResult> UpdateUser(long userId, [FromBody] UpdateAdminUserRequest request)
    {
        var user = await _db.Usuarios
            .Include(u => u.CuentaAcceso)
            .FirstOrDefaultAsync(u => u.UsuarioId == userId && !u.Eliminado);
        if (user is null)
            return NotFound();

        user.Nombre = request.Name.Trim();
        user.Email = request.Email;
        user.Activo = request.Status == "active";
        if (user.CuentaAcceso is not null)
            user.CuentaAcceso.EstadoCuenta = request.Status;

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            UsuarioId = userId,
            Accion = "Usuario actualizado desde administración",
            Detalles = request.Name,
            Severidad = "info",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPut("{userId:long}/block")]
    public async Task<IActionResult> BlockUser(long userId)
    {
        var user = await _db.Usuarios
            .Include(u => u.CuentaAcceso)
            .FirstOrDefaultAsync(u => u.UsuarioId == userId && !u.Eliminado);

        if (user is null)
            return NotFound();

        user.Activo = false;
        if (user.CuentaAcceso is not null)
            user.CuentaAcceso.EstadoCuenta = "suspended";

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            UsuarioId = userId,
            Accion = "Usuario bloqueado",
            Detalles = user.Nombre,
            Severidad = "warning",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPut("{userId:long}/unblock")]
    public async Task<IActionResult> UnblockUser(long userId)
    {
        var user = await _db.Usuarios
            .Include(u => u.CuentaAcceso)
            .FirstOrDefaultAsync(u => u.UsuarioId == userId && !u.Eliminado);

        if (user is null)
            return NotFound();

        user.Activo = true;
        if (user.CuentaAcceso is not null)
            user.CuentaAcceso.EstadoCuenta = "active";

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            UsuarioId = userId,
            Accion = "Usuario desbloqueado",
            Detalles = user.Nombre,
            Severidad = "info",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{userId:long}")]
    public async Task<IActionResult> DeleteUser(long userId)
    {
        var user = await _db.Usuarios
            .Include(u => u.CuentaAcceso)
            .Include(u => u.UsuarioRoles)
            .FirstOrDefaultAsync(u => u.UsuarioId == userId);
        
        if (user is null)
            return NotFound();

        // Registrar en bitácora ANTES de eliminar
        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            UsuarioId = userId,
            Accion = "Usuario eliminado permanentemente",
            Detalles = user.Nombre,
            Severidad = "critical",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        // Eliminar CuentaAcceso si existe
        if (user.CuentaAcceso is not null)
            _db.CuentasAcceso.Remove(user.CuentaAcceso);
        
        // Eliminar UsuarioRoles
        if (user.UsuarioRoles.Any())
            _db.UsuarioRoles.RemoveRange(user.UsuarioRoles);
        
        // Eliminar relaciones con chats (ChatParticipantes)
        var chatParticipantes = await _db.ChatParticipantes
            .Where(cp => cp.UsuarioId == userId)
            .ToListAsync();
        if (chatParticipantes.Any())
            _db.ChatParticipantes.RemoveRange(chatParticipantes);
        
        // Marcar mensajes enviados por este usuario como eliminados
        var mensajes = await _db.Mensajes
            .Where(m => m.RemitenteUsuarioId == userId)
            .ToListAsync();
        foreach (var mensaje in mensajes)
        {
            mensaje.Eliminado = true;
        }

        // Eliminar usuario físicamente
        _db.Usuarios.Remove(user);

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
