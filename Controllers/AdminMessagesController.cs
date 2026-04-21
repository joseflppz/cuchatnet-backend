using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/messages")]
public class AdminMessagesController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public AdminMessagesController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminMessageDto>>> GetMessages()
    {
        var messages = await _db.Mensajes
            .OrderByDescending(m => m.FechaEnvio)
            .Take(200)
            .Select(m => new
            {
                m.MensajeId,
                RemitenteNombre = m.RemitenteUsuario.Nombre,
                m.Chat.TipoChat,
                ChatNombre = m.Chat.Nombre,
                m.Chat.CodigoConversacion,
                m.FechaEnvio,
                m.Encriptado,
                m.IpOrigen,
                m.RemitenteUsuarioId,
                // Obtener el destinatario solo si es chat privado
                DestinatarioNombre = m.Chat.TipoChat == "private" 
                    ? m.Chat.Participantes
                        .Where(p => p.UsuarioId != m.RemitenteUsuarioId)
                        .Select(p => p.Usuario.Nombre)
                        .FirstOrDefault()
                    : null,
                // Obtener el primer estado
                PrimerEstado = m.Estados
                    .OrderByDescending(e => e.FechaVista ?? e.FechaEntrega)
                    .Select(e => new { e.Estado, e.FechaEntrega, e.FechaVista })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var result = messages.Select(m => new AdminMessageDto(
            m.MensajeId,
            m.RemitenteNombre,
            m.TipoChat ?? "private",
            m.TipoChat == "group" ? (m.ChatNombre ?? "Grupo") : (m.DestinatarioNombre ?? "Contacto"),
            m.CodigoConversacion ?? "",
            m.FechaEnvio,
            m.PrimerEstado?.Estado == "seen" ? "Visto" : "Entregado",
            m.Encriptado,
            null,
            m.IpOrigen,
            m.PrimerEstado?.FechaEntrega,
            m.PrimerEstado?.FechaVista
        ));

        return Ok(result);
    }
}
