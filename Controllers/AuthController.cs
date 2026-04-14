using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Helpers;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly CUChatNetDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(CUChatNetDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("verify/send")]
    public async Task<IActionResult> SendCode([FromBody] SendVerificationRequest request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { error = "El correo es requerido." });

            var email = NormalizeEmail(request.Email);

            var existingUser = await _db.Usuarios
                .FirstOrDefaultAsync(x =>
                    x.Email != null &&
                    x.Email.ToLower() == email &&
                    !x.Eliminado);

            var activeCodes = await _db.CodigosVerificacion
                .Where(x =>
                    x.Email.ToLower() == email &&
                    !x.Usado &&
                    x.Tipo == "VERIFY")
                .ToListAsync();

            foreach (var item in activeCodes)
                item.Usado = true;

            var code = GenerateVerificationCode();

            var codigoVerificacion = new CodigoVerificacion
            {
                Email = email,
                Codigo = code,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(5),
                Usado = false,
                Intentos = 0,
                Tipo = "VERIFY"
            };

            _db.CodigosVerificacion.Add(codigoVerificacion);
            await _db.SaveChangesAsync();

            await SendEmailAsync(
                email,
                "Código de verificación - CUChatNet",
                $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <h2>Código de verificación</h2>
                    <p>Tu código de acceso es:</p>
                    <div style='font-size: 28px; font-weight: bold; letter-spacing: 4px; margin: 16px 0;'>
                        {code}
                    </div>
                    <p>Este código vence en 5 minutos.</p>
                    <p>Si no solicitaste este código, puedes ignorar este correo.</p>
                </div>"
            );

            return Ok(new VerifyResponse(
                true,
                "Código enviado correctamente al correo.",
                existingUser is not null,
                null,
                null
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("verify/check")]
    public async Task<IActionResult> CheckCode([FromBody] CheckVerificationRequest request)
    {
        try
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { error = "Correo y código son requeridos." });
            }

            var email = NormalizeEmail(request.Email);
            var code = request.Code.Trim();

            var codigoGuardado = await _db.CodigosVerificacion
                .Where(x =>
                    x.Email.ToLower() == email &&
                    !x.Usado &&
                    x.Tipo == "VERIFY")
                .OrderByDescending(x => x.FechaCreacion)
                .FirstOrDefaultAsync();

            if (codigoGuardado is null)
                return BadRequest(new { error = "No hay un código activo para este correo." });

            if (codigoGuardado.FechaExpiracion < DateTime.UtcNow)
            {
                codigoGuardado.Usado = true;
                await _db.SaveChangesAsync();
                return BadRequest(new { error = "El código ha expirado." });
            }

            if (!string.Equals(codigoGuardado.Codigo, code, StringComparison.OrdinalIgnoreCase))
            {
                codigoGuardado.Intentos += 1;
                await _db.SaveChangesAsync();
                return BadRequest(new { error = "Código inválido." });
            }

            codigoGuardado.Usado = true;
            await _db.SaveChangesAsync();

            var user = await _db.Usuarios
                .Include(u => u.CuentaAcceso)
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(x =>
                    x.Email != null &&
                    x.Email.ToLower() == email &&
                    !x.Eliminado);

            if (user is null)
            {
                return Ok(new VerifyResponse(
                    true,
                    "Código verificado correctamente. Usuario pendiente de completar perfil.",
                    false,
                    null,
                    null
                ));
            }

            user.Verificado = true;
            user.Activo = true;
            user.FechaUltimoAcceso = DateTime.UtcNow;

            if (user.CuentaAcceso is not null)
            {
                user.CuentaAcceso.UltimoLogin = DateTime.UtcNow;
                user.CuentaAcceso.EstadoCuenta = "active";
                user.CuentaAcceso.Activo = true;
            }

            _db.BitacoraEventos.Add(new BitacoraEvento
            {
                Categoria = "security",
                UsuarioId = user.UsuarioId,
                Accion = "Correo verificado",
                Detalles = $"Correo: {email}",
                Severidad = "info",
                DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                FechaEvento = DateTime.UtcNow,
            });

            await _db.SaveChangesAsync();

            var roleCode = user.UsuarioRoles
                .Select(x => x.Rol.Codigo)
                .FirstOrDefault() ?? "USER";

            var userDto = new AuthUserDto(
                user.UsuarioId,
                user.TelefonoCompleto ?? email,
                user.Email,
                user.Nombre,
                user.FotoUrl,
                user.Descripcion,
                user.EstadoPerfil,
                roleCode,
                user.FechaCreacion,
                user.Activo
            );

            return Ok(new VerifyResponse(
                true,
                "Código verificado correctamente.",
                true,
                userDto,
                null
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("auth/profile-setup")]
    public async Task<IActionResult> SetupProfile([FromBody] SetupProfileRequest request)
    {
        try
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Correo y nombre son requeridos." });
            }

            var email = NormalizeEmail(request.Email);

            string? countryCode = null;
            string? number = null;
            string? fullPhone = null;

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var defaultCountry = _configuration["Verification:DefaultCountryCode"] ?? "+506";
                var parsed = PhoneHelper.Parse(request.Phone, defaultCountry);
                countryCode = parsed.CountryCode;
                number = parsed.Number;
                fullPhone = parsed.FullPhone;
            }

            var user = await _db.Usuarios
                .Include(u => u.CuentaAcceso)
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(x =>
                    x.Email != null &&
                    x.Email.ToLower() == email &&
                    !x.Eliminado);

            if (user is null)
            {
                user = new Usuario
                {
                    ExtensionPais = countryCode,
                    NumeroTelefono = number,
                    Email = email,
                    Nombre = request.Name.Trim(),
                    FotoUrl = request.PhotoUrl,
                    Descripcion = request.Description,
                    EstadoPerfil = string.IsNullOrWhiteSpace(request.Status) ? "available" : request.Status,
                    Verificado = true,
                    Activo = true,
                    Eliminado = false,
                    FechaCreacion = DateTime.UtcNow,
                    FechaUltimoAcceso = DateTime.UtcNow
                };

                _db.Usuarios.Add(user);
                await _db.SaveChangesAsync();

                var userRoleId = await _db.Roles
                    .Where(x => x.Codigo == "USER" && x.Activo)
                    .Select(x => x.RolId)
                    .FirstOrDefaultAsync();

                if (userRoleId <= 0)
                    return StatusCode(500, new { error = "No existe el rol USER." });

                _db.UsuarioRoles.Add(new UsuarioRol
                {
                    UsuarioId = user.UsuarioId,
                    RolId = userRoleId,
                    FechaAsignacion = DateTime.UtcNow,
                });

                _db.CuentasAcceso.Add(new CuentaAcceso
                {
                    UsuarioId = user.UsuarioId,
                    NombreUsuario = email,
                    EstadoCuenta = "active",
                    UsaOtp = true,
                    DebeCambiarContrasena = false,
                    IntentosFallidos = 0,
                    Activo = true,
                    UltimoLogin = DateTime.UtcNow,
                });
            }
            else
            {
                user.Email = email;
                user.Nombre = request.Name.Trim();
                user.FotoUrl = request.PhotoUrl;
                user.Descripcion = request.Description;
                user.EstadoPerfil = string.IsNullOrWhiteSpace(request.Status)
                    ? user.EstadoPerfil
                    : request.Status;

                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    user.ExtensionPais = countryCode;
                    user.NumeroTelefono = number;
                }

                user.Verificado = true;
                user.Activo = true;
                user.FechaUltimoAcceso = DateTime.UtcNow;

                if (user.CuentaAcceso is not null)
                {
                    user.CuentaAcceso.UltimoLogin = DateTime.UtcNow;
                    user.CuentaAcceso.EstadoCuenta = "active";
                    user.CuentaAcceso.Activo = true;
                    user.CuentaAcceso.NombreUsuario = email;
                }
                else
                {
                    _db.CuentasAcceso.Add(new CuentaAcceso
                    {
                        UsuarioId = user.UsuarioId,
                        NombreUsuario = email,
                        EstadoCuenta = "active",
                        UsaOtp = true,
                        DebeCambiarContrasena = false,
                        IntentosFallidos = 0,
                        Activo = true,
                        UltimoLogin = DateTime.UtcNow,
                    });
                }
            }

            await _db.SaveChangesAsync();

            _db.BitacoraEventos.Add(new BitacoraEvento
            {
                Categoria = "system",
                UsuarioId = user.UsuarioId,
                Accion = "Perfil configurado",
                Detalles = $"Usuario: {request.Name} - Correo: {email}",
                Severidad = "info",
                DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                FechaEvento = DateTime.UtcNow,
            });

            await _db.SaveChangesAsync();

            user = await _db.Usuarios
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .FirstAsync(x => x.UsuarioId == user.UsuarioId);

            var roleCode = user.UsuarioRoles
                .Select(x => x.Rol.Codigo)
                .FirstOrDefault() ?? "USER";

            var response = new AuthUserDto(
                user.UsuarioId,
                user.TelefonoCompleto ?? email,
                user.Email,
                user.Nombre,
                user.FotoUrl,
                user.Descripcion,
                user.EstadoPerfil,
                roleCode,
                user.FechaCreacion,
                user.Activo
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("auth/hash/{password}")]
    public IActionResult GetHash(string password)
    {
        return Ok(new { hash = BCrypt.Net.BCrypt.HashPassword(password) });
    }

    [HttpPost("auth/admin-login")]
    public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
    {
        try
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Correo y contraseña son requeridos." });
            }

            var email = NormalizeEmail(request.Email);

            var user = await _db.Usuarios
                .Include(u => u.CuentaAcceso)
                .Include(u => u.UsuarioRoles)
                    .ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(x =>
                    x.Email != null &&
                    x.Email.ToLower() == email &&
                    !x.Eliminado &&
                    x.Activo);

            if (user is null)
                return Unauthorized(new { error = "Credenciales inválidas." });

            var isAdmin = user.UsuarioRoles.Any(x => x.Rol.Codigo == "ADMIN" && x.Rol.Activo);
            if (!isAdmin)
                return Forbid();

            if (user.CuentaAcceso is null || string.IsNullOrWhiteSpace(user.CuentaAcceso.HashContrasena))
                return Unauthorized(new { error = "El administrador no tiene contraseña configurada." });

            var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user.CuentaAcceso.HashContrasena);
            if (!passwordOk)
                return Unauthorized(new { error = "Credenciales inválidas." });

            user.FechaUltimoAcceso = DateTime.UtcNow;
            user.CuentaAcceso.UltimoLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var jwtKey = _configuration["Jwt:Key"]!;
            var jwtIssuer = _configuration["Jwt:Issuer"]!;
            var jwtAudience = _configuration["Jwt:Audience"]!;
            var expirationHours = int.TryParse(_configuration["Jwt:ExpirationHours"], out var h) ? h : 8;

            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Nombre),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "admin"),
            };

            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: creds
            );

            var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .WriteToken(token);

            return Ok(new
            {
                success = true,
                token = tokenString,
                nombre = user.Nombre,
                email = user.Email,
                usuarioId = user.UsuarioId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        var host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var user = _configuration["Smtp:User"];
        var pass = _configuration["Smtp:Pass"];
        var from = _configuration["Smtp:From"] ?? user;
        var fromName = _configuration["Smtp:FromName"] ?? "CUChatNet";

        if (string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(pass) ||
            string.IsNullOrWhiteSpace(from))
        {
            throw new Exception("Faltan credenciales SMTP.");
        }

        using var message = new MailMessage();
        message.From = new MailAddress(from, fromName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = bodyHtml;
        message.IsBodyHtml = true;

        using var smtp = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true,
            UseDefaultCredentials = false
        };

        await smtp.SendMailAsync(message);
    }
}