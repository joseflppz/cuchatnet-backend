namespace CUChatNet.Api.Dtos;

public class EditContactoRequest
{
    public long UserId { get; set; }
    public string? Alias { get; set; }
}