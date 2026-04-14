using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CUChatNet.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEsGrupoToChats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitacoraEventos",
                columns: table => new
                {
                    EventoID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    DispositivoId = table.Column<long>(type: "bigint", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detalles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Severidad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DireccionIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaEvento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitacoraEventos", x => x.EventoID);
                });

            migrationBuilder.CreateTable(
                name: "Chats",
                columns: table => new
                {
                    ChatID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsGrupo = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CodigoConversacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoChat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadoPorUsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.ChatID);
                });

            migrationBuilder.CreateTable(
                name: "ClavesDispositivo",
                columns: table => new
                {
                    ClaveDispositivoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispositivoId = table.Column<long>(type: "bigint", nullable: false),
                    ClavePublica = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionClave = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimaRotacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClavesDispositivo", x => x.ClaveDispositivoId);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionSistema",
                columns: table => new
                {
                    ConfiguracionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreAplicacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxGrupo = table.Column<int>(type: "int", nullable: false),
                    TimeoutMensajesMinutos = table.Column<int>(type: "int", nullable: false),
                    MaxArchivoMB = table.Column<int>(type: "int", nullable: false),
                    ServidorSmtp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PuertoSmtp = table.Column<int>(type: "int", nullable: true),
                    UsuarioSmtp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaveSmtpEncriptada = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ProveedorSms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiKeySmsEncriptada = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiTokenEncriptado = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ModoMantenimiento = table.Column<bool>(type: "bit", nullable: false),
                    E2ERequerido = table.Column<bool>(type: "bit", nullable: false),
                    AutoArchivarInactividadDias = table.Column<int>(type: "int", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionSistema", x => x.ConfiguracionId);
                });

            migrationBuilder.CreateTable(
                name: "ContactosSilenciadosEstado",
                columns: table => new
                {
                    ContactoSilenciadoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    ContactoUsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    FechaSilenciado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactosSilenciadosEstado", x => x.ContactoSilenciadoId);
                });

            migrationBuilder.CreateTable(
                name: "DispositivosUsuario",
                columns: table => new
                {
                    DispositivoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    NombreDispositivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Plataforma = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SistemaOperativo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Navegador = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HuellaDispositivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoSeguridad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UltimaIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Verificado = table.Column<bool>(type: "bit", nullable: false),
                    Confiable = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimoUso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispositivosUsuario", x => x.DispositivoId);
                });

            migrationBuilder.CreateTable(
                name: "PoliticasSeguridad",
                columns: table => new
                {
                    PoliticaSeguridadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BloquearTrasIntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    DuracionBloqueoMinutos = table.Column<int>(type: "int", nullable: false),
                    RotacionClavesDias = table.Column<int>(type: "int", nullable: false),
                    DeteccionCambioDispositivo = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionIdentidad = table.Column<bool>(type: "bit", nullable: false),
                    RequiereE2E = table.Column<bool>(type: "bit", nullable: false),
                    TimeoutLoginSospechosoMin = table.Column<int>(type: "int", nullable: false),
                    MaxDispositivosPorUsuario = table.Column<int>(type: "int", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasSeguridad", x => x.PoliticaSeguridadId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RolID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RolID);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsuarioID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExtensionPais = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumeroTelefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelefonoCompleto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstadoPerfil = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Verificado = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsuarioID);
                });

            migrationBuilder.CreateTable(
                name: "VerificacionesSms",
                columns: table => new
                {
                    VerificacionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    DispositivoId = table.Column<long>(type: "bigint", nullable: true),
                    ExtensionPais = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumeroTelefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProveedorSms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Intentos = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVerificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificacionesSms", x => x.VerificacionId);
                });

            migrationBuilder.CreateTable(
                name: "ChatParticipantes",
                columns: table => new
                {
                    ChatParticipanteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fijado = table.Column<bool>(type: "bit", nullable: false),
                    Archivado = table.Column<bool>(type: "bit", nullable: false),
                    Silenciado = table.Column<bool>(type: "bit", nullable: false),
                    FechaUnion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaSalida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUltimaLectura = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUltimaLimpieza = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipantes", x => x.ChatParticipanteId);
                    table.ForeignKey(
                        name: "FK_ChatParticipantes_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "ChatID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatParticipantes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatUsuarios",
                columns: table => new
                {
                    ChatUsuarioId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatUsuarios", x => x.ChatUsuarioId);
                    table.ForeignKey(
                        name: "FK_ChatUsuarios_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "ChatID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatUsuarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactosUsuario",
                columns: table => new
                {
                    ContactoRelacionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    ContactoUsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SincronizadoAgenda = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaAgregado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactosUsuario", x => x.ContactoRelacionId);
                    table.ForeignKey(
                        name: "FK_ContactosUsuario_Usuarios_ContactoUsuarioId",
                        column: x => x.ContactoUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactosUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuentasAcceso",
                columns: table => new
                {
                    CuentaAccesoID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    NombreUsuario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HashContrasena = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsaOtp = table.Column<bool>(type: "bit", nullable: false),
                    DebeCambiarContrasena = table.Column<bool>(type: "bit", nullable: false),
                    EstadoCuenta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    BloqueadoHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasAcceso", x => x.CuentaAccesoID);
                    table.ForeignKey(
                        name: "FK_CuentasAcceso_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstadosUsuario",
                columns: table => new
                {
                    EstadoUsuarioId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EliminadoManual = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosUsuario", x => x.EstadoUsuarioId);
                    table.ForeignKey(
                        name: "FK_EstadosUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mensajes",
                columns: table => new
                {
                    MensajeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    RemitenteUsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    DispositivoId = table.Column<long>(type: "bigint", nullable: true),
                    EsRespuestaA = table.Column<long>(type: "bigint", nullable: true),
                    TipoMensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Encriptado = table.Column<bool>(type: "bit", nullable: false),
                    Editado = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoParaTodos = table.Column<bool>(type: "bit", nullable: false),
                    EstadoServidor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEdicion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.MensajeId);
                    table.ForeignKey(
                        name: "FK_Mensajes_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "ChatID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mensajes_Usuarios_RemitenteUsuarioId",
                        column: x => x.RemitenteUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    UsuarioRolId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => x.UsuarioRolId);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "RolID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstadoVisualizaciones",
                columns: table => new
                {
                    EstadoVisualizacionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstadoUsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    FechaVisualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoVisualizaciones", x => x.EstadoVisualizacionId);
                    table.ForeignKey(
                        name: "FK_EstadoVisualizaciones_EstadosUsuario_EstadoUsuarioId",
                        column: x => x.EstadoUsuarioId,
                        principalTable: "EstadosUsuario",
                        principalColumn: "EstadoUsuarioId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstadoVisualizaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MensajeAdjuntos",
                columns: table => new
                {
                    AdjuntoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MensajeId = table.Column<long>(type: "bigint", nullable: false),
                    TipoAdjunto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UrlArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: true),
                    DuracionSegundos = table.Column<int>(type: "int", nullable: true),
                    Ancho = table.Column<int>(type: "int", nullable: true),
                    Alto = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensajeAdjuntos", x => x.AdjuntoId);
                    table.ForeignKey(
                        name: "FK_MensajeAdjuntos_Mensajes_MensajeId",
                        column: x => x.MensajeId,
                        principalTable: "Mensajes",
                        principalColumn: "MensajeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MensajeEstados",
                columns: table => new
                {
                    MensajeEstadoId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MensajeId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaVista = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoParaMi = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminadoParaMi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensajeEstados", x => x.MensajeEstadoId);
                    table.ForeignKey(
                        name: "FK_MensajeEstados_Mensajes_MensajeId",
                        column: x => x.MensajeId,
                        principalTable: "Mensajes",
                        principalColumn: "MensajeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MensajeEstados_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipantes_ChatId",
                table: "ChatParticipantes",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipantes_UsuarioId",
                table: "ChatParticipantes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsuarios_ChatId",
                table: "ChatUsuarios",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsuarios_UsuarioId",
                table: "ChatUsuarios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactosUsuario_ContactoUsuarioId",
                table: "ContactosUsuario",
                column: "ContactoUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactosUsuario_UsuarioId",
                table: "ContactosUsuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasAcceso_UsuarioId",
                table: "CuentasAcceso",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstadosUsuario_UsuarioId",
                table: "EstadosUsuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoVisualizaciones_EstadoUsuarioId",
                table: "EstadoVisualizaciones",
                column: "EstadoUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoVisualizaciones_UsuarioId",
                table: "EstadoVisualizaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MensajeAdjuntos_MensajeId",
                table: "MensajeAdjuntos",
                column: "MensajeId");

            migrationBuilder.CreateIndex(
                name: "IX_MensajeEstados_MensajeId",
                table: "MensajeEstados",
                column: "MensajeId");

            migrationBuilder.CreateIndex(
                name: "IX_MensajeEstados_UsuarioId",
                table: "MensajeEstados",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_ChatId",
                table: "Mensajes",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_RemitenteUsuarioId",
                table: "Mensajes",
                column: "RemitenteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RolId",
                table: "UsuarioRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_UsuarioId",
                table: "UsuarioRoles",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitacoraEventos");

            migrationBuilder.DropTable(
                name: "ChatParticipantes");

            migrationBuilder.DropTable(
                name: "ChatUsuarios");

            migrationBuilder.DropTable(
                name: "ClavesDispositivo");

            migrationBuilder.DropTable(
                name: "ConfiguracionSistema");

            migrationBuilder.DropTable(
                name: "ContactosSilenciadosEstado");

            migrationBuilder.DropTable(
                name: "ContactosUsuario");

            migrationBuilder.DropTable(
                name: "CuentasAcceso");

            migrationBuilder.DropTable(
                name: "DispositivosUsuario");

            migrationBuilder.DropTable(
                name: "EstadoVisualizaciones");

            migrationBuilder.DropTable(
                name: "MensajeAdjuntos");

            migrationBuilder.DropTable(
                name: "MensajeEstados");

            migrationBuilder.DropTable(
                name: "PoliticasSeguridad");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "VerificacionesSms");

            migrationBuilder.DropTable(
                name: "EstadosUsuario");

            migrationBuilder.DropTable(
                name: "Mensajes");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Chats");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
