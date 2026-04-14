using CUChatNet.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiAdminController : ControllerBase
{
    private readonly CUChatNetDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AiAdminController(CUChatNetDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
        _httpClient = new HttpClient();
    }

    [HttpPost("admin-chat")]
    [AllowAnonymous]
    public async Task<IActionResult> AdminChat([FromBody] AdminChatRequest request)
    {
        var totalUsuarios = await _db.Usuarios.CountAsync(u => !u.Eliminado);
        var usuariosActivos = await _db.Usuarios.CountAsync(u => !u.Eliminado && u.Activo);
        var usuariosBloqueados = await _db.CuentasAcceso.CountAsync(u => u.EstadoCuenta == "suspended");
        var totalChats = await _db.Chats.CountAsync();
        var totalMensajes = await _db.Mensajes.CountAsync();
        var totalGrupos = await _db.Chats.CountAsync(c => c.TipoChat == "group");
        var usuariosHoy = await _db.Usuarios
            .CountAsync(u => !u.Eliminado && u.FechaCreacion.Date == DateTime.UtcNow.Date);
        var ultimosEventos = await _db.BitacoraEventos
            .OrderByDescending(e => e.FechaEvento)
            .Take(5)
            .Select(e => $"{e.Accion} - {e.Severidad} ({e.FechaEvento:dd/MM/yyyy HH:mm})")
            .ToListAsync();
        var roles = await _db.Roles
            .Select(r => $"{r.Nombre}: {r.UsuarioRoles.Count} usuarios")
            .ToListAsync();

        var contexto = $@"Eres un asistente inteligente del panel de administración de CUChatNet, una app de mensajería universitaria.
Responde de forma clara, concisa y en español. Usa los siguientes datos reales del sistema:
- Total usuarios registrados: {totalUsuarios}
- Usuarios activos: {usuariosActivos}
- Usuarios bloqueados: {usuariosBloqueados}
- Usuarios registrados hoy: {usuariosHoy}
- Total chats: {totalChats}
- Total grupos: {totalGrupos}
- Total mensajes enviados: {totalMensajes}
- Roles: {string.Join(", ", roles)}
- Últimos eventos: {string.Join(" | ", ultimosEventos)}
Responde solo con información relevante a la pregunta del administrador.";

        var apiKey = _configuration["Groq:ApiKey"];
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var body = new
        {
            model = "llama-3.1-8b-instant",
            max_tokens = 500,
            messages = new[]
            {
                new { role = "system", content = contexto },
                new { role = "user", content = request.Message }
            }
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode(500, new { error = result });

        var parsed = JsonSerializer.Deserialize<JsonElement>(result);
        var text = parsed.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return Ok(new { reply = text });
    }
}

public record AdminChatRequest(string Message);