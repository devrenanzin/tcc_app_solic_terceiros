using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Web.Infrastructure.Persistencia.Configuracoes;

internal sealed class ConfiguracaoPerfil : IEntityTypeConfiguration<LinhaPerfil>
{
    public void Configure(EntityTypeBuilder<LinhaPerfil> b)
    {
        b.ToTable("Perfil");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Nome).HasMaxLength(50).IsRequired();
        b.Property(p => p.Descricao).HasMaxLength(250);
        b.HasIndex(p => p.Nome).IsUnique().HasDatabaseName("UQ_Perfil_Nome");

        b.HasData(
            new LinhaPerfil { Id = Perfil.Admin, Nome = "Admin", Descricao = "Criador/Admin: gerencia Gestores, parâmetros e auditoria" },
            new LinhaPerfil { Id = Perfil.Gestor, Nome = "Gestor", Descricao = "Gestor do Contrato: valida, devolve e cancela demandas dos seus contratos" },
            new LinhaPerfil { Id = Perfil.Solicitante, Nome = "Solicitante", Descricao = "Usuário Solicitante: cria, envia e corrige demandas" },
            new LinhaPerfil { Id = Perfil.FuncionarioSesi, Nome = "FuncionarioSesi", Descricao = "Funcionário SESI: conduz o processo das demandas do seu contrato" });
    }
}

internal sealed class ConfiguracaoUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuario", t => t.HasCheckConstraint("CK_Usuario_Email_Dominio", "Email LIKE '%@ucl.br'"));

        b.HasKey(u => u.Id);
        b.Property(u => u.Perfil).HasColumnName("PerfilId");
        b.Property(u => u.GestorResponsavelId);
        b.Property(u => u.CriadoPorUsuarioId);
        b.Property(u => u.ContratoId);
        b.Property(u => u.Nome).HasMaxLength(150).IsRequired();
        b.Property(u => u.Email).HasMaxLength(254).IsRequired();
        b.Property(u => u.Login).HasMaxLength(254).IsRequired();
        b.Property(u => u.Ativo);
        b.Property(u => u.DataCadastro);
        b.Property(u => u.DataUltimoAcesso);
        b.Property(u => u.SenhaHash).HasMaxLength(500).IsRequired();
        b.Property(u => u.DeveTrocarSenha);

        b.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Usuario_Email");
        b.HasIndex(u => u.Login).IsUnique().HasDatabaseName("UQ_Usuario_Login");

        b.HasOne<LinhaPerfil>().WithMany().HasForeignKey(u => u.Perfil).HasConstraintName("FK_Usuario_Perfil");
        b.HasOne<Usuario>().WithMany().HasForeignKey(u => u.GestorResponsavelId).HasConstraintName("FK_Usuario_Gestor");
        b.HasOne<Usuario>().WithMany().HasForeignKey(u => u.CriadoPorUsuarioId).HasConstraintName("FK_Usuario_CriadoPor");
        b.HasOne<Contrato>().WithMany().HasForeignKey(u => u.ContratoId).HasConstraintName("FK_Usuario_Contrato");
    }
}

internal sealed class ConfiguracaoGestorContrato : IEntityTypeConfiguration<GestorContrato>
{
    public void Configure(EntityTypeBuilder<GestorContrato> b)
    {
        b.ToTable("GestorContrato");
        b.Property(g => g.GestorId);
        b.Property(g => g.ContratoId);
        b.HasKey(g => new { g.GestorId, g.ContratoId }).HasName("PK_GestorContrato");

        b.HasOne<Usuario>().WithMany().HasForeignKey(g => g.GestorId).HasConstraintName("FK_GestorContrato_Gestor");
        b.HasOne<Contrato>().WithMany().HasForeignKey(g => g.ContratoId).HasConstraintName("FK_GestorContrato_Contrato");
    }
}
