using CUChatNet.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly CUChatNetDbContext _db;
    private readonly IConfiguration _configuration;

    public AdminAuthController(CUChatNetDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("token-dev")]
    public async Task<IActionResult> GetDevToken([FromBody] DevTokenRequest request)
    {
        var usuarioRol = await _db.UsuarioRoles
            .Include(ur => ur.Rol)
            .Include(ur => ur.Usuario)
            .FirstOrDefaultAsync(ur =>
                ur.UsuarioId == request.UsuarioId &&
                ur.Rol.Codigo == "admin" &&
                ur.Usuario.Activo &&
                !ur.Usuario.Eliminado);

        if (usuarioRol is null)
            return Unauthorized(new { message = "Usuario no encontrado o no tiene rol admin." });

        var token = GenerarToken(usuarioRol.Usuario.UsuarioId, usuarioRol.Usuario.Nombre);

        return Ok(new
        {
            token,
            usuarioId = usuarioRol.Usuario.UsuarioId,
            nombre = usuarioRol.Usuario.Nombre,
            expira = DateTime.UtcNow.AddHours(
                int.Parse(_configuration["Jwt:ExpirationHours"] ?? "8"))
        });
    }

    private string GenerarToken(long usuarioId, string nombre)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Name, nombre),
            new Claim(ClaimTypes.Role, "admin")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(
                int.Parse(_configuration["Jwt:ExpirationHours"] ?? "8")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record DevTokenRequest(long UsuarioId);