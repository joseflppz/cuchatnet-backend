using System.Linq;
using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/ajustes")]
public class AjustesController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public AjustesController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet("{usuarioId:long}")]
    public async Task<IActionResult> ObtenerAjustes(long usuarioId)
    {
        try
        {
            var usuario = await _db.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && !x.Eliminado);

            if (usuario is null)
                return NotFound(new { error = "Usuario no encontrado." });

            var preferencias = await ObtenerOCrearPreferencias(usuarioId);
            var seguridad = await ConstruirSeguridad(usuarioId);

            var perfilDto = new PerfilAjustesDto(
                usuario.UsuarioId,
                usuario.Nombre,
                usuario.Email,
                usuario.TelefonoCompleto,
                usuario.Descripcion,
                usuario.FotoUrl,
                usuario.EstadoPerfil
            );

            var preferenciasDto = new PreferenciasUsuarioDto(
                usuarioId,
                preferencias.FotoPerfilVisible,
                preferencias.EstadoVisible,
                preferencias.UltimaVezVisible,
                preferencias.ConfirmacionesLectura,
                preferencias.NotificacionesEmail,
                preferencias.NotificacionesPush,
                preferencias.SonidoNotificaciones,
                preferencias.FechaActualizacion
            );

            var response = new AjustesUsuarioDto(perfilDto, preferenciasDto, seguridad);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    [HttpPut("perfil")]
    public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarPerfilRequest request)
    {
        try
        {
            if (request is null ||
                request.UsuarioId <= 0 ||
                string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { error = "Usuario, nombre y estado son requeridos." });
            }

            var usuario = await _db.Usuarios
                .FirstOrDefaultAsync(x => x.UsuarioId == request.UsuarioId && !x.Eliminado);

            if (usuario is null)
                return NotFound(new { error = "Usuario no encontrado." });

            usuario.Nombre = request.Name.Trim();
            usuario.Descripcion = request.Description;
            usuario.FotoUrl = request.PhotoUrl;
            usuario.EstadoPerfil = request.Status.Trim();
            usuario.FechaUltimoAcceso = DateTime.UtcNow;

            RegistrarBitacora(
                usuario.UsuarioId,
                "system",
                "Perfil actualizado",
                $"Nombre: {usuario.Nombre}, Estado: {usuario.EstadoPerfil}"
            );

            await _db.SaveChangesAsync();

            var response = new PerfilAjustesDto(
                usuario.UsuarioId,
                usuario.Nombre,
                usuario.Email,
                usuario.TelefonoCompleto,
                usuario.Descripcion,
                usuario.FotoUrl,
                usuario.EstadoPerfil
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    [HttpPut("preferencias")]
    public async Task<IActionResult> ActualizarPreferencias([FromBody] ActualizarPreferenciasRequest request)
    {
        try
        {
            if (request is null || request.UsuarioId <= 0)
                return BadRequest(new { error = "UsuarioId es requerido." });

            if (!EsVisibilidadValida(request.FotoPerfilVisible) ||
                !EsVisibilidadValida(request.EstadoVisible) ||
                !EsVisibilidadValida(request.UltimaVezVisible))
            {
                return BadRequest(new { error = "Los valores de visibilidad solo pueden ser 0, 1 o 2." });
            }

            var usuarioExiste = await _db.Usuarios
                .AnyAsync(x => x.UsuarioId == request.UsuarioId && !x.Eliminado);

            if (!usuarioExiste)
                return NotFound(new { error = "Usuario no encontrado." });

            var preferencias = await _db.PreferenciasUsuario
                .FirstOrDefaultAsync(x => x.UsuarioId == request.UsuarioId);

            if (preferencias is null)
            {
                preferencias = new PreferenciasUsuario
                {
                    UsuarioId = request.UsuarioId,
                    FotoPerfilVisible = request.FotoPerfilVisible,
                    EstadoVisible = request.EstadoVisible,
                    UltimaVezVisible = request.UltimaVezVisible,
                    ConfirmacionesLectura = request.ConfirmacionesLectura,
                    NotificacionesEmail = request.NotificacionesEmail,
                    NotificacionesPush = request.NotificacionesPush,
                    SonidoNotificaciones = request.SonidoNotificaciones,
                    FechaActualizacion = DateTime.UtcNow
                };

                _db.PreferenciasUsuario.Add(preferencias);
            }
            else
            {
                preferencias.FotoPerfilVisible = request.FotoPerfilVisible;
                preferencias.EstadoVisible = request.EstadoVisible;
                preferencias.UltimaVezVisible = request.UltimaVezVisible;
                preferencias.ConfirmacionesLectura = request.ConfirmacionesLectura;
                preferencias.NotificacionesEmail = request.NotificacionesEmail;
                preferencias.NotificacionesPush = request.NotificacionesPush;
                preferencias.SonidoNotificaciones = request.SonidoNotificaciones;
                preferencias.FechaActualizacion = DateTime.UtcNow;
            }

            RegistrarBitacora(
                request.UsuarioId,
                "system",
                "Preferencias actualizadas",
                "Se actualizaron privacidad y notificaciones."
            );

            await _db.SaveChangesAsync();

            var response = new PreferenciasUsuarioDto(
                preferencias.UsuarioId,
                preferencias.FotoPerfilVisible,
                preferencias.EstadoVisible,
                preferencias.UltimaVezVisible,
                preferencias.ConfirmacionesLectura,
                preferencias.NotificacionesEmail,
                preferencias.NotificacionesPush,
                preferencias.SonidoNotificaciones,
                preferencias.FechaActualizacion
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }


    [HttpGet("seguridad/{usuarioId:long}")]
    public async Task<IActionResult> ObtenerSeguridad(long usuarioId)
    {
        try
        {
            var usuarioExiste = await _db.Usuarios
                .AnyAsync(x => x.UsuarioId == usuarioId && !x.Eliminado);

            if (!usuarioExiste)
                return NotFound(new { error = "Usuario no encontrado." });

            var seguridad = await ConstruirSeguridad(usuarioId);
            return Ok(seguridad);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    private void RegistrarBitacora(long? usuarioId, string categoria, string accion, string? detalles)
    {
        _db.BitacoraEventos.Add(new BitacoraEvento
        {
            Categoria = categoria,
            UsuarioId = usuarioId,
            DispositivoId = null,
            Accion = accion,
            Detalles = detalles,
            Severidad = "info",
            DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            FechaEvento = DateTime.UtcNow
        });
    }

    private static bool EsVisibilidadValida(byte valor)
    {
        return valor <= 2;
    }

    private async Task<PreferenciasUsuario> ObtenerOCrearPreferencias(long usuarioId)
    {
        var preferencias = await _db.PreferenciasUsuario
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);

        if (preferencias is not null)
            return preferencias;

        preferencias = new PreferenciasUsuario
        {
            UsuarioId = usuarioId,
            FotoPerfilVisible = 1,
            EstadoVisible = 1,
            UltimaVezVisible = 1,
            ConfirmacionesLectura = true,
            NotificacionesEmail = true,
            NotificacionesPush = true,
            SonidoNotificaciones = true,
            FechaActualizacion = DateTime.UtcNow
        };

        _db.PreferenciasUsuario.Add(preferencias);
        await _db.SaveChangesAsync();

        return preferencias;
    }

    private async Task<SeguridadUsuarioDto> ConstruirSeguridad(long usuarioId)
    {
        var cuenta = await _db.CuentasAcceso
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);

        var politica = await _db.PoliticasSeguridad
            .AsNoTracking()
            .Where(x => x.Activa)
            .OrderByDescending(x => x.FechaActualizacion)
            .FirstOrDefaultAsync();

        var dispositivos = await _db.DispositivosUsuario
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.FechaUltimoUso)
            .ToListAsync();

        var dispositivosIds = dispositivos.Select(x => x.DispositivoId).ToList();

        var claves = await _db.ClavesDispositivo
            .AsNoTracking()
            .Where(x => dispositivosIds.Contains(x.DispositivoId))
            .GroupBy(x => x.DispositivoId)
            .Select(g => g
                .OrderByDescending(c => c.FechaUltimaRotacion ?? c.FechaGeneracion)
                .FirstOrDefault()!)
            .ToListAsync();

        var dispositivosDto = dispositivos.Select(d =>
        {
            var clave = claves.FirstOrDefault(x => x.DispositivoId == d.DispositivoId);

            return new DispositivoSeguridadDto(
                (int)d.DispositivoId,
                d.NombreDispositivo,
                d.Plataforma,
                d.SistemaOperativo,
                d.Navegador,
                d.CodigoSeguridad,
                d.UltimaIp,
                d.Verificado,
                d.Confiable,
                d.Activo,
                d.FechaRegistro,
                d.FechaUltimoUso,
                clave?.Fingerprint,
                clave?.VersionClave,
                clave?.Activa,
                clave?.FechaUltimaRotacion
            );
        }).ToList();

        return new SeguridadUsuarioDto(
            usuarioId,
            cuenta?.UsaOtp ?? false,
            cuenta?.DebeCambiarContrasena ?? false,
            cuenta?.EstadoCuenta ?? "active",
            cuenta?.IntentosFallidos ?? 0,
            cuenta?.BloqueadoHasta,
            cuenta?.UltimoLogin,
            cuenta?.Activo ?? true,
            politica != null ? politica.BloquearTrasIntentosFallidos == 1 : true,
            politica?.DuracionBloqueoMinutos ?? 15,
            politica?.RotacionClavesDias ?? 30,
            politica?.DeteccionCambioDispositivo ?? true,
            politica?.VerificacionIdentidad ?? true,
            politica?.RequiereE2E ?? true,
            politica?.TimeoutLoginSospechosoMin ?? 10,
            politica?.MaxDispositivosPorUsuario ?? 5,
            dispositivosDto
        );
    }
}
