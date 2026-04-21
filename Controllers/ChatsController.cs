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
        // ✅ OPTIMIZACIÓN CRÍTICA: Proyección directa SIN Include
        var chats = await _db.ChatParticipantes
            .AsNoTracking()
            .Where(cp => cp.UsuarioId == userId && cp.Activo)
            .Select(cp => new
            {
                ChatId = cp.ChatId,
                TipoChat = cp.Chat.TipoChat,
                NombreGrupo = cp.Chat.Nombre,
                FotoGrupo = cp.Chat.FotoUrl,
                DescripcionGrupo = cp.Chat.Descripcion,
                FechaCreacion = cp.Chat.FechaCreacion,
                
                // ✅ Solo traer el otro participante para chats individuales
                OtroParticipante = cp.Chat.TipoChat == "individual"
                    ? cp.Chat.Participantes
                        .Where(p => p.UsuarioId != userId && p.Activo)
                        .Select(p => new
                        {
                            p.UsuarioId,
                            p.Usuario.Nombre,
                            p.Usuario.FotoUrl,
                            p.Usuario.Descripcion
                        })
                        .FirstOrDefault()
                    : null,
                
                // ✅ Solo el último mensaje (no todos)
                UltimoMensaje = cp.Chat.Mensajes
                    .Where(m => !m.EliminadoParaTodos)
                    .OrderByDescending(m => m.FechaEnvio)
                    .Select(m => new
                    {
                        m.Contenido,
                        m.FechaEnvio
                    })
                    .FirstOrDefault(),
                
                // ✅ Contar no leídos directamente en BD
                MensajesNoLeidos = cp.Chat.Mensajes.Count(m =>
                    !m.EliminadoParaTodos &&
                    m.Estados.Any(e =>
                        e.UsuarioId == userId &&
                        e.Estado == "received"
                    )
                )
            })
            .ToListAsync();

        if (!chats.Any())
            return Ok(new List<ChatListItemDto>());

        // ✅ Mapear en memoria (descifrado si es necesario)
        var result = chats.Select(c =>
        {
            bool isGroup = c.TipoChat == "group";

            return new ChatListItemDto
            {
                Id = c.ChatId,
                ParticipantId = isGroup ? 0 : (c.OtroParticipante?.UsuarioId ?? 0),
                ParticipantName = isGroup
                    ? (c.NombreGrupo ?? "Grupo")
                    : (c.OtroParticipante?.Nombre ?? "Usuario"),
                ParticipantPhoto = isGroup
                    ? c.FotoGrupo
                    : c.OtroParticipante?.FotoUrl,
                ParticipantDescription = isGroup
                    ? c.DescripcionGrupo
                    : c.OtroParticipante?.Descripcion,
                ParticipantStatus = isGroup ? null : "Online",
                LastMessage = c.UltimoMensaje?.Contenido ?? "Sin mensajes",
                LastMessageTime = c.UltimoMensaje?.FechaEnvio ?? c.FechaCreacion,
                Unread = c.MensajesNoLeidos,
                Pinned = false,
                Archived = false,
                IsGroup = isGroup,
                Silenced = false
            };
        })
        .OrderByDescending(x => x.LastMessageTime)
        .ToList();

        return Ok(result);
    }

    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirectChat([FromBody] CreateDirectChatRequest request)
    {
        // ✅ OPTIMIZACIÓN: Query simplificada
        var existingChatId = await _db.ChatParticipantes
            .AsNoTracking()
            .Where(cp => cp.UsuarioId == request.CurrentUserId && cp.Activo)
            .Where(cp => cp.Chat.TipoChat == "individual")
            .Where(cp => cp.Chat.Participantes.Any(p =>
                p.UsuarioId == request.OtherUserId && p.Activo))
            .Select(cp => cp.ChatId)
            .FirstOrDefaultAsync();

        if (existingChatId > 0)
        {
            return Ok(new { chatId = existingChatId, isNew = false });
        }

        // ✅ Crear chat nuevo
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
                    CodigoConversacion = $"IND-{Guid.NewGuid():N}"[..12].ToUpper()
                };

                _db.Chats.Add(newChat);
                await _db.SaveChangesAsync();

                var participantes = new[]
                {
                    new ChatParticipante
                    {
                        ChatId = newChat.ChatId,
                        UsuarioId = request.CurrentUserId,
                        Rol = "member",
                        Activo = true,
                        FechaUnion = DateTime.UtcNow
                    },
                    new ChatParticipante
                    {
                        ChatId = newChat.ChatId,
                        UsuarioId = request.OtherUserId,
                        Rol = "member",
                        Activo = true,
                        FechaUnion = DateTime.UtcNow
                    }
                };

                _db.ChatParticipantes.AddRange(participantes);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { chatId = newChat.ChatId, isNew = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Error al crear el chat", detalle = ex.Message });
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
                    CodigoConversacion = $"GRP-{Guid.NewGuid():N}"[..12].ToUpper(),
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
                if (!memberIds.Contains(request.CurrentUserId))
                    memberIds.Add(request.CurrentUserId);

                var now = DateTime.UtcNow;
                var participantes = memberIds.Select(id => new ChatParticipante
                {
                    ChatId = chat.ChatId,
                    UsuarioId = id,
                    Rol = (id == request.CurrentUserId) ? "admin" : "member",
                    Activo = true,
                    FechaUnion = now
                }).ToList();

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
            .Include(c => c.Participantes.Where(p => p.Activo))
            .FirstOrDefaultAsync(c => c.ChatId == chatId && c.TipoChat == "group");

        if (chat == null)
            return NotFound(new { error = "El grupo no existe" });

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // ✅ Actualizar info del grupo
                chat.Nombre = !string.IsNullOrWhiteSpace(request.GroupName)
                    ? request.GroupName.Trim()
                    : chat.Nombre;
                chat.Descripcion = request.GroupDescription ?? chat.Descripcion;
                chat.FotoUrl = request.GroupPhoto ?? chat.FotoUrl;
                chat.Reglas = request.GroupRules ?? chat.Reglas;
                chat.PermisoEnviarMensajes = request.OnlyAdminsCanPost ? "admins" : "all";
                chat.PermisoEditarInfo = request.OnlyAdminsCanEdit ? "admins" : "all";

                var currentMemberIds = chat.Participantes.Select(p => p.UsuarioId).ToHashSet();
                var incomingMemberIds = (request.MemberIds ?? new List<long>()).ToHashSet();

                // ✅ Agregar nuevos miembros
                var newMemberIds = incomingMemberIds.Except(currentMemberIds).ToList();
                if (newMemberIds.Any())
                {
                    var now = DateTime.UtcNow;
                    var newParticipants = newMemberIds.Select(id => new ChatParticipante
                    {
                        ChatId = chatId,
                        UsuarioId = id,
                        Rol = "member",
                        Activo = true,
                        FechaUnion = now
                    }).ToList();

                    _db.ChatParticipantes.AddRange(newParticipants);
                }

                // ✅ Remover miembros
                var removedMemberIds = currentMemberIds.Except(incomingMemberIds).ToList();
                if (removedMemberIds.Any())
                {
                    var participantsToRemove = chat.Participantes
                        .Where(p => removedMemberIds.Contains(p.UsuarioId) &&
                                   p.UsuarioId != chat.CreadoPorUsuarioId)
                        .ToList();

                    foreach (var p in participantsToRemove)
                        p.Activo = false;
                }

                // ✅ Actualizar roles
                var adminIds = (request.AdminIds ?? new List<long>()).ToHashSet();
                foreach (var participant in chat.Participantes)
                {
                    if (participant.UsuarioId == chat.CreadoPorUsuarioId)
                        continue; // El creador siempre es admin

                    participant.Rol = adminIds.Contains(participant.UsuarioId)
                        ? "admin"
                        : "member";
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
                var participant = await _db.ChatParticipantes
                    .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UsuarioId == userId);

                if (participant == null)
                    return NotFound("No eres participante de este chat");

                if (clearHistory)
                {
                    // ✅ OPTIMIZACIÓN: Delete en cascada más eficiente
                    await _db.MensajeEstados
                        .Where(me => me.Mensaje.ChatId == chatId)
                        .ExecuteDeleteAsync();

                    await _db.Mensajes
                        .Where(m => m.ChatId == chatId)
                        .ExecuteDeleteAsync();
                }

                participant.Activo = false;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ Fire-and-forget SignalR
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hubContext.Clients
                            .User(userId.ToString())
                            .SendAsync("ChatDeleted", chatId);
                    }
                    catch { }
                });

                return Ok(new { message = "Chat e historial eliminados correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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
        // ✅ OPTIMIZACIÓN CRÍTICA: Proyección directa
        var groups = await _db.ChatParticipantes
            .AsNoTracking()
            .Where(cp =>
                cp.UsuarioId == userId &&
                cp.Activo &&
                cp.Chat.TipoChat == "group")
            .Select(cp => new
            {
                Id = cp.ChatId,
                Nombre = cp.Chat.Nombre ?? "Grupo",
                FotoUrl = cp.Chat.FotoUrl ?? "",
                Descripcion = cp.Chat.Descripcion ?? "",
                Reglas = cp.Chat.Reglas ?? "",
                PermisoEnviarMensajes = cp.Chat.PermisoEnviarMensajes ?? "all",
                PermisoEditarInfo = cp.Chat.PermisoEditarInfo ?? "admins",
                CreadoPorUsuarioId = cp.Chat.CreadoPorUsuarioId ?? 0,
                FechaCreacion = cp.Chat.FechaCreacion,
                
                // ✅ Solo IDs y nombres de participantes
                Participantes = cp.Chat.Participantes
                    .Where(p => p.Activo)
                    .Select(p => new
                    {
                        IdUsuario = p.UsuarioId,
                        Nombre = p.Usuario.Nombre ?? "Usuario",
                        Rol = p.Rol ?? "member"
                    })
                    .ToList()
            })
            .ToListAsync();

        var result = groups.Select(g => new
        {
            g.Id,
            nombre = g.Nombre,
            fotoUrl = g.FotoUrl,
            descripcion = g.Descripcion,
            reglas = g.Reglas,
            permisoEnviarMensajes = g.PermisoEnviarMensajes,
            permisoEditarInfo = g.PermisoEditarInfo,
            creadoPorUsuarioId = g.CreadoPorUsuarioId,
            fechaCreacion = g.FechaCreacion,
            isGroup = true,
            participantes = g.Participantes
        }).ToList();

        return Ok(result);
    }
}
