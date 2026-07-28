using IdosoDigital.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdosoDigital.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ChatSessao> Chats => Set<ChatSessao>();
    public DbSet<Conversa> Conversas => Set<Conversa>();
    public DbSet<Exercicio> Exercicios => Set<Exercicio>();
    public DbSet<Resultado> Resultados => Set<Resultado>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Conteudo> Conteudos => Set<Conteudo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.SenhaHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<ChatSessao>(entity =>
        {
            entity.ToTable("Chats");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Titulo).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.Chats)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UsuarioId, x.DataAtualizacao });
        });

        modelBuilder.Entity<Conversa>(entity =>
        {
            entity.ToTable("Conversas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Pergunta).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Resposta).HasMaxLength(8000).IsRequired();
            entity.HasOne(x => x.Chat)
                .WithMany(x => x.Mensagens)
                .HasForeignKey(x => x.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.Conversas)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Exercicio>(entity =>
        {
            entity.ToTable("Exercicios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Pergunta).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.AlternativasJson).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.RespostaCorreta).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Explicacao).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Categoria).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<Resultado>(entity =>
        {
            entity.ToTable("Resultados");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.Resultados)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Exercicio)
                .WithMany(x => x.Resultados)
                .HasForeignKey(x => x.ExercicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedbacks");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Usuario)
                .WithMany(x => x.Feedbacks)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Conversa)
                .WithMany(x => x.Feedbacks)
                .HasForeignKey(x => x.ConversaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UsuarioId, x.ConversaId }).IsUnique();
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("Categorias");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Conteudo>(entity =>
        {
            entity.ToTable("Conteudos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Titulo).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Corpo).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.UrlMidia).HasMaxLength(500);
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(x => x.Categoria)
                .WithMany(x => x.Conteudos)
                .HasForeignKey(x => x.CategoriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
