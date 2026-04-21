using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using CUChatNet.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api")]
public class MessagesController : ControllerBase
{
    private readonly CUChatNetDbContext _db;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly EncryptionService _encryptionService;

    public MessagesController(CUChatNetDbContext db, IHubContext<ChatHub> hubContext, EncryptionService encryptionService)
    {
        _db = db;
        _hubContext = hubContext;
        _encryptionService = encryptionService;
    }

    [HttpGet("chats/{chatId:long}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(long chatId, [FromQuery] long? userId = null)
    {
        var messages = await _db.Mensajes
            .AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .Include(m => m.RemitenteUsuario)
            .Include(m => m.Adjuntos)
            .Include(m => m.Estados)
            .OrderBy(m => m.FechaEnvio)
            .ToListAsync();

        var result = messages.Select(m =>
        {
            var userState = userId.HasValue
                ? m.Estados.FirstOrDefault(e => e.UsuarioId == userId.Value)
                : null;

            // DESCIFRADO: Si el mensaje está marcado como encriptado y no está eliminado
            string contenidoAMostrar = m.Contenido ?? "";
            if (m.Encriptado && !m.EliminadoParaTodos && m.TipoMensaje == "text")
            {
                contenidoAMostrar = _encryptionService.Decrypt(contenidoAMostrar);
            }

            return new MessageDto(
                m.MensajeId,
                m.ChatId,
                m.RemitenteUsuarioId,
                m.RemitenteUsuario.Nombre,
                contenidoAMostrar,
                m.FechaEnvio,
                userState?.Estado ?? (m.EstadoServidor == "sent" ? "sent" : m.EstadoServidor),
                m.Encriptado,
                m.Editado,
                userState?.EliminadoParaMi ?? false,
                m.EliminadoParaTodos,
                m.TipoMensaje,
                m.Adjuntos.FirstOrDefault()?.UrlArchivo
            );
        });

        return Ok(result);
    }

    [HttpPost("chats/{chatId:long}/messages")]
    public async Task<IActionResult> SendMessage(long chatId, [FromBody] SendMessageRequest request)
    {
        // Buscamos el chat e incluimos participantes para saber a quién notificar
        var chat = await _db.Chats
            .Include(c => c.Participantes)
            .FirstOrDefaultAsync(c => c.ChatId == chatId && c.Activo);

        if (chat is null) return NotFound(new { error = "Chat no encontrado." });

        var sender = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == request.SenderId && !u.Eliminado);
        if (sender is null) return BadRequest(new { error = "Remitente no válido." });

        string contenidoFinal = request.Content;
        if (request.Type == "text")
        {
            contenidoFinal = _encryptionService.Encrypt(request.Content);
        }

        var message = new Mensaje
        {
            ChatId = chatId,
            RemitenteUsuarioId = request.SenderId,
            TipoMensaje = request.Type,
            Contenido = contenidoFinal,
            Encriptado = true,
            FechaEnvio = DateTime.UtcNow,
            EstadoServidor = "sent"
        };

        _db.Mensajes.Add(message);
        
        // Crear estados para todos los participantes (excepto el remitente)
        var participantIds = chat.Participantes
            .Where(p => p.Activo && p.UsuarioId != request.SenderId)
            .Select(p => p.UsuarioId)
            .ToList();

        foreach (var participantId in participantIds)
        {
            _db.MensajeEstados.Add(new MensajeEstado
            {
                MensajeId = message.MensajeId,
                UsuarioId = participantId,
                Estado = "received"  // ✅ CAMBIO AQUÍ
            });
        }
        
        await _db.SaveChangesAsync();

        // Preparar datos del mensaje para SignalR
        var messageData = new
        {
            id = message.MensajeId,
            chatId = message.ChatId,
            senderId = message.RemitenteUsuarioId,
            senderName = sender.Nombre,
            content = request.Content, // Enviamos texto plano para el tiempo real
            timestamp = message.FechaEnvio,
            type = message.TipoMensaje,
            status = "sent",
            encrypted = true
        };

        // Notificar a TODOS los participantes del chat (incluyendo el remitente)
        await _hubContext.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", messageData);

        return Ok(messageData);
    }

    [HttpDelete("messages/{messageId:long}")]
    public async Task<IActionResult> DeleteMessage(long messageId)
    {
        var message = await _db.Mensajes.FindAsync(messageId);
        if (message == null) return NotFound();

        message.EliminadoParaTodos = true;
        message.Contenido = "Este mensaje fue eliminado";
        message.Encriptado = false;

        await _db.SaveChangesAsync();
        await _hubContext.Clients.Group(message.ChatId.ToString()).SendAsync("MessageDeleted", messageId);

        return Ok(new { success = true });
    }

    [HttpPatch("messages/{messageId:long}/status")]
    public async Task<IActionResult> UpdateStatus(long messageId, [FromBody] UpdateMessageStatusRequest request)
    {
        var state = await _db.MensajeEstados
            .FirstOrDefaultAsync(x => x.MensajeId == messageId && x.UsuarioId == request.UserId);

        if (state is null) return NotFound();

        state.Estado = request.Status;
        if (request.Status == "received" && state.FechaEntrega is null) state.FechaEntrega = DateTime.UtcNow;
        if (request.Status == "seen") state.FechaVista = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPatch("chats/{chatId:long}/read")]
    public async Task<IActionResult> MarkChatAsRead(long chatId, [FromQuery] long userId)
    {
        var unreadStates = await _db.MensajeEstados
            .Include(e => e.Mensaje)
            .Where(e => e.Mensaje.ChatId == chatId && e.UsuarioId == userId && e.Estado != "seen")
            .ToListAsync();

        if (!unreadStates.Any()) return Ok(new { message = "Sin mensajes pendientes" });

        foreach (var state in unreadStates)
        {
            state.Estado = "seen";
            state.FechaVista = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _hubContext.Clients.Group(chatId.ToString()).SendAsync("ChatReadByPeer", userId);

        return Ok(new { success = true, count = unreadStates.Count });
    }

    [HttpPost("messages/{messageId}/delivered")]
    public async Task<IActionResult> MarkAsDelivered(long messageId, [FromBody] MarkMessageRequest request)
    {
        try
        {
            var estado = await _db.MensajeEstados
                .FirstOrDefaultAsync(e => e.MensajeId == messageId && e.UsuarioId == request.UserId);
            
            if (estado == null)
            {
                estado = new MensajeEstado
                {
                    MensajeId = messageId,
                    UsuarioId = request.UserId,
                    Estado = "delivered",
                    FechaEntrega = DateTime.UtcNow
                };
                _db.MensajeEstados.Add(estado);
            }
            else if (estado.Estado == "sent")
            {
                estado.Estado = "delivered";
                estado.FechaEntrega = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            var mensaje = await _db.Mensajes
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MensajeId == messageId);

            if (mensaje != null)
            {
                await _hubContext.Clients.User(mensaje.RemitenteUsuarioId.ToString())
                    .SendAsync("MessageDelivered", new
                    {
                        messageId,
                        userId = request.UserId,
                        timestamp = DateTime.UtcNow
                    });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
        }
    }

    [HttpPost("messages/{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(long messageId, [FromBody] MarkMessageRequest request)
    {
        try
        {
            // Buscar el estado existente DIRECTAMENTE en DbSet (evita problemas de tracking)
            var estado = await _db.MensajeEstados
                .FirstOrDefaultAsync(e => e.MensajeId == messageId && e.UsuarioId == request.UserId);
            
            if (estado == null)
            {
                // Solo crear si NO existe
                estado = new MensajeEstado
                {
                    MensajeId = messageId,
                    UsuarioId = request.UserId,
                    Estado = "seen",  // ✅ "seen" funciona con el CHECK constraint
                    FechaEntrega = DateTime.UtcNow,
                    FechaVista = DateTime.UtcNow
                };
                _db.MensajeEstados.Add(estado);
            }
            else
            {
                // Si existe, solo actualizar
                estado.Estado = "seen";  // ✅ "seen" funciona con el CHECK constraint
                estado.FechaVista = DateTime.UtcNow;
                if (estado.FechaEntrega == null)
                    estado.FechaEntrega = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Buscar el mensaje para notificar al remitente
            var mensaje = await _db.Mensajes
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MensajeId == messageId);

            if (mensaje != null)
            {
                await _hubContext.Clients.User(mensaje.RemitenteUsuarioId.ToString())
                    .SendAsync("MessageRead", new
                    {
                        messageId,
                        userId = request.UserId,
                        timestamp = DateTime.UtcNow
                    });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
        }
    }
}

public class MarkMessageRequest
{
    public long UserId { get; set; }
}
