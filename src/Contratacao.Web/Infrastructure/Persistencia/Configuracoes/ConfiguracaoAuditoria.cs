using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Parametros;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Web.Infrastructure.Persistencia.Configuracoes;

internal sealed class ConfiguracaoLogAuditoria : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> b)
    {
        b.ToTable("LogAuditoria");
        b.HasKey(l => l.Id);
        b.Property(l => l.UsuarioId);
        b.Property(l => l.PerfilUsuario).HasMaxLength(50).IsRequired();
        b.Property(l => l.EnderecoIp).HasMaxLength(45).IsUnicode(false);
        b.Property(l => l.Entidade).HasMaxLength(100).IsRequired();
        b.Property(l => l.EntidadeId);
        b.Property(l => l.Acao).HasMaxLength(100).IsRequired();
        b.Property(l => l.ValorAnterior);
        b.Property(l => l.NovoValor);
        b.Property(l => l.Justificativa).HasMaxLength(1000);
        b.Property(l => l.DataHora);

        b.HasIndex(l => new { l.Entidade, l.EntidadeId, l.DataHora }).HasDatabaseName("IX_Log_Entidade");
        b.HasOne<Usuario>().WithMany().HasForeignKey(l => l.UsuarioId).HasConstraintName("FK_Log_Usuario");
    }
}

internal sealed class ConfiguracaoHistoricoAlteracao : IEntityTypeConfiguration<HistoricoAlteracao>
{
    public void Configure(EntityTypeBuilder<HistoricoAlteracao> b)
    {
        b.ToTable("HistoricoAlteracao");
        b.HasKey(h => h.Id);
        b.Property(h => h.DemandaId);
        b.Property(h => h.UsuarioId);
        b.Property(h => h.Campo).HasMaxLength(150).IsRequired();
        b.Property(h => h.ValorAnterior);
        b.Property(h => h.NovoValor);
        b.Property(h => h.Justificativa).HasMaxLength(1000);
        b.Property(h => h.DataHora);

        b.HasIndex(h => new { h.DemandaId, h.DataHora }).HasDatabaseName("IX_HistAlt_Demanda");
        b.HasOne<Demanda>().WithMany().HasForeignKey(h => h.DemandaId).HasConstraintName("FK_HistAlt_Demanda");
        b.HasOne<Usuario>().WithMany().HasForeignKey(h => h.UsuarioId).HasConstraintName("FK_HistAlt_Usuario");
    }
}

internal sealed class ConfiguracaoAnexo : IEntityTypeConfiguration<Anexo>
{
    public void Configure(EntityTypeBuilder<Anexo> b)
    {
        b.ToTable("Anexo", t => t.HasCheckConstraint("CK_Anexo_Categoria", "Categoria IN ('Geral', 'DeAcordoVP2')"));

        b.HasKey(a => a.Id);
        b.Property(a => a.DemandaId);
        b.Property(a => a.Etapa).HasColumnName("EtapaId");
        b.Property(a => a.UsuarioUploadId);
        b.Property(a => a.NomeArquivo).HasMaxLength(260).IsRequired();
        b.Property(a => a.TipoArquivo).HasMaxLength(150).IsRequired();
        b.Property(a => a.Tamanho);
        b.Property(a => a.Identificador).HasMaxLength(500).IsRequired();
        b.Property(a => a.Categoria).HasConversion<string>().HasMaxLength(30).IsUnicode(false);
        b.Property(a => a.DataUpload);

        b.HasIndex(a => a.DemandaId).HasDatabaseName("IX_Anexo_Demanda");
        b.HasOne<Demanda>().WithMany().HasForeignKey(a => a.DemandaId).HasConstraintName("FK_Anexo_Demanda");
        b.HasOne<LinhaEtapa>().WithMany().HasForeignKey(a => a.Etapa).HasConstraintName("FK_Anexo_Etapa");
        b.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioUploadId).HasConstraintName("FK_Anexo_Usuario");
    }
}

internal sealed class ConfiguracaoParametroSistema : IEntityTypeConfiguration<ParametroSistema>
{
    public void Configure(EntityTypeBuilder<ParametroSistema> b)
    {
        b.ToTable("ParametroSistema");
        b.HasKey(p => p.Chave);
        b.Property(p => p.Chave).HasMaxLength(100).IsUnicode(false);
        b.Property(p => p.Valor).HasMaxLength(500).IsRequired();
        b.Property(p => p.DataAlteracao);
        b.Property(p => p.AlteradoPorId);

        b.HasOne<Usuario>().WithMany().HasForeignKey(p => p.AlteradoPorId).HasConstraintName("FK_Parametro_Usuario");

        // Prazo padrão do SLA (RN07).
        b.HasData(new
        {
            Chave = ParametroSistema.PrazoSlaDias,
            Valor = "45",
            DataAlteracao = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}

internal sealed class ConfiguracaoSequenciaNumeroDemanda : IEntityTypeConfiguration<SequenciaNumeroDemanda>
{
    public void Configure(EntityTypeBuilder<SequenciaNumeroDemanda> b)
    {
        b.ToTable("SequenciaNumeroDemanda");
        b.HasKey(s => s.Ano);
        b.Property(s => s.Ano).ValueGeneratedNever();
        b.Property(s => s.UltimoNumero);
    }
}
