using System.Net;
using System.Net.Mail;
using CUChatNet.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Servicios;

public interface ICorreoRecuperacionAdminServicio
{
    Task EnviarCodigoAsync(string correoDestino, string nombreUsuario, string codigo);
}

public class CorreoRecuperacionAdminServicio : ICorreoRecuperacionAdminServicio
{
    private readonly IConfiguration _configuration;
    private readonly CUChatNetDbContext _db;

    public CorreoRecuperacionAdminServicio(IConfiguration configuration, CUChatNetDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    public async Task EnviarCodigoAsync(string correoDestino, string nombreUsuario, string codigo)
    {
        var host = _configuration["Smtp:Host"];
        var puertoTexto = _configuration["Smtp:Port"];
        var usuario = _configuration["Smtp:User"];
        var clave = _configuration["Smtp:Password"] ?? _configuration["Smtp:Pass"];
        var remitente = _configuration["Smtp:From"] ?? usuario;
        var nombreRemitente = _configuration["Smtp:FromName"] ?? "CUChatNet";
        var sslTexto = _configuration["Smtp:EnableSsl"] ?? "true";

        var configSistema = await _db.ConfiguracionSistema
            .AsNoTracking()
            .OrderByDescending(x => x.ConfiguracionId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(host) && configSistema is not null)
        {
            host = configSistema.ServidorSmtp;
            puertoTexto = configSistema.PuertoSmtp.ToString();
            usuario = configSistema.UsuarioSmtp;
            clave = System.Text.Encoding.UTF8.GetString(configSistema.ClaveSmtpEncriptada);
            remitente ??= usuario;
        }

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(usuario) ||
            string.IsNullOrWhiteSpace(clave) ||
            string.IsNullOrWhiteSpace(remitente))
        {
            throw new Exception("No hay configuración SMTP válida.");
        }

        var puerto = int.TryParse(puertoTexto, out var puertoFinal) ? puertoFinal : 587;
        var usarSsl = bool.TryParse(sslTexto, out var sslFinal) ? sslFinal : true;

        using var cliente = new SmtpClient(host, puerto)
        {
            Credentials = new NetworkCredential(usuario, clave),
            EnableSsl = usarSsl
        };

        var asunto = "Recuperación de contraseña - Administrador";
        var cuerpo = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <h2>Recuperación de contraseña</h2>
                <p>Hola {WebUtility.HtmlEncode(nombreUsuario)}.</p>
                <p>Tu código de verificación es:</p>
                <div style='font-size: 28px; font-weight: bold; letter-spacing: 6px; margin: 16px 0;'>
                    {WebUtility.HtmlEncode(codigo)}
                </div>
                <p>Este código vence en 15 minutos.</p>
                <p>Si no solicitaste este cambio, ignora este correo.</p>
            </div>";

        using var mensaje = new MailMessage
        {
            From = new MailAddress(remitente, nombreRemitente),
            Subject = asunto,
            Body = cuerpo,
            IsBodyHtml = true
        };

        mensaje.To.Add(correoDestino);

        await cliente.SendMailAsync(mensaje);
    }
}