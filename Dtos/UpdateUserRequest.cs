namespace CUChatNet.Api.Dtos
{
    public class UpdateUserRequest
    {
        public string? Name { get; set; }

        public string? Status { get; set; }

        // 🔥 ESTO ES LO QUE TE FALTA
        public string? Description { get; set; }
    }
}
