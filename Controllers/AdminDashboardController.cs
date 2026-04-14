using CUChatNet.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public AdminDashboardController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var totalUsers = await _db.Usuarios.CountAsync(u => !u.Eliminado);
        var activeUsers = await _db.Usuarios.CountAsync(u => !u.Eliminado && u.FechaUltimoAcceso >= DateTime.UtcNow.AddDays(-1));
        var totalGroups = await _db.Chats.CountAsync(c => c.TipoChat == "group" && c.Activo);
        var messagesToday = await _db.Mensajes.CountAsync(m => m.FechaEnvio >= DateTime.UtcNow.Date);

        var recentActivity = await _db.BitacoraEventos
            .OrderByDescending(x => x.FechaEvento)
            .Take(10)
            .Select(x => new
            {
                id = x.EventoId,
                action = x.Accion,
                details = x.Detalles,
                timestamp = x.FechaEvento,
                severity = x.Severidad
            })
            .ToListAsync();

        return Ok(new
        {
            totalUsers,
            activeUsers,
            totalGroups,
            messagesToday,
            recentActivity
        });
    }
}
