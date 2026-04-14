using Microsoft.AspNetCore.Mvc;

namespace CUChatNet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public UploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("{chatId}")]
        public async Task<IActionResult> UploadFile(string chatId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se seleccionó ningún archivo.");

            // Crear la ruta de la carpeta Uploads
            string uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Nombre único para evitar sobrescribir archivos
            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Devolvemos la URL relativa para que el frontend la use
            var fileUrl = $"/uploads/{fileName}";

            return Ok(new { url = fileUrl });
        }
    }
}