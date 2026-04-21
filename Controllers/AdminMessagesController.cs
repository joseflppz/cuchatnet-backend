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

    [HttpDelete("{messageId:long}")]
    public async Task<IActionResult> DeleteMessage(long messageId)
    {
        var message = await _db.Mensajes.FindAsync(messageId);
        
        if (message is null)
            return NotFound();

        // Eliminar estados del mensaje
        var estados = await _db.MensajeEstados
            .Where(e => e.MensajeId == messageId)
            .ToListAsync();
        if (estados.Any())
            _db.MensajeEstados.RemoveRange(estados);

        // Eliminar adjuntos del mensaje
        var adjuntos = await _db.MensajeAdjuntos
            .Where(a => a.MensajeId == messageId)
            .ToListAsync();
        if (adjuntos.Any())
            _db.MensajeAdjuntos.RemoveRange(adjuntos);

        // Eliminar mensaje
        _db.Mensajes.Remove(message);
        
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
