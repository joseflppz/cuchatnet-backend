namespace CUChatNet.Api.Servicios
{
    public interface IContactosServicio
    {
        Task<bool> EditarContactoAsync(long userId, long contactoUsuarioId, string? alias, string? ip);
        Task<bool> EliminarContactoAsync(long userId, long contactoUsuarioId, string? ip);
    }
}