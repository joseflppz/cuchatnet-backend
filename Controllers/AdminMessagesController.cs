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
            .Include(m => m.RemitenteUsuario)
            .Include(m => m.Chat)
            .ThenInclude(c => c.Participantes)
            .ThenInclude(p => p.Usuario)
            .Include(m => m.Estados)
            .OrderByDescending(m => m.FechaEnvio)
            .Take(200)
            .ToListAsync();

        var result = messages.Select(m =>
        {
            string destination;
            if (m.Chat.TipoChat == "group")
            {
                destination = m.Chat.Nombre ?? "Grupo";
            }
            else
            {
                destination = m.Chat.Participantes
                    .FirstOrDefault(p => p.UsuarioId != m.RemitenteUsuarioId)?.Usuario?.Nombre ?? "Contacto";
            }

            var firstState = m.Estados.OrderByDescending(e => e.FechaVista ?? e.FechaEntrega).FirstOrDefault();
            return new AdminMessageDto(
                m.MensajeId,
                m.RemitenteUsuario.Nombre,
                m.Chat.TipoChat,
                destination,
                m.Chat.CodigoConversacion,
                m.FechaEnvio,
                firstState?.Estado == "seen" ? "Visto" : "Entregado",
                m.Encriptado,
                null,
                m.IpOrigen,
                firstState?.FechaEntrega,
                firstState?.FechaVista
            );
        });

        return Ok(result);
    }
}
