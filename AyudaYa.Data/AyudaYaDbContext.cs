using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AyudaYa.Data;

public partial class AyudaYaDbContext : IdentityDbContext<UsuarioIdentity, RolIdentity, Guid>
{
    public AyudaYaDbContext(DbContextOptions<AyudaYaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Asignacion> Asignacion { get; set; }

    public virtual DbSet<Centro> Centro { get; set; }

    public virtual DbSet<CentroRecurso> CentroRecurso { get; set; }

    public virtual DbSet<Ciudad> Ciudad { get; set; }

    public virtual DbSet<EventoAdministrativo> EventoAdministrativo { get; set; }

    public virtual DbSet<HistorialSolicitud> HistorialSolicitud { get; set; }

    public virtual DbSet<Organizacion> Organizacion { get; set; }

    public virtual DbSet<OrganizacionUsuario> OrganizacionUsuario { get; set; }

    public virtual DbSet<Solicitud> Solicitud { get; set; }

    public virtual DbSet<SolicitudContacto> SolicitudContacto { get; set; }

    public virtual DbSet<SolicitudRecurso> SolicitudRecurso { get; set; }

    public virtual DbSet<TipoCentro> TipoCentro { get; set; }

    public virtual DbSet<TipoRecurso> TipoRecurso { get; set; }

    public virtual DbSet<UsuarioDatosPrivados> UsuarioDatosPrivados { get; set; }

    public virtual DbSet<VCentrosPublicos> VCentrosPublicos { get; set; }

    public virtual DbSet<VMetricasPersonasPorRecurso> VMetricasPersonasPorRecurso { get; set; }

    public virtual DbSet<VSolicitudesAbiertas> VSolicitudesAbiertas { get; set; }

    public virtual DbSet<Voluntario> Voluntario { get; set; }

    public virtual DbSet<Zona> Zona { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -----------------------------------------------------------------
        // Identity: columnas y defaults que no se pueden inferir solos.
        // Las tablas y relaciones base las configura IdentityDbContext.
        // -----------------------------------------------------------------
        modelBuilder.Entity<UsuarioIdentity>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.HasKey(e => e.Id).HasName("pk_aspnetusers");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserName).IsRequired();
            entity.Property(e => e.NormalizedUserName).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.NormalizedEmail).IsRequired();
            entity.Property(e => e.SecurityStamp)
                .IsRequired()
                .HasDefaultValueSql("(gen_random_uuid())::text");
            entity.Property(e => e.ConcurrencyStamp)
                .IsRequired()
                .HasDefaultValueSql("(gen_random_uuid())::text");
            entity.Property(e => e.NombreCompleto)
                .IsRequired()
                .HasComment("Nombre y apellido visibles en la aplicación.");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasComment("Baja lógica. No se eliminan usuarios.");
            entity.Property(e => e.CreadoAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<RolIdentity>(entity =>
        {
            entity.ToTable("AspNetRoles");
            entity.HasKey(e => e.Id).HasName("pk_aspnetroles");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.NormalizedName).IsRequired();
            entity.Property(e => e.ConcurrencyStamp)
                .IsRequired()
                .HasDefaultValueSql("(gen_random_uuid())::text");
        });
        modelBuilder.Entity<Asignacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_asignacion");

            entity.ToTable("asignacion", tb => tb.HasComment("Una asignación activa por solicitud_recurso; se conserva el histórico. Exactamente un voluntario o una organización."));

            entity.HasIndex(e => new { e.OrganizacionId, e.Estado }, "ix_asignacion_organizacion");

            entity.HasIndex(e => new { e.SolicitudRecursoId, e.Estado }, "ix_asignacion_recurso");

            entity.HasIndex(e => new { e.VoluntarioId, e.Estado }, "ix_asignacion_voluntario");

            entity.HasIndex(e => e.SolicitudRecursoId, "ux_asignacion_activa")
                .IsUnique()
                .HasFilter("(estado = ANY (ARRAY['asignada'::text, 'en_atencion'::text, 'completada'::text]))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.AsignadaAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("asignada_at");
            entity.Property(e => e.AsignadaPor).HasColumnName("asignada_por");
            entity.Property(e => e.CanceladaAt).HasColumnName("cancelada_at");
            entity.Property(e => e.CanceladaPor).HasColumnName("cancelada_por");
            entity.Property(e => e.CompletadaAt).HasColumnName("completada_at");
            entity.Property(e => e.CompletadaPor).HasColumnName("completada_por");
            entity.Property(e => e.ConfirmadaAt).HasColumnName("confirmada_at");
            entity.Property(e => e.ConfirmadaPor).HasColumnName("confirmada_por");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'asignada'::text")
                .HasColumnName("estado");
            entity.Property(e => e.IniciadaAt).HasColumnName("iniciada_at");
            entity.Property(e => e.IniciadaPor).HasColumnName("iniciada_por");
            entity.Property(e => e.LiberadaAt).HasColumnName("liberada_at");
            entity.Property(e => e.LiberadaPor).HasColumnName("liberada_por");
            entity.Property(e => e.MotivoCancelacion).HasColumnName("motivo_cancelacion");
            entity.Property(e => e.MotivoLiberacion).HasColumnName("motivo_liberacion");
            entity.Property(e => e.OrganizacionId).HasColumnName("organizacion_id");
            entity.Property(e => e.SolicitudRecursoId).HasColumnName("solicitud_recurso_id");
            entity.Property(e => e.VoluntarioId).HasColumnName("voluntario_id");

            entity.HasOne(d => d.AsignadaPorNavigation).WithMany(p => p.AsignacionAsignadaPorNavigation)
                .HasForeignKey(d => d.AsignadaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_asignada_por");

            entity.HasOne(d => d.CanceladaPorNavigation).WithMany(p => p.AsignacionCanceladaPorNavigation)
                .HasForeignKey(d => d.CanceladaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_cancelada_por");

            entity.HasOne(d => d.CompletadaPorNavigation).WithMany(p => p.AsignacionCompletadaPorNavigation)
                .HasForeignKey(d => d.CompletadaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_completada_por");

            entity.HasOne(d => d.ConfirmadaPorNavigation).WithMany(p => p.AsignacionConfirmadaPorNavigation)
                .HasForeignKey(d => d.ConfirmadaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_confirmada_por");

            entity.HasOne(d => d.IniciadaPorNavigation).WithMany(p => p.AsignacionIniciadaPorNavigation)
                .HasForeignKey(d => d.IniciadaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_iniciada_por");

            entity.HasOne(d => d.LiberadaPorNavigation).WithMany(p => p.AsignacionLiberadaPorNavigation)
                .HasForeignKey(d => d.LiberadaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_liberada_por");

            entity.HasOne(d => d.Organizacion).WithMany(p => p.Asignacion)
                .HasForeignKey(d => d.OrganizacionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_organizacion");

            entity.HasOne(d => d.SolicitudRecurso).WithMany(p => p.Asignaciones)
                .HasForeignKey(d => d.SolicitudRecursoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_recurso");

            entity.HasOne(d => d.Voluntario).WithMany(p => p.Asignacion)
                .HasForeignKey(d => d.VoluntarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_asignacion_voluntario");
        });

        modelBuilder.Entity<Centro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_centro");

            entity.ToTable("centro");

            entity.HasIndex(e => new { e.CiudadId, e.ZonaId, e.EstadoVerificacion }, "ix_centro_ciudad_zona");

            entity.HasIndex(e => new { e.OrganizacionId, e.EstadoVerificacion }, "ix_centro_organizacion");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.AprobadoAt).HasColumnName("aprobado_at");
            entity.Property(e => e.AprobadoPor).HasColumnName("aprobado_por");
            entity.Property(e => e.CiudadId).HasColumnName("ciudad_id");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Direccion).HasColumnName("direccion");
            entity.Property(e => e.EmailContacto).HasColumnName("email_contacto");
            entity.Property(e => e.EstadoOperativo)
                .HasDefaultValueSql("'operativo'::text")
                .HasColumnName("estado_operativo");
            entity.Property(e => e.EstadoVerificacion)
                .HasDefaultValueSql("'pendiente'::text")
                .HasColumnName("estado_verificacion");
            entity.Property(e => e.Latitud).HasColumnName("latitud");
            entity.Property(e => e.Longitud).HasColumnName("longitud");
            entity.Property(e => e.MotivoEstado).HasColumnName("motivo_estado");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
            entity.Property(e => e.OrganizacionId).HasColumnName("organizacion_id");
            entity.Property(e => e.TelefonoContacto).HasColumnName("telefono_contacto");
            entity.Property(e => e.TipoCentroId).HasColumnName("tipo_centro_id");
            entity.Property(e => e.ZonaId).HasColumnName("zona_id");

            entity.HasOne(d => d.AprobadoPorNavigation).WithMany(p => p.CentroAprobadoPorNavigation)
                .HasForeignKey(d => d.AprobadoPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_centro_aprobado_por");

            entity.HasOne(d => d.CreadoPorNavigation).WithMany(p => p.CentroCreadoPorNavigation)
                .HasForeignKey(d => d.CreadoPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_centro_creado_por");

            entity.HasOne(d => d.TipoCentro).WithMany(p => p.Centro)
                .HasForeignKey(d => d.TipoCentroId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_centro_tipo");

            entity.HasOne(d => d.Organizacion).WithMany(p => p.Centro)
                .HasPrincipalKey(p => new { p.Id, p.CiudadId })
                .HasForeignKey(d => new { d.OrganizacionId, d.CiudadId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_centro_organizacion_ciudad");

            entity.HasOne(d => d.Zona).WithMany(p => p.Centro)
                .HasPrincipalKey(p => new { p.Id, p.CiudadId })
                .HasForeignKey(d => new { d.ZonaId, d.CiudadId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_centro_zona_ciudad");
        });

        modelBuilder.Entity<CentroRecurso>(entity =>
        {
            entity.HasKey(e => new { e.CentroId, e.TipoRecursoId }).HasName("pk_centro_recurso");

            entity.ToTable("centro_recurso");

            entity.HasIndex(e => new { e.TipoRecursoId, e.Nivel }, "ix_centro_recurso_tipo");

            entity.Property(e => e.CentroId).HasColumnName("centro_id");
            entity.Property(e => e.TipoRecursoId).HasColumnName("tipo_recurso_id");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.ActualizadoPor).HasColumnName("actualizado_por");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Nivel).HasColumnName("nivel");

            entity.HasOne(d => d.ActualizadoPorNavigation).WithMany(p => p.CentroRecurso)
                .HasForeignKey(d => d.ActualizadoPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_centro_recurso_actualizado_por");

            entity.HasOne(d => d.Centro).WithMany(p => p.CentroRecurso)
                .HasForeignKey(d => d.CentroId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_centro_recurso_centro");

            entity.HasOne(d => d.TipoRecurso).WithMany(p => p.CentroRecurso)
                .HasForeignKey(d => d.TipoRecursoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_centro_recurso_tipo");
        });

        modelBuilder.Entity<Ciudad>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_ciudad");

            entity.ToTable("ciudad");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activa)
                .HasDefaultValue(true)
                .HasColumnName("activa");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
            entity.Property(e => e.Pais)
                .HasDefaultValueSql("'Argentina'::text")
                .HasColumnName("pais");
            entity.Property(e => e.Provincia).HasColumnName("provincia");
        });

        modelBuilder.Entity<EventoAdministrativo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_evento_administrativo");

            entity.ToTable("evento_administrativo", tb => tb.HasComment("Auditoría de acciones administrativas. No guardar datos sensibles innecesarios en detalle."));

            entity.HasIndex(e => e.CreadoAt, "ix_evento_administrativo").IsDescending();

            entity.HasIndex(e => new { e.Entidad, e.EntidadId }, "ix_evento_administrativo_entidad");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Accion).HasColumnName("accion");
            entity.Property(e => e.AdministradorId).HasColumnName("administrador_id");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.Entidad).HasColumnName("entidad");
            entity.Property(e => e.EntidadId).HasColumnName("entidad_id");

            entity.HasOne(d => d.Administrador).WithMany(p => p.EventoAdministrativo)
                .HasForeignKey(d => d.AdministradorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_evento_administrativo_admin");
        });

        modelBuilder.Entity<HistorialSolicitud>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_historial_solicitud");

            entity.ToTable("historial_solicitud", tb => tb.HasComment("Historial de cambios de estado de la solicitud. Lo escribe la capa Negocio en la misma transacción."));

            entity.HasIndex(e => new { e.SolicitudId, e.CreadoAt }, "ix_historial_solicitud");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.EstadoAnterior).HasColumnName("estado_anterior");
            entity.Property(e => e.EstadoNuevo).HasColumnName("estado_nuevo");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.SolicitudId).HasColumnName("solicitud_id");

            entity.HasOne(d => d.Actor).WithMany(p => p.HistorialSolicitud)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_historial_solicitud_actor");

            entity.HasOne(d => d.Solicitud).WithMany(p => p.HistorialSolicitud)
                .HasForeignKey(d => d.SolicitudId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_historial_solicitud_solicitud");
        });

        modelBuilder.Entity<Organizacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_organizacion");

            entity.ToTable("organizacion");

            entity.HasIndex(e => new { e.CiudadId, e.Estado }, "ix_organizacion_ciudad_estado");

            entity.HasIndex(e => e.RegistradaPor, "ix_organizacion_registrada_por");

            entity.HasIndex(e => new { e.Id, e.CiudadId }, "uq_organizacion_id_ciudad").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.AprobadaAt).HasColumnName("aprobada_at");
            entity.Property(e => e.AprobadaPor).HasColumnName("aprobada_por");
            entity.Property(e => e.CiudadId).HasColumnName("ciudad_id");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.EmailContacto).HasColumnName("email_contacto");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'pendiente'::text")
                .HasColumnName("estado");
            entity.Property(e => e.MotivoEstado).HasColumnName("motivo_estado");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
            entity.Property(e => e.RegistradaPor).HasColumnName("registrada_por");
            entity.Property(e => e.TelefonoContacto).HasColumnName("telefono_contacto");
            entity.Property(e => e.Tipo).HasColumnName("tipo");

            entity.HasOne(d => d.AprobadaPorNavigation).WithMany(p => p.OrganizacionAprobadaPorNavigation)
                .HasForeignKey(d => d.AprobadaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_organizacion_aprobada_por");

            entity.HasOne(d => d.Ciudad).WithMany(p => p.Organizacion)
                .HasForeignKey(d => d.CiudadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_organizacion_ciudad");

            entity.HasOne(d => d.RegistradaPorNavigation).WithMany(p => p.OrganizacionRegistradaPorNavigation)
                .HasForeignKey(d => d.RegistradaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_organizacion_registrada_por");
        });

        modelBuilder.Entity<OrganizacionUsuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_organizacion_usuario");

            entity.ToTable("organizacion_usuario");

            entity.HasIndex(e => new { e.OrganizacionId, e.Rol, e.Activo }, "ix_organizacion_usuario_org");

            entity.HasIndex(e => e.UsuarioId, "ix_organizacion_usuario_usuario");

            entity.HasIndex(e => new { e.OrganizacionId, e.UsuarioId }, "uq_organizacion_usuario").IsUnique();

            entity.HasIndex(e => e.UsuarioId, "ux_organizacion_usuario_responsable")
                .IsUnique()
                .HasFilter("((rol = 'responsable'::text) AND activo)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por");
            entity.Property(e => e.DesactivadoAt).HasColumnName("desactivado_at");
            entity.Property(e => e.MotivoBaja).HasColumnName("motivo_baja");
            entity.Property(e => e.OrganizacionId).HasColumnName("organizacion_id");
            entity.Property(e => e.Rol).HasColumnName("rol");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.CreadoPorNavigation).WithMany(p => p.OrganizacionUsuarioCreadoPorNavigation)
                .HasForeignKey(d => d.CreadoPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_organizacion_usuario_creado_por");

            entity.HasOne(d => d.Organizacion).WithMany(p => p.OrganizacionUsuario)
                .HasForeignKey(d => d.OrganizacionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_organizacion_usuario_org");

            entity.HasOne(d => d.Usuario).WithMany(p => p.OrganizacionUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_organizacion_usuario_usuario");
        });

        modelBuilder.Entity<Solicitud>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_solicitud");

            entity.ToTable("solicitud");

            entity.HasIndex(e => new { e.CiudadId, e.Estado, e.CreadoAt }, "ix_solicitud_estado_ciudad").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.SolicitanteId, e.CreadoAt }, "ix_solicitud_solicitante").IsDescending(false, true);

            entity.HasIndex(e => new { e.ZonaId, e.Estado }, "ix_solicitud_zona");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.AsignadaAt).HasColumnName("asignada_at");
            entity.Property(e => e.CanceladaAt).HasColumnName("cancelada_at");
            entity.Property(e => e.CanceladaPor).HasColumnName("cancelada_por");
            entity.Property(e => e.CiudadId).HasColumnName("ciudad_id");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.EnAtencionAt).HasColumnName("en_atencion_at");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'pendiente'::text")
                .HasColumnName("estado");
            entity.Property(e => e.MotivoCancelacion).HasColumnName("motivo_cancelacion");
            entity.Property(e => e.ParcialAt).HasColumnName("parcial_at");
            entity.Property(e => e.PersonasAfectadas).HasColumnName("personas_afectadas");
            entity.Property(e => e.ResueltaAt).HasColumnName("resuelta_at");
            entity.Property(e => e.SolicitanteId).HasColumnName("solicitante_id");
            entity.Property(e => e.Urgencia)
                .HasDefaultValueSql("'normal'::text")
                .HasColumnName("urgencia");
            entity.Property(e => e.ZonaId).HasColumnName("zona_id");

            entity.HasOne(d => d.CanceladaPorNavigation).WithMany(p => p.SolicitudCanceladaPorNavigation)
                .HasForeignKey(d => d.CanceladaPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_solicitud_cancelada_por");

            entity.HasOne(d => d.Ciudad).WithMany(p => p.Solicitud)
                .HasForeignKey(d => d.CiudadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_solicitud_ciudad");

            entity.HasOne(d => d.Solicitante).WithMany(p => p.SolicitudSolicitante)
                .HasForeignKey(d => d.SolicitanteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_solicitud_solicitante");

            entity.HasOne(d => d.Zona).WithMany(p => p.Solicitud)
                .HasPrincipalKey(p => new { p.Id, p.CiudadId })
                .HasForeignKey(d => new { d.ZonaId, d.CiudadId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_solicitud_zona_ciudad");
        });

        modelBuilder.Entity<SolicitudContacto>(entity =>
        {
            entity.HasKey(e => e.SolicitudId).HasName("pk_solicitud_contacto");

            entity.ToTable("solicitud_contacto", tb => tb.HasComment("Dirección y teléfono exactos. Solo solicitante, asignados activos y administrador."));

            entity.Property(e => e.SolicitudId)
                .ValueGeneratedNever()
                .HasColumnName("solicitud_id");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Direccion).HasColumnName("direccion");
            entity.Property(e => e.Telefono).HasColumnName("telefono");

            entity.HasOne(d => d.Solicitud).WithOne(p => p.SolicitudContacto)
                .HasForeignKey<SolicitudContacto>(d => d.SolicitudId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_solicitud_contacto_solicitud");
        });

        modelBuilder.Entity<SolicitudRecurso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_solicitud_recurso");

            entity.ToTable("solicitud_recurso");

            entity.HasIndex(e => new { e.SolicitudId, e.Estado }, "ix_solicitud_recurso_solicitud");

            entity.HasIndex(e => new { e.TipoRecursoId, e.Estado }, "ix_solicitud_recurso_tipo");

            entity.HasIndex(e => new { e.SolicitudId, e.TipoRecursoId }, "uq_solicitud_recurso_tipo").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'pendiente'::text")
                .HasColumnName("estado");
            entity.Property(e => e.Personas)
                .HasComment("Personas alcanzadas por ESTE recurso. Es la unidad de la métrica; una misma persona puede contarse en más de un recurso.")
                .HasColumnName("personas");
            entity.Property(e => e.ResueltaAt).HasColumnName("resuelta_at");
            entity.Property(e => e.SolicitudId).HasColumnName("solicitud_id");
            entity.Property(e => e.TipoRecursoId).HasColumnName("tipo_recurso_id");

            entity.HasOne(d => d.Solicitud).WithMany(p => p.SolicitudRecurso)
                .HasForeignKey(d => d.SolicitudId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_solicitud_recurso_solicitud");

            entity.HasOne(d => d.TipoRecurso).WithMany(p => p.SolicitudRecurso)
                .HasForeignKey(d => d.TipoRecursoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_solicitud_recurso_tipo");
        });

        modelBuilder.Entity<TipoCentro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_tipo_centro");

            entity.ToTable("tipo_centro");

            entity.HasIndex(e => e.Codigo, "uq_tipo_centro_codigo").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.Codigo).HasColumnName("codigo");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
        });

        modelBuilder.Entity<TipoRecurso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_tipo_recurso");

            entity.ToTable("tipo_recurso");

            entity.HasIndex(e => e.Codigo, "uq_tipo_recurso_codigo").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.Codigo).HasColumnName("codigo");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
        });

        modelBuilder.Entity<UsuarioDatosPrivados>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("pk_usuario_datos_privados");

            entity.ToTable("usuario_datos_privados", tb => tb.HasComment("DNI y datos sensibles separados de la tabla principal. Acceso solo administrador o el propio usuario."));

            entity.HasIndex(e => e.Dni, "ux_usuario_datos_privados_dni")
                .IsUnique()
                .HasFilter("(dni IS NOT NULL)");

            entity.Property(e => e.UsuarioId)
                .ValueGeneratedNever()
                .HasColumnName("usuario_id");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Dni).HasColumnName("dni");

            entity.HasOne(d => d.Usuario).WithOne(p => p.UsuarioDatosPrivados)
                .HasForeignKey<UsuarioDatosPrivados>(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_usuario_datos_privados_usuario");
        });

        modelBuilder.Entity<VCentrosPublicos>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_centros_publicos")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.CentroActualizadoAt).HasColumnName("centro_actualizado_at");
            entity.Property(e => e.CiudadId).HasColumnName("ciudad_id");
            entity.Property(e => e.CiudadNombre).HasColumnName("ciudad_nombre");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Direccion).HasColumnName("direccion");
            entity.Property(e => e.EmailContacto).HasColumnName("email_contacto");
            entity.Property(e => e.EstadoOperativo).HasColumnName("estado_operativo");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Latitud).HasColumnName("latitud");
            entity.Property(e => e.Longitud).HasColumnName("longitud");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
            entity.Property(e => e.OrganizacionId).HasColumnName("organizacion_id");
            entity.Property(e => e.OrganizacionNombre).HasColumnName("organizacion_nombre");
            entity.Property(e => e.OrganizacionTipo).HasColumnName("organizacion_tipo");
            entity.Property(e => e.Recursos)
                .HasColumnType("jsonb")
                .HasColumnName("recursos");
            entity.Property(e => e.RecursosActualizadoAt).HasColumnName("recursos_actualizado_at");
            entity.Property(e => e.TelefonoContacto).HasColumnName("telefono_contacto");
            entity.Property(e => e.TipoCentroCodigo).HasColumnName("tipo_centro_codigo");
            entity.Property(e => e.TipoCentroNombre).HasColumnName("tipo_centro_nombre");
            entity.Property(e => e.ZonaId).HasColumnName("zona_id");
            entity.Property(e => e.ZonaNombre).HasColumnName("zona_nombre");
        });

        modelBuilder.Entity<VMetricasPersonasPorRecurso>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_metricas_personas_por_recurso")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.CiudadId).HasColumnName("ciudad_id");
            entity.Property(e => e.CiudadNombre).HasColumnName("ciudad_nombre");
            entity.Property(e => e.Mes).HasColumnName("mes");
            entity.Property(e => e.PersonasAlcanzadas).HasColumnName("personas_alcanzadas");
            entity.Property(e => e.RecursosResueltos).HasColumnName("recursos_resueltos");
            entity.Property(e => e.TipoRecursoCodigo).HasColumnName("tipo_recurso_codigo");
            entity.Property(e => e.TipoRecursoNombre).HasColumnName("tipo_recurso_nombre");
        });

        modelBuilder.Entity<VSolicitudesAbiertas>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_solicitudes_abiertas")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.ActualizadoAt).HasColumnName("actualizado_at");
            entity.Property(e => e.CiudadId).HasColumnName("ciudad_id");
            entity.Property(e => e.CiudadNombre).HasColumnName("ciudad_nombre");
            entity.Property(e => e.CreadoAt).HasColumnName("creado_at");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PersonasAfectadas).HasColumnName("personas_afectadas");
            entity.Property(e => e.Recursos)
                .HasColumnType("jsonb")
                .HasColumnName("recursos");
            entity.Property(e => e.Urgencia).HasColumnName("urgencia");
            entity.Property(e => e.ZonaId).HasColumnName("zona_id");
            entity.Property(e => e.ZonaNombre).HasColumnName("zona_nombre");
        });

        modelBuilder.Entity<Voluntario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("pk_voluntario");

            entity.ToTable("voluntario");

            entity.HasIndex(e => new { e.CiudadId, e.ZonaId, e.Activo }, "ix_voluntario_ciudad_zona");

            entity.Property(e => e.UsuarioId)
                .ValueGeneratedNever()
                .HasColumnName("usuario_id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.CiudadId).HasColumnName("ciudad_id");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.TelefonoContacto).HasColumnName("telefono_contacto");
            entity.Property(e => e.ZonaId).HasColumnName("zona_id");

            entity.HasOne(d => d.Ciudad).WithMany(p => p.Voluntario)
                .HasForeignKey(d => d.CiudadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_voluntario_ciudad");

            entity.HasOne(d => d.Usuario).WithOne(p => p.Voluntario)
                .HasForeignKey<Voluntario>(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_voluntario_usuario");

            entity.HasOne(d => d.Zona).WithMany(p => p.Voluntario)
                .HasPrincipalKey(p => new { p.Id, p.CiudadId })
                .HasForeignKey(d => new { d.ZonaId, d.CiudadId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_voluntario_zona_ciudad");
        });

        modelBuilder.Entity<Zona>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_zona");

            entity.ToTable("zona");

            entity.HasIndex(e => e.CiudadId, "ix_zona_ciudad");

            entity.HasIndex(e => new { e.Id, e.CiudadId }, "uq_zona_id_ciudad").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Activa)
                .HasDefaultValue(true)
                .HasColumnName("activa");
            entity.Property(e => e.ActualizadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("actualizado_at");
            entity.Property(e => e.CiudadId).HasColumnName("ciudad_id");
            entity.Property(e => e.CreadoAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("creado_at");
            entity.Property(e => e.Nombre).HasColumnName("nombre");

            entity.HasOne(d => d.Ciudad).WithMany(p => p.Zona)
                .HasForeignKey(d => d.CiudadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_zona_ciudad");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
