using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using CUChatNet.Api.Services; // Asegúrate de que el namespace coincida
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

    // ... (mismo encabezado que ya tienes)

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
        await _db.SaveChangesAsync();

        // Notificar a los miembros del grupo por SignalR
        var messageData = new
        {
            id = message.MensajeId,
            chatId = message.ChatId,
            senderId = message.RemitenteUsuarioId,
            senderName = sender.Nombre,
            content = request.Content, // Enviamos texto plano para el tiempo real
            timestamp = message.FechaEnvio,
            type = message.TipoMensaje
        };

        // El secreto de los grupos es que el Hub use el chatId como nombre de la sala (Group)
        await _hubContext.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", messageData);

        return Ok(messageData);
    }

    [HttpDelete("messages/{messageId:long}")]
    public async Task<IActionResult> DeleteMessage(long messageId)
    {
        var message = await _db.Mensajes.FindAsync(messageId);
        if (message == null) return NotFound();

        message.EliminadoParaTodos = true;
        message.Contenido = "Este mensaje fue eliminado"; // No necesita cifrado al estar eliminado
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

        // Notificamos al grupo que alguien leyó los mensajes. 
        // Esto hace que al otro usuario se le pongan los checks en azul.
        await _hubContext.Clients.Group(chatId.ToString()).SendAsync("ChatReadByPeer", userId);

        return Ok(new { success = true, count = unreadStates.Count });
    }
}