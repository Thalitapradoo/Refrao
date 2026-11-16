using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Context;

public partial class RefraoDbContext : DbContext
{
    public RefraoDbContext()
    {
    }

    public RefraoDbContext(DbContextOptions<RefraoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Album> Albuns { get; set; }

    public virtual DbSet<Artista> Artistas { get; set; }

    public virtual DbSet<Genero> Generos { get; set; }

    public virtual DbSet<Musica> Musicas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<ComentarioRefrao> ComentariosRefrao { get; set; }

    public virtual DbSet<Sugestao> Sugestoes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Album>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Albuns");

            entity.HasOne(d => d.Artista).WithMany(p => p.Albuns)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Albuns_Artistas");
        });

        modelBuilder.Entity<Artista>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Artistas");
        });

        modelBuilder.Entity<Genero>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Generos");
        });

        modelBuilder.Entity<Musica>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Musicas");

            entity.HasOne(d => d.Artista).WithMany(p => p.Musicas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Musicas_Artistas");

            // No banco a FK é composta (AlbumId, ArtistaId) para garantir que o álbum
            // seja do mesmo artista. Para o EF basta o relacionamento pelo AlbumId.
            entity.HasOne(d => d.Album).WithMany(p => p.Musicas)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Genero).WithMany(p => p.Musicas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Musicas_Generos");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Usuarios");

            entity.ToTable(tb => tb.HasTrigger("TR_Usuarios_SoftDelete"));

            entity.Property(e => e.Ativo).HasDefaultValue(true);
            entity.Property(e => e.Perfil).HasDefaultValue("Usuario");
            entity.Property(e => e.DataCadastro).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<ComentarioRefrao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ComentariosRefrao");

            entity.Property(e => e.DataCriacao).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Musica).WithMany(p => p.Comentarios)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ComentariosRefrao_Musicas");

            entity.HasOne(d => d.Usuario).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComentariosRefrao_Usuarios");
        });

        modelBuilder.Entity<Sugestao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Sugestoes");

            entity.Property(e => e.Status).HasDefaultValue("Pendente");
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Usuario).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sugestoes_Usuarios");

            entity.HasOne(d => d.Musica).WithMany()
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Sugestoes_Musicas");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
