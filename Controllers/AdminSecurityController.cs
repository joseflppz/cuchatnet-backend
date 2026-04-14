using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/security")]
public class AdminSecurityController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public AdminSecurityController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet("policy")]
    public async Task<ActionResult<SecurityPolicyDto>> GetPolicy()
    {
        var policy = await _db.PoliticasSeguridad.OrderByDescending(x => x.PoliticaSeguridadId).FirstOrDefaultAsync();
        if (policy is null)
            return NotFound();

        return Ok(new SecurityPolicyDto(
            policy.BloquearTrasIntentosFallidos,
            policy.DuracionBloqueoMinutos,
            policy.RotacionClavesDias,
            policy.DeteccionCambioDispositivo,
            policy.VerificacionIdentidad,
            policy.RequiereE2E,
            policy.TimeoutLoginSospechosoMin,
            policy.MaxDispositivosPorUsuario
        ));
    }

    [HttpPut("policy")]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdateSecurityPolicyRequest request)
    {
        var policy = await _db.PoliticasSeguridad.OrderByDescending(x => x.PoliticaSeguridadId).FirstOrDefaultAsync();
        if (policy is null)
        {
            policy = new PoliticaSeguridad { Activa = true };
            _db.PoliticasSeguridad.Add(policy);
        }

        policy.BloquearTrasIntentosFallidos = request.BlockAfterFailedAttempts;
        policy.DuracionBloqueoMinutos = request.BlockDuration;
        policy.RotacionClavesDias = request.KeyRotationDays;
        policy.DeteccionCambioDispositivo = request.EnableDeviceDetection;
        policy.VerificacionIdentidad = request.EnableIdentityVerification;
        policy.RequiereE2E = request.RequireE2EEncryption;
        policy.TimeoutLoginSospechosoMin = request.SuspiciousLoginTimeout;
        policy.MaxDispositivosPorUsuario = request.MaxDevicesPerUser;
        policy.FechaActualizacion = DateTime.UtcNow;

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "security",
            Accion = "Política de seguridad actualizada",
            Detalles = "Actualización manual desde módulo administrativo",
            Severidad = "warning",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<AdminLogDto>>> GetLogs([FromQuery] string? category = null)
    {
        var logsQuery = _db.BitacoraEventos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            logsQuery = logsQuery.Where(x => x.Categoria == category);

        var rawLogs = await logsQuery
            .OrderByDescending(x => x.FechaEvento)
            .Take(300)
            .ToListAsync();

        var userIds = rawLogs.Where(x => x.UsuarioId.HasValue).Select(x => x.UsuarioId!.Value).Distinct().ToList();
        var userMap = await _db.Usuarios
            .Where(u => userIds.Contains(u.UsuarioId))
            .ToDictionaryAsync(u => u.UsuarioId, u => u.Nombre);

        var logs = rawLogs.Select(x => new AdminLogDto(
            x.EventoId,
            x.FechaEvento,
            x.Accion,
            x.UsuarioId.HasValue && userMap.ContainsKey(x.UsuarioId.Value) ? userMap[x.UsuarioId.Value] : "Sistema",
            x.Detalles,
            x.Severidad
        ));

        return Ok(logs);
    }
}
