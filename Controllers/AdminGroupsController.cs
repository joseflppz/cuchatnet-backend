using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api/admin/groups")]
public class AdminGroupsController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public AdminGroupsController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminGroupDto>>> GetGroups()
    {
        var groups = await _db.Chats
            .Where(c => c.TipoChat == "group" && c.Activo)
            .Include(c => c.Participantes)
            .ThenInclude(p => p.Usuario)
            .Include(c => c.Mensajes)
            .OrderByDescending(c => c.FechaCreacion)
            .ToListAsync();

        var result = groups.Select(g => new AdminGroupDto(
            g.ChatId,
            g.Nombre ?? "Grupo",
            g.Participantes.FirstOrDefault(p => p.UsuarioId == g.CreadoPorUsuarioId)?.Usuario?.Nombre ?? "N/D",
            g.Participantes.Count(p => p.Activo),
            g.FechaCreacion,
            g.Mensajes.Count,
            g.Descripcion
        ));

        return Ok(result);
    }

    [HttpDelete("{chatId:long}")]
    public async Task<IActionResult> DeleteGroup(long chatId)
    {
        var group = await _db.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId && c.TipoChat == "group");
        if (group is null)
            return NotFound();

        group.Activo = false;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
