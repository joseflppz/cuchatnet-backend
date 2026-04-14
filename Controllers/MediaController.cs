using Microsoft.AspNetCore.Mvc;

namespace CUChatNet.Api.Controllers
{
    [ApiController]
    [Route("api/media")] // Cambiamos esto para que sea más claro
    public class MediaController : ControllerBase
    {
        [HttpPost("upload/{chatId}")] // La ruta final es: api/media/upload/123
        public async Task<IActionResult> UploadMedia(long chatId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest("Archivo vacío");

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // IMPORTANTE: Devolvemos un objeto con la propiedad "url"
                var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}