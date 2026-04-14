namespace CUChatNet.Api.Models;

public class ChatUsuario
{
    public long ChatUsuarioId { get; set; }

    public long ChatId { get; set; }
    public Chat Chat { get; set; }

    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
}