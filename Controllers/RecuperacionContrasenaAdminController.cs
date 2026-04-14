using System.Security.Cryptography;
using System.Reflection;
using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using CUChatNet.Api.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/recuperacion-contrasena")]
public class RecuperacionContrasenaAdminController : ControllerBase
{
    private readonly CUChatNetDbContext _db;
    private readonly ICorreoRecuperacionAdminServicio _correoServicio;

    public RecuperacionContrasenaAdminController(
        CUChatNetDbContext db,
        ICorreoRecuperacionAdminServicio correoServicio)
    {
        _db = db;
        _correoServicio = correoServicio;
    }

    [HttpPost("enviar-codigo")]
    public async Task<IActionResult> EnviarCodigo([FromBody] EnviarCodigoRecuperacionAdminRequest request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Correo))
                return BadRequest(new RespuestaSimpleDto("El correo es requerido."));

            var correo = NormalizarCorreo(request.Correo);

            var usuario = await _db.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == correo && !x.Eliminado);

            if (usuario is null)
            {
                return Ok(new RespuestaSimpleDto("Si el correo existe, se envió un código."));
            }

            var registrosPendientes = await _db.CodigosRecuperacionAdmin
                .Where(x => x.Correo == correo && !x.Usado)
                .ToListAsync();

            foreach (var item in registrosPendientes)
            {
                item.Usado = true;
                item.FechaUso = DateTime.UtcNow;
            }

            var codigo = GenerarCodigoNumerico();

            var nuevoCodigo = new CodigoRecuperacionAdmin
            {
                UsuarioId = usuario.UsuarioId,
                Correo = correo,
                Codigo = codigo,
                TokenRecuperacion = null,
                Verificado = false,
                Usado = false,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(15)
            };

            _db.CodigosRecuperacionAdmin.Add(nuevoCodigo);

            _db.BitacoraEventos.Add(new BitacoraEvento
            {
                Categoria = "system",
                UsuarioId = usuario.UsuarioId,
                Accion = "Solicitud recuperación contraseña admin",
                Detalles = $"Se envió código al correo {correo}.",
                Severidad = "info",
                DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                FechaEvento = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await _correoServicio.EnviarCodigoAsync(
                correo,
                usuario.Nombre ?? "Administrador",
                codigo
            );

            return Ok(new RespuestaSimpleDto("Se envió un código al correo indicado."));
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

    [HttpPost("verificar-codigo")]
    public async Task<IActionResult> VerificarCodigo([FromBody] VerificarCodigoRecuperacionAdminRequest request)
    {
        try
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Correo) ||
                string.IsNullOrWhiteSpace(request.Codigo))
            {
                return BadRequest(new RespuestaSimpleDto("Correo y código son requeridos."));
            }

            var correo = NormalizarCorreo(request.Correo);
            var codigo = request.Codigo.Trim();

            var registro = await _db.CodigosRecuperacionAdmin
                .Where(x =>
                    x.Correo == correo &&
                    x.Codigo == codigo &&
                    !x.Usado &&
                    x.FechaExpiracion >= DateTime.UtcNow)
                .OrderByDescending(x => x.CodigoRecuperacionAdminId)
                .FirstOrDefaultAsync();

            if (registro is null)
                return BadRequest(new RespuestaSimpleDto("Código inválido o vencido."));

            registro.Verificado = true;
            registro.FechaVerificacion = DateTime.UtcNow;
            registro.TokenRecuperacion = Guid.NewGuid().ToString("N");

            _db.BitacoraEventos.Add(new BitacoraEvento
            {
                Categoria = "system",
                UsuarioId = registro.UsuarioId,
                Accion = "Código recuperación verificado",
                Detalles = $"Se verificó correctamente el código para {correo}.",
                Severidad = "info",
                DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                FechaEvento = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new VerificarCodigoRecuperacionAdminResponse(
                "Código verificado correctamente.",
                registro.TokenRecuperacion!
            ));
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

    [HttpPost("cambiar-contrasena")]
    public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaAdminRequest request)
    {
        try
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.TokenRecuperacion) ||
                string.IsNullOrWhiteSpace(request.NuevaContrasena) ||
                string.IsNullOrWhiteSpace(request.ConfirmarContrasena))
            {
                return BadRequest(new RespuestaSimpleDto("Todos los campos son requeridos."));
            }

            if (request.NuevaContrasena != request.ConfirmarContrasena)
                return BadRequest(new RespuestaSimpleDto("Las contraseñas no coinciden."));

            if (request.NuevaContrasena.Length < 8)
                return BadRequest(new RespuestaSimpleDto("La nueva contraseña debe tener al menos 8 caracteres."));

            var registro = await _db.CodigosRecuperacionAdmin
                .Where(x =>
                    x.TokenRecuperacion == request.TokenRecuperacion &&
                    x.Verificado &&
                    !x.Usado &&
                    x.FechaExpiracion >= DateTime.UtcNow)
                .OrderByDescending(x => x.CodigoRecuperacionAdminId)
                .FirstOrDefaultAsync();

            if (registro is null)
                return BadRequest(new RespuestaSimpleDto("El enlace o token de recuperación ya no es válido."));

            var cuenta = await _db.CuentasAcceso
                .FirstOrDefaultAsync(x => x.UsuarioId == registro.UsuarioId);

            if (cuenta is null)
                return NotFound(new RespuestaSimpleDto("No se encontró la cuenta de acceso del usuario."));

            var actualizada = AsignarNuevaContrasena(cuenta, request.NuevaContrasena);

            if (!actualizada)
            {
                return BadRequest(new RespuestaSimpleDto(
                    "No se pudo actualizar la contraseña porque no se encontró un campo compatible en CuentasAcceso."
                ));
            }

            IntentarAsignarBooleano(cuenta, "DebeCambiarContrasena", false);
            IntentarAsignarEntero(cuenta, "IntentosFallidos", 0);
            IntentarAsignarFechaNullable(cuenta, "BloqueadoHasta", null);
            IntentarAsignarBooleano(cuenta, "Activo", true);
            IntentarAsignarTexto(cuenta, "EstadoCuenta", "active");

            registro.Usado = true;
            registro.FechaUso = DateTime.UtcNow;

            _db.BitacoraEventos.Add(new BitacoraEvento
            {
                Categoria = "system",
                UsuarioId = registro.UsuarioId,
                Accion = "Contraseña admin restablecida",
                Detalles = $"Se cambió la contraseña mediante recuperación para {registro.Correo}.",
                Severidad = "info",
                DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                FechaEvento = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new RespuestaSimpleDto("La contraseña se cambió correctamente."));
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

    private static string NormalizarCorreo(string correo)
    {
        return correo.Trim().ToLower();
    }

    private static string GenerarCodigoNumerico()
    {
        var numero = RandomNumberGenerator.GetInt32(100000, 999999);
        return numero.ToString();
    }

    private static bool AsignarNuevaContrasena(CuentaAcceso cuenta, string nuevaContrasena)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(nuevaContrasena);

        // Intenta primero campos hash comunes
        if (IntentarAsignarTexto(cuenta, "PasswordHash", hash)) return true;
        if (IntentarAsignarTexto(cuenta, "ContrasenaHash", hash)) return true;
        if (IntentarAsignarTexto(cuenta, "HashContrasena", hash)) return true;
        if (IntentarAsignarTexto(cuenta, "ClaveHash", hash)) return true;

        // Fallback por si tu modelo usa otro nombre
        if (IntentarAsignarTexto(cuenta, "Contrasena", hash)) return true;
        if (IntentarAsignarTexto(cuenta, "Clave", hash)) return true;

        return false;
    }

    private static bool IntentarAsignarTexto(object objeto, string propiedad, string? valor)
    {
        var prop = objeto.GetType().GetProperty(propiedad, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null || !prop.CanWrite || prop.PropertyType != typeof(string))
            return false;

        prop.SetValue(objeto, valor);
        return true;
    }

    private static bool IntentarAsignarBooleano(object objeto, string propiedad, bool valor)
    {
        var prop = objeto.GetType().GetProperty(propiedad, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null || !prop.CanWrite || prop.PropertyType != typeof(bool))
            return false;

        prop.SetValue(objeto, valor);
        return true;
    }

    private static bool IntentarAsignarEntero(object objeto, string propiedad, int valor)
    {
        var prop = objeto.GetType().GetProperty(propiedad, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null || !prop.CanWrite)
            return false;

        if (prop.PropertyType == typeof(int))
        {
            prop.SetValue(objeto, valor);
            return true;
        }

        if (prop.PropertyType == typeof(int?))
        {
            prop.SetValue(objeto, (int?)valor);
            return true;
        }

        return false;
    }

    private static bool IntentarAsignarFechaNullable(object objeto, string propiedad, DateTime? valor)
    {
        var prop = objeto.GetType().GetProperty(propiedad, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null || !prop.CanWrite)
            return false;

        if (prop.PropertyType == typeof(DateTime?))
        {
            prop.SetValue(objeto, valor);
            return true;
        }

        return false;
    }
}