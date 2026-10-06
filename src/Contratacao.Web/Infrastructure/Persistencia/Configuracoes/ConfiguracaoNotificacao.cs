using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Notificacoes;
using Contratacao.Web.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Web.Infrastructure.Persistencia.Configuracoes;

/// <summary>Avisos dentro do sistema (seção 29; Cliente, revisão de 06/10/2026). Nunca são excluídos.</summary>
internal sealed class ConfiguracaoNotificacao : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> b)
    {
        b.ToTable("Notificacao");
        b.HasKey(n => n.Id);
        b.Property(n => n.UsuarioId);
        b.Property(n => n.DemandaId);
        b.Property(n => n.Tipo).HasConversion<string>().HasMaxLength(40).IsUnicode(false);
        b.Property(n => n.Mensagem).HasMaxLength(Notificacao.TamanhoMaximoMensagem).IsRequired();
        b.Property(n => n.DataCriacao);
        b.Property(n => n.DataLeitura);

        b.HasIndex(n => new { n.UsuarioId, n.DataLeitura }).HasDatabaseName("IX_Notificacao_Usuario");
        b.HasIndex(n => new { n.DemandaId, n.Tipo }).HasDatabaseName("IX_Notificacao_DemandaTipo");
        b.HasOne<Usuario>().WithMany().HasForeignKey(n => n.UsuarioId).HasConstraintName("FK_Notificacao_Usuario");
        b.HasOne<Demanda>().WithMany().HasForeignKey(n => n.DemandaId).HasConstraintName("FK_Notificacao_Demanda");
    }
}
