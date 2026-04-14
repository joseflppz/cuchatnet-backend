namespace CUChatNet.Api.Dtos;

public record EnviarCodigoRecuperacionAdminRequest(
    string Correo
);

public record VerificarCodigoRecuperacionAdminRequest(
    string Correo,
    string Codigo
);

public record CambiarContrasenaAdminRequest(
    string TokenRecuperacion,
    string NuevaContrasena,
    string ConfirmarContrasena
);

public record RespuestaSimpleDto(
    string Mensaje
);

public record VerificarCodigoRecuperacionAdminResponse(
    string Mensaje,
    string TokenRecuperacion
);