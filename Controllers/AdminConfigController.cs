using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/config")]
public class AdminConfigController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public AdminConfigController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<AdminConfigDto>> GetConfig()
    {
        var config = await _db.ConfiguracionSistema.OrderByDescending(x => x.ConfiguracionId).FirstOrDefaultAsync();
        if (config is null)
            return NotFound();

        return Ok(new AdminConfigDto(
            config.NombreAplicacion,
            config.MaxGrupo,
            config.TimeoutMensajesMinutos,
            config.MaxArchivoMB,
            config.ServidorSmtp,
            config.PuertoSmtp,
            config.ProveedorSms,
            config.ApiEndpoint,
            config.ModoMantenimiento,
            config.E2ERequerido,
            config.AutoArchivarInactividadDias
        ));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateAdminConfigRequest request)
    {
        var config = await _db.ConfiguracionSistema.OrderByDescending(x => x.ConfiguracionId).FirstOrDefaultAsync();
        if (config is null)
        {
            config = new ConfiguracionSistema
            {
                Activo = true,
                FechaActualizacion = DateTime.UtcNow
            };
            _db.ConfiguracionSistema.Add(config);
        }

        config.NombreAplicacion = request.AppName;
        config.MaxGrupo = request.MaxGroupSize;
        config.TimeoutMensajesMinutos = request.MessageTimeout;
        config.MaxArchivoMB = request.MaxFileSize;
        config.ServidorSmtp = request.SmtpServer;
        config.PuertoSmtp = request.SmtpPort;
        config.ProveedorSms = request.SmsProvider;
        config.ApiEndpoint = request.ApiEndpoint;
        config.ModoMantenimiento = request.MaintenanceMode;
        config.E2ERequerido = request.E2ERequired;
        config.AutoArchivarInactividadDias = request.AutoArchiveInactivity;
        config.FechaActualizacion = DateTime.UtcNow;

        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = "admin",
            Accion = "Configuración del sistema actualizada",
            Detalles = request.AppName,
            Severidad = "info",
            FechaEvento = DateTime.UtcNow,
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
