using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly CUChatNetDbContext _db;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatsController(CUChatNetDbContext db, IHubContext<ChatHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    [HttpGet("user/{userId:long}")]
    public async Task<ActionResult<IEnumerable<ChatListItemDto>>> GetChats(long userId)
    {
        // 1. Obtener las membresías del usuario (Sin Includes profundos para evitar ciclos)
        var memberships = await _db.ChatParticipantes
            .AsNoTracking()
            .Where(cp => cp.UsuarioId == userId && cp.Activo)
            .Include(cp => cp.Chat)
            .ToListAsync();

        var chatIds = memberships.Select(m => m.ChatId).ToList();

        // 2. Cargar todos los participantes de esos chats en una lista plana 
        // Esto evita el error "Cycles are not allowed in no-tracking queries"
        var allParticipants = await _db.ChatParticipantes
            .AsNoTracking()
            .Where(p => chatIds.Contains(p.ChatId) && p.Activo)
            .Include(p => p.Usuario)
            .ToListAsync();

        // 3. Obtener el último mensaje de cada chat
        var lastMessages = await _db.Mensajes
            .AsNoTracking()
            .Where(m => chatIds.Contains(m.ChatId))
            .GroupBy(m => m.ChatId)
            .Select(g => g.OrderByDescending(m => m.FechaEnvio).FirstOrDefault())
            .ToListAsync();

        var lastMessageMap = lastMessages
            .Where(m => m != null)
            .ToDictionary(m => m!.ChatId, m => m);

        // 4. Proyectar al DTO combinando los datos en memoria
        var result = memberships.Select(cp =>
        {
            var chat = cp.Chat;
            lastMessageMap.TryGetValue(chat.ChatId, out var lastMessage);

            bool isGroup = chat.TipoChat == "group";

            // Buscamos al otro participante en la lista plana que cargamos en el paso 2
            var otherParticipant = !isGroup
                ? allParticipants.FirstOrDefault(p => p.ChatId == chat.ChatId && p.UsuarioId != userId)?.Usuario
                : null;

            return new ChatListItemDto
            {
                Id = chat.ChatId,
                ParticipantId = isGroup ? 0 : (otherParticipant?.UsuarioId ?? 0),
                ParticipantName = isGroup ? (chat.Nombre ?? "Grupo") : (otherParticipant?.Nombre ?? "Usuario"),
                ParticipantPhoto = isGroup ? chat.FotoUrl : otherParticipant?.FotoUrl,
                ParticipantDescription = isGroup ? chat.Descripcion : otherParticipant?.Descripcion,
                ParticipantStatus = isGroup ? null : "Online",
                LastMessage = lastMessage?.Contenido ?? "Sin mensajes",
                LastMessageTime = lastMessage?.FechaEnvio,
                Unread = 0,
                Pinned = false,
                Archived = false,
                IsGroup = isGroup,
                Silenced = false
            };
        })
        .OrderByDescending(x => x.LastMessageTime ?? DateTime.MinValue)
        .ToList();

        return Ok(result);
    }

    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirectChat([FromBody] CreateDirectChatRequest request)
    {
        var existingChat = await _db.Chats
            .Where(c => c.TipoChat == "individual")
            .Where(c => c.Participantes.Any(p => p.UsuarioId == request.CurrentUserId) &&
                        c.Participantes.Any(p => p.UsuarioId == request.OtherUserId))
            .FirstOrDefaultAsync();

        if (existingChat != null)
        {
            return Ok(new { chatId = existingChat.ChatId, isNew = false });
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var newChat = new Chat
                {
                    TipoChat = "individual",
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true,
                    CodigoConversacion = $"IND-{Guid.NewGuid().ToString()[..8].ToUpper()}"
                };

                _db.Chats.Add(newChat);
                await _db.SaveChangesAsync();

                var participantes = new List<ChatParticipante>
                {
                    new ChatParticipante { ChatId = newChat.ChatId, UsuarioId = request.CurrentUserId, Rol = "member", Activo = true, FechaUnion = DateTime.UtcNow },
                    new ChatParticipante { ChatId = newChat.ChatId, UsuarioId = request.OtherUserId, Rol = "member", Activo = true, FechaUnion = DateTime.UtcNow }
                };

                _db.ChatParticipantes.AddRange(participantes);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { chatId = newChat.ChatId, isNew = true });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error al crear el chat");
            }
        });
    }

    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GroupName))
            return BadRequest(new { error = "El nombre del grupo es obligatorio" });

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var chat = new Chat
                {
                    TipoChat = "group",
                    CodigoConversacion = $"GRP-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    Nombre = request.GroupName.Trim(),
                    FotoUrl = request.GroupPhoto ?? "",
                    Descripcion = request.GroupDescription ?? "",
                    CreadoPorUsuarioId = request.CurrentUserId,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();

                var memberIds = request.MemberIds ?? new List<long>();
                if (!memberIds.Contains(request.CurrentUserId)) memberIds.Add(request.CurrentUserId);

                var participantes = memberIds.Select(id => new ChatParticipante
                {
                    ChatId = chat.ChatId,
                    UsuarioId = id,
                    Rol = (id == request.CurrentUserId) ? "admin" : "member",
                    Activo = true,
                    FechaUnion = DateTime.UtcNow
                });

                _db.ChatParticipantes.AddRange(participantes);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { chatId = chat.ChatId, name = chat.Nombre });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Error en BD", detalle = ex.Message });
            }
        });
    }

    [HttpPut("{chatId:long}/group")]
    public async Task<IActionResult> UpdateGroup(long chatId, [FromBody] FullUpdateGroupRequest request)
    {
        var chat = await _db.Chats
            .Include(c => c.Participantes)
            .FirstOrDefaultAsync(c => c.ChatId == chatId && c.TipoChat == "group");

        if (chat == null) return NotFound(new { error = "El grupo no existe" });

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                chat.Nombre = !string.IsNullOrWhiteSpace(request.GroupName) ? request.GroupName.Trim() : chat.Nombre;
                chat.Descripcion = request.GroupDescription ?? chat.Descripcion;
                chat.FotoUrl = request.GroupPhoto ?? chat.FotoUrl;
                chat.Reglas = request.GroupRules ?? chat.Reglas;
                chat.PermisoEnviarMensajes = request.OnlyAdminsCanPost ? "admins" : "all";
                chat.PermisoEditarInfo = request.OnlyAdminsCanEdit ? "admins" : "all";

                var currentMemberIds = chat.Participantes.Where(p => p.Activo).Select(p => p.UsuarioId).ToList();
                var incomingMemberIds = request.MemberIds ?? new List<long>();

                var newMemberIds = incomingMemberIds.Where(id => !currentMemberIds.Contains(id)).ToList();
                foreach (var newId in newMemberIds)
                {
                    _db.ChatParticipantes.Add(new ChatParticipante
                    {
                        ChatId = chatId,
                        UsuarioId = newId,
                        Rol = "member",
                        Activo = true,
                        FechaUnion = DateTime.UtcNow
                    });
                }

                var incomingAdminIds = request.AdminIds ?? new List<long>();
                foreach (var participant in chat.Participantes)
                {
                    if (incomingAdminIds.Contains(participant.UsuarioId))
                        participant.Rol = "admin";
                    else if (participant.UsuarioId != chat.CreadoPorUsuarioId)
                        participant.Rol = "member";
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "¡Grupo actualizado!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Error al actualizar", detalle = ex.Message });
            }
        });
    }

    [HttpDelete("{chatId:long}")]
    public async Task<IActionResult> DeleteChat(long chatId, [FromQuery] long userId, [FromQuery] bool clearHistory)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Verificar participante
                var participant = await _db.ChatParticipantes
                    .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UsuarioId == userId);

                if (participant == null) return NotFound("No eres participante de este chat");

                if (clearHistory)
                {
                    // --- EL ORDEN AQUÍ ES CRÍTICO ---

                    // A. Obtener IDs de los mensajes de este chat
                    var messageIds = await _db.Mensajes
                        .Where(m => m.ChatId == chatId)
                        .Select(m => m.MensajeId)
                        .ToListAsync();

                    if (messageIds.Any())
                    {
                        // B. Borrar primero los ESTADOS de esos mensajes (La restricción FK_MensajeEstados_Mensajes)
                        var states = _db.MensajeEstados.Where(me => messageIds.Contains(me.MensajeId));
                        _db.MensajeEstados.RemoveRange(states);
                        await _db.SaveChangesAsync(); // Guardamos para liberar la FK

                        // C. Ahora sí podemos borrar los MENSAJES
                        var messages = _db.Mensajes.Where(m => m.ChatId == chatId);
                        _db.Mensajes.RemoveRange(messages);
                        await _db.SaveChangesAsync();
                    }
                }

                // 2. Borrado lógico del participante
                participant.Activo = false;
                _db.Entry(participant).State = EntityState.Modified;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // 3. Notificar vía SignalR
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ChatDeleted", chatId);

                return Ok(new { message = "Chat e historial eliminados correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Esto te devolverá el error real si algo más falla
                return StatusCode(500, new
                {
                    error = "Error de integridad referencial",
                    detalle = ex.InnerException?.Message ?? ex.Message
                });
            }
        });
    }
    [HttpGet("my-groups/{userId:long}")]
    public async Task<IActionResult> GetMyGroups(long userId)
    {
        var memberships = await _db.ChatParticipantes
            .AsNoTracking()
            .Where(cp => cp.UsuarioId == userId && cp.Activo && cp.Chat.TipoChat == "group")
            .Include(cp => cp.Chat)
            .ToListAsync();

        var chatIds = memberships.Select(m => m.ChatId).ToList();

        var allParticipants = await _db.ChatParticipantes
            .AsNoTracking()
            .Where(p => chatIds.Contains(p.ChatId) && p.Activo)
            .Include(p => p.Usuario)
            .ToListAsync();

        var result = memberships.Select(cp =>
        {
            var chat = cp.Chat;
            var participants = allParticipants.Where(p => p.ChatId == chat.ChatId).ToList();

            return new
            {
                id = chat.ChatId,
                nombre = chat.Nombre ?? "Grupo",
                fotoUrl = chat.FotoUrl ?? "",
                descripcion = chat.Descripcion ?? "",
                reglas = chat.Reglas ?? "",
                permisoEnviarMensajes = chat.PermisoEnviarMensajes ?? "all",
                permisoEditarInfo = chat.PermisoEditarInfo ?? "admins",
                creadoPorUsuarioId = chat.CreadoPorUsuarioId ?? 0,
                fechaCreacion = chat.FechaCreacion,
                isGroup = true,
                participantes = participants.Select(p => new
                {
                    idUsuario = p.UsuarioId,
                    nombre = p.Usuario?.Nombre ?? "Usuario",
                    rol = p.Rol ?? "member"
                }).ToList()
            };
        }).ToList();

        return Ok(result);
    }

}