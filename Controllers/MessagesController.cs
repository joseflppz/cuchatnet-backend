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
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(
        long chatId, 
        [FromQuery] long? userId = null,
        [FromQuery] int limit = 100) // ✅ Limitar mensajes
    {
        // ✅ OPTIMIZACIÓN CRÍTICA: Proyección directa sin Include
        var messages = await _db.Mensajes
            .AsNoTracking() // ✅ No rastrear cambios
            .AsSplitQuery() // ✅ Evitar cartesian explosion
            .Where(m => m.ChatId == chatId && !m.EliminadoParaTodos)
            .OrderByDescending(m => m.FechaEnvio) // ✅ Más recientes primero
            .Take(limit) // ✅ LIMITAR cantidad
            .Select(m => new
            {
                m.MensajeId,
                m.ChatId,
                m.RemitenteUsuarioId,
                RemitenteNombre = m.RemitenteUsuario.Nombre,
                m.Contenido,
                m.FechaEnvio,
                m.EstadoServidor,
                m.Encriptado,
                m.Editado,
                m.EliminadoParaTodos,
                m.TipoMensaje,
                // ✅ Solo el primer adjunto (no todos)
                ArchivoUrl = m.Adjuntos.FirstOrDefault() != null 
                    ? m.Adjuntos.First().UrlArchivo 
                    : null,
                // ✅ Solo el estado del usuario actual
                UserState = userId.HasValue
                    ? m.Estados.FirstOrDefault(e => e.UsuarioId == userId.Value)
                    : null
            })
            .ToListAsync();

        // ✅ Descifrar en memoria (no en BD) y ordenar cronológicamente
        var result = messages
            .Select(m =>
            {
                string contenidoAMostrar = m.Contenido ?? "";
                if (m.Encriptado && m.TipoMensaje == "text")
                {
                    try
                    {
                        contenidoAMostrar = _encryptionService.Decrypt(contenidoAMostrar);
                    }
                    catch
                    {
                        contenidoAMostrar = "[Error al descifrar]";
                    }
                }

                return new MessageDto(
                    m.MensajeId,
                    m.ChatId,
                    m.RemitenteUsuarioId,
                    m.RemitenteNombre,
                    contenidoAMostrar,
                    m.FechaEnvio,
                    m.UserState?.Estado ?? m.EstadoServidor,
                    m.Encriptado,
                    m.Editado,
                    m.UserState?.EliminadoParaMi ?? false,
                    m.EliminadoParaTodos,
                    m.TipoMensaje,
                    m.ArchivoUrl
                );
            })
            .Reverse() // ✅ Cronológicamente para la UI
            .ToList();

        return Ok(result);
    }

    [HttpPost("chats/{chatId:long}/messages")]
    public async Task<IActionResult> SendMessage(long chatId, [FromBody] SendMessageRequest request)
    {
        // ✅ OPTIMIZACIÓN: AsNoTracking + Select solo lo necesario
        var chat = await _db.Chats
            .AsNoTracking()
            .Where(c => c.ChatId == chatId && c.Activo)
            .Select(c => new
            {
                c.ChatId,
                ParticipantIds = c.Participantes
                    .Where(p => p.Activo && p.UsuarioId != request.SenderId)
                    .Select(p => p.UsuarioId)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (chat is null) 
            return NotFound(new { error = "Chat no encontrado." });

        // ✅ OPTIMIZACIÓN: Solo verificar existencia (AnyAsync)
        var senderExists = await _db.Usuarios
            .AsNoTracking()
            .AnyAsync(u => u.UsuarioId == request.SenderId && !u.Eliminado);
            
        if (!senderExists) 
            return BadRequest(new { error = "Remitente no válido." });

        // ✅ Obtener nombre para SignalR (query separada)
        var senderName = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.UsuarioId == request.SenderId)
            .Select(u => u.Nombre)
            .FirstOrDefaultAsync();

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
        await _db.SaveChangesAsync(); // ✅ Guardar mensaje primero

        // ✅ Crear estados en BATCH (más eficiente)
        if (chat.ParticipantIds.Any())
        {
            var estados = chat.ParticipantIds.Select(participantId => new MensajeEstado
            {
                MensajeId = message.MensajeId,
                UsuarioId = participantId,
                Estado = "received",
                FechaEntrega = DateTime.UtcNow
            }).ToList();

            _db.MensajeEstados.AddRange(estados); // ✅ AddRange es más rápido
            await _db.SaveChangesAsync();
        }

        // ✅ Preparar datos para SignalR
        var messageData = new
        {
            id = message.MensajeId,
            chatId = message.ChatId,
            senderId = message.RemitenteUsuarioId,
            senderName = senderName ?? "Usuario",
            content = request.Content,
            timestamp = message.FechaEnvio,
            type = message.TipoMensaje,
            status = "sent",
            encrypted = true
        };

        // ✅ Fire-and-forget para SignalR (no esperar)
        _ = Task.Run(async () =>
        {
            try
            {
                await _hubContext.Clients
                    .Group(chatId.ToString())
                    .SendAsync("ReceiveMessage", messageData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error SignalR: {ex.Message}");
            }
        });

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

        // ✅ Fire-and-forget SignalR
        _ = Task.Run(async () =>
        {
            try
            {
                await _hubContext.Clients
                    .Group(message.ChatId.ToString())
                    .SendAsync("MessageDeleted", messageId);
            }
            catch { }
        });

        return Ok(new { success = true });
    }

    [HttpPatch("messages/{messageId:long}/status")]
    public async Task<IActionResult> UpdateStatus(long messageId, [FromBody] UpdateMessageStatusRequest request)
    {
        var state = await _db.MensajeEstados
            .FirstOrDefaultAsync(x => x.MensajeId == messageId && x.UsuarioId == request.UserId);

        if (state is null) return NotFound();

        state.Estado = request.Status;
        if (request.Status == "received" && state.FechaEntrega is null) 
            state.FechaEntrega = DateTime.UtcNow;
        if (request.Status == "seen") 
            state.FechaVista = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPatch("chats/{chatId:long}/read")]
    public async Task<IActionResult> MarkChatAsRead(long chatId, [FromQuery] long userId)
    {
        // ✅ OPTIMIZACIÓN: Filtrar en BD, no en memoria
        var unreadStates = await _db.MensajeEstados
            .Where(e => 
                e.Mensaje.ChatId == chatId && 
                e.UsuarioId == userId && 
                e.Estado != "seen")
            .ToListAsync();

        if (!unreadStates.Any()) 
            return Ok(new { message = "Sin mensajes pendientes" });

        var now = DateTime.UtcNow;
        foreach (var state in unreadStates)
        {
            state.Estado = "seen";
            state.FechaVista = now;
        }

        await _db.SaveChangesAsync();

        // ✅ Fire-and-forget SignalR
        _ = Task.Run(async () =>
        {
            try
            {
                await _hubContext.Clients
                    .Group(chatId.ToString())
                    .SendAsync("ChatReadByPeer", userId);
            }
            catch { }
        });

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
                    Estado = "received",
                    FechaEntrega = DateTime.UtcNow
                };
                _db.MensajeEstados.Add(estado);
            }
            else if (estado.Estado == "sent")
            {
                estado.Estado = "received";
                estado.FechaEntrega = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // ✅ OPTIMIZACIÓN: Solo traer RemitenteUsuarioId
            var remitenteId = await _db.Mensajes
                .AsNoTracking()
                .Where(m => m.MensajeId == messageId)
                .Select(m => m.RemitenteUsuarioId)
                .FirstOrDefaultAsync();

            if (remitenteId > 0)
            {
                // ✅ Fire-and-forget SignalR
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients.User(remitenteId.ToString())
                            .SendAsync("MessageDelivered", new
                            {
                                messageId,
                                userId = request.UserId,
                                timestamp = DateTime.UtcNow
                            });
                    }
                    catch { }
                });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("messages/{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(long messageId, [FromBody] MarkMessageRequest request)
    {
        try
        {
            var estado = await _db.MensajeEstados
                .FirstOrDefaultAsync(e => e.MensajeId == messageId && e.UsuarioId == request.UserId);
            
            var now = DateTime.UtcNow;
            
            if (estado == null)
            {
                estado = new MensajeEstado
                {
                    MensajeId = messageId,
                    UsuarioId = request.UserId,
                    Estado = "seen",
                    FechaEntrega = now,
                    FechaVista = now
                };
                _db.MensajeEstados.Add(estado);
            }
            else
            {
                estado.Estado = "seen";
                estado.FechaVista = now;
                estado.FechaEntrega ??= now;
            }

            await _db.SaveChangesAsync();

            // ✅ OPTIMIZACIÓN: Solo traer RemitenteUsuarioId
            var remitenteId = await _db.Mensajes
                .AsNoTracking()
                .Where(m => m.MensajeId == messageId)
                .Select(m => m.RemitenteUsuarioId)
                .FirstOrDefaultAsync();

            if (remitenteId > 0)
            {
                // ✅ Fire-and-forget SignalR
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients.User(remitenteId.ToString())
                            .SendAsync("MessageRead", new
                            {
                                messageId,
                                userId = request.UserId,
                                timestamp = now
                            });
                    }
                    catch { }
                });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class MarkMessageRequest
{
    public long UserId { get; set; }
}
