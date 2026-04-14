using CUChatNet.Api.Data;
using CUChatNet.Api.Dtos;
using CUChatNet.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Controllers;

[ApiController]
[Route("api")]
public class StatesController : ControllerBase
{
    private readonly CUChatNetDbContext _db;

    public StatesController(CUChatNetDbContext db)
    {
        _db = db;
    }

    [HttpGet("users/{userId:long}/states-feed")]
    public async Task<ActionResult<IEnumerable<StateDto>>> GetFeed(long userId)
    {
        var contactIds = await _db.ContactosUsuario
            .Where(x => x.UsuarioId == userId && x.Activo)
            .Select(x => x.ContactoUsuarioId)
            .ToListAsync();

        var mutedIds = await _db.ContactosSilenciadosEstado
            .Where(x => x.UsuarioId == userId && x.Activo)
            .Select(x => x.ContactoUsuarioId)
            .ToListAsync();

        var allowedIds = contactIds.Except(mutedIds).Append(userId).Distinct().ToList();

        var states = await _db.EstadosUsuario
            .Where(x => x.Activo
                     && !x.EliminadoManual
                     && x.FechaExpiracion > DateTime.UtcNow
                     && allowedIds.Contains(x.UsuarioId))
            .Include(x => x.Usuario)
            .Include(x => x.Visualizaciones)
            .OrderByDescending(x => x.FechaCreacion)
            .ToListAsync();

        var result = states.Select(s => new StateDto(
            s.EstadoUsuarioId,
            s.UsuarioId,
            s.Usuario.Nombre,
            s.Usuario.FotoUrl,
            s.TipoContenido == "image" && !string.IsNullOrWhiteSpace(s.MediaUrl) ? s.MediaUrl! : s.Contenido,
            s.TipoContenido,
            s.FechaCreacion,
            s.FechaExpiracion,
            s.Visualizaciones.Select(v => v.UsuarioId).ToList()
        ));

        return Ok(result);
    }

    [HttpPost("users/{userId:long}/states")]
    public async Task<IActionResult> CreateState(long userId, [FromBody] CreateStateRequest request)
    {
        if (userId != request.UserId)
            return BadRequest(new { error = "El userId de la ruta no coincide con el cuerpo." });

        var state = new EstadoUsuario
        {
            UsuarioId = request.UserId,
            TipoContenido = request.Type,
            Contenido = request.Type == "image" && !string.IsNullOrWhiteSpace(request.MediaUrl) ? request.MediaUrl! : request.Content,
            MediaUrl = request.MediaUrl,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddHours(24),
            Activo = true,
            EliminadoManual = false
        };

        _db.EstadosUsuario.Add(state);
        await _db.SaveChangesAsync();

        return Ok(new { id = state.EstadoUsuarioId, success = true });
    }

    [HttpPost("states/{stateId:long}/view")]
    public async Task<IActionResult> ViewState(long stateId, [FromBody] ViewStateRequest request)
    {
        var exists = await _db.EstadoVisualizaciones.AnyAsync(x => x.EstadoUsuarioId == stateId && x.UsuarioId == request.UserId);
        if (!exists)
        {
            _db.EstadoVisualizaciones.Add(new EstadoVisualizacion
            {
                EstadoUsuarioId = stateId,
                UsuarioId = request.UserId,
                FechaVisualizacion = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }

    [HttpGet("states/{stateId:long}/viewers")]
    public async Task<IActionResult> GetViewers(long stateId)
    {
        var viewers = await _db.EstadoVisualizaciones
            .Where(x => x.EstadoUsuarioId == stateId)
            .Include(x => x.Usuario)
            .OrderByDescending(x => x.FechaVisualizacion)
            .Select(x => new
            {
                id = x.Usuario.UsuarioId,
                name = x.Usuario.Nombre,
                photo = x.Usuario.FotoUrl,
                status = x.Usuario.EstadoPerfil,
                viewedAt = x.FechaVisualizacion
            })
            .ToListAsync();

        return Ok(viewers);
    }
}
