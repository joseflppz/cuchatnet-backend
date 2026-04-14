using CUChatNet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CUChatNet.Api.Data;

public class CUChatNetDbContext : DbContext
{
    public CUChatNetDbContext(DbContextOptions<CUChatNetDbContext> options) : base(options) { }
    public DbSet<CodigoRecuperacionAdmin> CodigosRecuperacionAdmin { get; set; }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<CuentaAcceso> CuentasAcceso => Set<CuentaAcceso>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<DispositivoUsuario> DispositivosUsuario => Set<DispositivoUsuario>();
    public DbSet<ClaveDispositivo> ClavesDispositivo => Set<ClaveDispositivo>();
    public DbSet<VerificacionSms> VerificacionesSms => Set<VerificacionSms>();
    public DbSet<ContactoUsuario> ContactosUsuario => Set<ContactoUsuario>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatParticipante> ChatParticipantes => Set<ChatParticipante>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<MensajeAdjunto> MensajeAdjuntos => Set<MensajeAdjunto>();
    public DbSet<MensajeEstado> MensajeEstados => Set<MensajeEstado>();
    public DbSet<EstadoUsuario> EstadosUsuario => Set<EstadoUsuario>();
    public DbSet<EstadoVisualizacion> EstadoVisualizaciones => Set<EstadoVisualizacion>();
    public DbSet<ContactoSilenciadoEstado> ContactosSilenciadosEstado => Set<ContactoSilenciadoEstado>();
    public DbSet<ConfiguracionSistema> ConfiguracionSistema => Set<ConfiguracionSistema>();
    public DbSet<PoliticaSeguridad> PoliticasSeguridad => Set<PoliticaSeguridad>();
    public DbSet<BitacoraEvento> BitacoraEventos => Set<BitacoraEvento>();
    public DbSet<CodigoVerificacion> CodigosVerificacion => Set<CodigoVerificacion>();
    public DbSet<PreferenciasUsuario> PreferenciasUsuario => Set<PreferenciasUsuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.RolId);
            entity.Property(x => x.RolId).HasColumnName("RolID");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(x => x.UsuarioId);
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.TelefonoCompleto)
                .ValueGeneratedOnAddOrUpdate()
                .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        });

        modelBuilder.Entity<CuentaAcceso>(entity =>
        {
            entity.ToTable("CuentasAcceso");
            entity.HasKey(x => x.CuentaAccesoId);
            entity.Property(x => x.CuentaAccesoId).HasColumnName("CuentaAccesoID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.HasOne(x => x.Usuario)
                .WithOne(x => x.CuentaAcceso)
                .HasForeignKey<CuentaAcceso>(x => x.UsuarioId);
        });

        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.ToTable("UsuarioRoles");
            entity.HasKey(x => x.UsuarioRolId);
            entity.Property(x => x.UsuarioRolId).HasColumnName("UsuarioRolID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.RolId).HasColumnName("RolID");
            entity.HasOne(x => x.Usuario).WithMany(x => x.UsuarioRoles).HasForeignKey(x => x.UsuarioId);
            entity.HasOne(x => x.Rol).WithMany(x => x.UsuarioRoles).HasForeignKey(x => x.RolId);
        });

        modelBuilder.Entity<DispositivoUsuario>(entity =>
        {
            entity.ToTable("DispositivosUsuario");
            entity.HasKey(x => x.DispositivoId);
            entity.Property(x => x.DispositivoId).HasColumnName("DispositivoID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.UltimaIp).HasColumnName("UltimaIP");
        });

        modelBuilder.Entity<ClaveDispositivo>(entity =>
        {
            entity.ToTable("ClavesDispositivo");
            entity.HasKey(x => x.ClaveDispositivoId);
            entity.Property(x => x.ClaveDispositivoId).HasColumnName("ClaveDispositivoID");
            entity.Property(x => x.DispositivoId).HasColumnName("DispositivoID");
        });

        modelBuilder.Entity<VerificacionSms>(entity =>
        {
            entity.ToTable("VerificacionesSMS");
            entity.HasKey(x => x.VerificacionId);
            entity.Property(x => x.VerificacionId).HasColumnName("VerificacionID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.DispositivoId).HasColumnName("DispositivoID");
            entity.Property(x => x.ProveedorSms).HasColumnName("ProveedorSMS");
        });

        modelBuilder.Entity<ContactoUsuario>(entity =>
        {
            entity.ToTable("ContactosUsuario");
            entity.HasKey(x => x.ContactoRelacionId);
            entity.Property(x => x.ContactoRelacionId).HasColumnName("ContactoRelacionID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.ContactoUsuarioId).HasColumnName("ContactoUsuarioID");
            entity.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Contacto)
                .WithMany()
                .HasForeignKey(x => x.ContactoUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.ToTable("Chats");
            entity.HasKey(x => x.ChatId);
            entity.Property(x => x.ChatId).HasColumnName("ChatID");
            entity.Property(x => x.CodigoConversacion).HasColumnName("CodigoConversacion");
            entity.Property(x => x.TipoChat).HasColumnName("TipoChat");
            entity.Property(x => x.FotoUrl).HasColumnName("FotoUrl");
            entity.Property(x => x.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioID");
        });

        modelBuilder.Entity<ChatParticipante>(entity =>
        {
            entity.ToTable("ChatParticipantes");
            entity.HasKey(x => x.ChatParticipanteId);
            entity.Property(x => x.ChatParticipanteId).HasColumnName("ChatParticipanteID");
            entity.Property(x => x.ChatId).HasColumnName("ChatID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.HasOne(x => x.Chat).WithMany(x => x.Participantes).HasForeignKey(x => x.ChatId);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<Mensaje>(entity =>
        {
            entity.ToTable("Mensajes");
            entity.HasKey(x => x.MensajeId);
            entity.Property(x => x.MensajeId).HasColumnName("MensajeID");
            entity.Property(x => x.ChatId).HasColumnName("ChatID");
            entity.Property(x => x.RemitenteUsuarioId).HasColumnName("RemitenteUsuarioID");
            entity.Property(x => x.DispositivoId).HasColumnName("DispositivoID");
            entity.Property(x => x.EsRespuestaA).HasColumnName("EsRespuestaA");
            entity.Property(x => x.TipoMensaje).HasColumnName("TipoMensaje");
            entity.Property(x => x.IpOrigen).HasColumnName("IpOrigen");
            entity.HasOne(x => x.Chat).WithMany(x => x.Mensajes).HasForeignKey(x => x.ChatId);
            entity.HasOne(x => x.RemitenteUsuario).WithMany().HasForeignKey(x => x.RemitenteUsuarioId);
        });

        modelBuilder.Entity<MensajeAdjunto>(entity =>
        {
            entity.ToTable("MensajeAdjuntos");
            entity.HasKey(x => x.AdjuntoId);
            entity.Property(x => x.AdjuntoId).HasColumnName("AdjuntoID");
            entity.Property(x => x.MensajeId).HasColumnName("MensajeID");
            entity.Property(x => x.TipoAdjunto).HasColumnName("TipoAdjunto");
            entity.Property(x => x.UrlArchivo).HasColumnName("UrlArchivo");
            entity.Property(x => x.NombreArchivo).HasColumnName("NombreArchivo");
            entity.Property(x => x.MimeType).HasColumnName("MimeType");
            entity.Property(x => x.TamanoBytes).HasColumnName("TamanoBytes");
            entity.Property(x => x.DuracionSegundos).HasColumnName("DuracionSegundos");
            entity.Property(x => x.FechaCreacion).HasColumnName("FechaCreacion");
            entity.HasOne(x => x.Mensaje).WithMany(x => x.Adjuntos).HasForeignKey(x => x.MensajeId);
        });

        modelBuilder.Entity<MensajeEstado>(entity =>
        {
            entity.ToTable("MensajeEstados");
            entity.HasKey(x => x.MensajeEstadoId);
            entity.Property(x => x.MensajeEstadoId).HasColumnName("MensajeEstadoID");
            entity.Property(x => x.MensajeId).HasColumnName("MensajeID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.FechaEliminadoParaMi).HasColumnName("FechaEliminadoParaMi");
            entity.HasOne(x => x.Mensaje).WithMany(x => x.Estados).HasForeignKey(x => x.MensajeId);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<EstadoUsuario>(entity =>
        {
            entity.ToTable("EstadosUsuario");
            entity.HasKey(x => x.EstadoUsuarioId);
            entity.Property(x => x.EstadoUsuarioId).HasColumnName("EstadoUsuarioID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.TipoContenido).HasColumnName("TipoContenido");
            entity.Property(x => x.MediaUrl).HasColumnName("MediaUrl");
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<EstadoVisualizacion>(entity =>
        {
            entity.ToTable("EstadoVisualizaciones");
            entity.HasKey(x => x.EstadoVisualizacionId);
            entity.Property(x => x.EstadoVisualizacionId).HasColumnName("EstadoVisualizacionID");
            entity.Property(x => x.EstadoUsuarioId).HasColumnName("EstadoUsuarioID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.HasOne(x => x.EstadoUsuario).WithMany(x => x.Visualizaciones).HasForeignKey(x => x.EstadoUsuarioId);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<ContactoSilenciadoEstado>(entity =>
        {
            entity.ToTable("ContactosSilenciadosEstado");
            entity.HasKey(x => x.ContactoSilenciadoId);
            entity.Property(x => x.ContactoSilenciadoId).HasColumnName("ContactoSilenciadoID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.ContactoUsuarioId).HasColumnName("ContactoUsuarioID");
        });

        modelBuilder.Entity<ConfiguracionSistema>(entity =>
        {
            entity.ToTable("ConfiguracionSistema");
            entity.HasKey(x => x.ConfiguracionId);
            entity.Property(x => x.ConfiguracionId).HasColumnName("ConfiguracionID");
            entity.Property(x => x.ServidorSmtp).HasColumnName("ServidorSMTP");
            entity.Property(x => x.PuertoSmtp).HasColumnName("PuertoSMTP");
            entity.Property(x => x.UsuarioSmtp).HasColumnName("UsuarioSMTP");
            entity.Property(x => x.ClaveSmtpEncriptada).HasColumnName("ClaveSMTPEncriptada");
            entity.Property(x => x.ProveedorSms).HasColumnName("ProveedorSMS");
            entity.Property(x => x.ApiKeySmsEncriptada).HasColumnName("ApiKeySMSEncriptada");
            entity.Property(x => x.ApiEndpoint).HasColumnName("ApiEndpoint");
            entity.Property(x => x.ApiTokenEncriptado).HasColumnName("ApiTokenEncriptado");
        });

        modelBuilder.Entity<PoliticaSeguridad>(entity =>
        {
            entity.ToTable("PoliticasSeguridad");
            entity.HasKey(x => x.PoliticaSeguridadId);
            entity.Property(x => x.PoliticaSeguridadId).HasColumnName("PoliticaSeguridadID");
        });

        modelBuilder.Entity<BitacoraEvento>(entity =>
        {
            entity.ToTable("BitacoraEventos");
            entity.HasKey(x => x.EventoId);
            entity.Property(x => x.EventoId).HasColumnName("EventoID");
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(x => x.DispositivoId).HasColumnName("DispositivoID");
            entity.Property(x => x.DireccionIp).HasColumnName("DireccionIP");
            entity.Property(x => x.FechaEvento).HasColumnName("FechaEvento");
        });

        modelBuilder.Entity<CodigoVerificacion>(entity =>
        {
            entity.ToTable("CodigosVerificacion");
            entity.HasKey(x => x.CodigoVerificacionId);
        });

        modelBuilder.Entity<PreferenciasUsuario>(entity =>
        {
            entity.ToTable("PreferenciasUsuario");
            entity.HasKey(x => x.PreferenciaId);
            entity.Property(x => x.UsuarioId).HasColumnName("UsuarioID");
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });
    }
}
