using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Web.Infrastructure.Persistencia.Configuracoes;

// Demanda e tabelas filhas (seção 23). Os campos do formulário (seção 8.1) entram na Etapa 4.

internal sealed class ConfiguracaoDemanda : IEntityTypeConfiguration<Demanda>
{
    public void Configure(EntityTypeBuilder<Demanda> b)
    {
        b.ToTable("Demanda", t =>
        {
            t.HasCheckConstraint("CK_Demanda_Finalizacao", "DataFinalizacao IS NULL OR DataFinalizacao >= DataCriacao");
            t.HasCheckConstraint("CK_Demanda_Sla", "DataLimiteSLA IS NULL OR DataLimiteSLA >= CAST(DataInicioSLA AS date)");
        });

        b.HasKey(d => d.Id);
        b.Property(d => d.Numero).HasMaxLength(30).IsRequired();
        b.Property(d => d.UsuarioSolicitanteId);
        b.Property(d => d.GestorId);
        b.Property(d => d.ResponsavelSesiId);
        b.Property(d => d.ContratadaId);
        b.Property(d => d.ContratoId);
        b.Property(d => d.Etapa).HasColumnName("EtapaAtualId");
        b.Property(d => d.Status).HasColumnName("StatusAtualId");
        b.Property(d => d.DataCriacao);
        b.Property(d => d.DataEnvio);
        b.Property(d => d.DataFinalizacao);
        b.Property(d => d.MotivoCancelamento).HasMaxLength(1000);

        // Concorrência otimista (RNF10): o domínio não conhece o token.
        // IsRequired: sem ele, o SLA opcional na mesma tabela deixaria a coluna anulável.
        b.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();

        b.OwnsOne(d => d.Sla, sla =>
        {
            sla.Property(s => s.InicioUtc).HasColumnName("DataInicioSLA");
            sla.Property(s => s.PrazoDias).HasColumnName("PrazoDiasSla").HasConversion<short>();
            sla.Property(s => s.DataLimite).HasColumnName("DataLimiteSLA");
            sla.HasIndex(s => s.DataLimite).HasDatabaseName("IX_Demanda_LimiteSla");
        });
        b.Navigation(d => d.Sla).IsRequired(false);

        b.HasIndex(d => d.Numero).IsUnique().HasDatabaseName("UQ_Demanda_Numero");
        b.HasIndex(d => d.GestorId).HasDatabaseName("IX_Demanda_Gestor");
        b.HasIndex(d => d.ResponsavelSesiId).HasDatabaseName("IX_Demanda_ResponsavelSesi");
        b.HasIndex(d => d.UsuarioSolicitanteId).HasDatabaseName("IX_Demanda_Solicitante");
        b.HasIndex(d => new { d.Etapa, d.Status }).HasDatabaseName("IX_Demanda_EtapaStatus");

        b.HasOne<Usuario>().WithMany().HasForeignKey(d => d.UsuarioSolicitanteId).HasConstraintName("FK_Demanda_Solicitante");
        b.HasOne<Usuario>().WithMany().HasForeignKey(d => d.GestorId).HasConstraintName("FK_Demanda_Gestor");
        b.HasOne<Usuario>().WithMany().HasForeignKey(d => d.ResponsavelSesiId).HasConstraintName("FK_Demanda_ResponsavelSesi");
        b.HasOne<Contratada>().WithMany().HasForeignKey(d => d.ContratadaId).HasConstraintName("FK_Demanda_Contratada");
        b.HasOne<Contrato>().WithMany().HasForeignKey(d => d.ContratoId).HasConstraintName("FK_Demanda_Contrato");
        b.HasOne<LinhaEtapa>().WithMany().HasForeignKey(d => d.Etapa).HasConstraintName("FK_Demanda_Etapa");
        b.HasOne<LinhaStatus>().WithMany().HasForeignKey(d => d.Status).HasConstraintName("FK_Demanda_Status");

        b.HasMany(d => d.Etapas).WithOne().HasForeignKey("DemandaId").HasConstraintName("FK_EtapaDemanda_Demanda");
        b.HasMany(d => d.Correcoes).WithOne().HasForeignKey("DemandaId").HasConstraintName("FK_Correcao_Demanda");
        b.HasMany(d => d.Historico).WithOne().HasForeignKey("DemandaId").HasConstraintName("FK_HistDemanda_Demanda");
        b.HasOne(d => d.Vaga).WithOne().HasForeignKey<Vaga>("DemandaId").HasConstraintName("FK_Vaga_Demanda");

        b.Navigation(d => d.Etapas).HasField("_etapas").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(d => d.Correcoes).HasField("_correcoes").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(d => d.Historico).HasField("_historico").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ConfiguracaoEtapaDemanda : IEntityTypeConfiguration<EtapaDemanda>
{
    public void Configure(EntityTypeBuilder<EtapaDemanda> b)
    {
        b.ToTable("EtapaDemanda", t =>
            t.HasCheckConstraint("CK_EtapaDemanda_Datas", "DataConclusao IS NULL OR DataConclusao >= DataInicio"));

        b.HasKey(e => e.Id);
        b.Property(e => e.Etapa).HasColumnName("EtapaId");
        b.Property(e => e.Status).HasColumnName("StatusId");
        b.Property(e => e.UsuarioResponsavelId);
        b.Property(e => e.DataInicio);
        b.Property(e => e.DataConclusao);
        b.Property(e => e.Observacao).HasMaxLength(1000);

        b.HasIndex("DemandaId").HasDatabaseName("IX_EtapaDemanda_Demanda");
        b.HasOne<LinhaEtapa>().WithMany().HasForeignKey(e => e.Etapa).HasConstraintName("FK_EtapaDemanda_Etapa");
        b.HasOne<LinhaStatus>().WithMany().HasForeignKey(e => e.Status).HasConstraintName("FK_EtapaDemanda_Status");
        b.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioResponsavelId).HasConstraintName("FK_EtapaDemanda_Usuario");
    }
}

internal sealed class ConfiguracaoSolicitacaoCorrecao : IEntityTypeConfiguration<SolicitacaoCorrecao>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCorrecao> b)
    {
        b.ToTable("SolicitacaoCorrecao", t =>
        {
            t.HasCheckConstraint("CK_Correcao_Origem", "Origem IN ('Gestor', 'SESI')");
            t.HasCheckConstraint("CK_Correcao_Tipo", "Tipo IN ('Solicitante', 'Contratual')");
            t.HasCheckConstraint("CK_Correcao_TipoOrigem",
                "(Origem = 'SESI' AND Tipo IS NOT NULL) OR (Origem = 'Gestor' AND Tipo IS NULL)");
        });

        b.HasKey(c => c.Id);
        b.Property(c => c.SolicitadoPorId);
        b.Property(c => c.Origem).HasConversion<ConversorOrigemCorrecao>().HasMaxLength(20).IsUnicode(false);
        b.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.Property(c => c.Motivo).HasMaxLength(1000).IsRequired();
        b.Property(c => c.DataSolicitacao);
        b.Property(c => c.DataResolucao);

        b.HasIndex("DemandaId").HasDatabaseName("IX_Correcao_Demanda");
        b.HasOne<Usuario>().WithMany().HasForeignKey(c => c.SolicitadoPorId).HasConstraintName("FK_Correcao_Usuario");
    }
}

internal sealed class ConfiguracaoVaga : IEntityTypeConfiguration<Vaga>
{
    public void Configure(EntityTypeBuilder<Vaga> b)
    {
        b.ToTable("Vaga");
        b.HasKey(v => v.Id);
        b.Property(v => v.LinkExterno).HasMaxLength(2048).IsRequired();
        b.Property(v => v.DataAbertura);
        b.Property(v => v.UsuarioResponsavelId);

        b.HasIndex("DemandaId").IsUnique().HasDatabaseName("UQ_Vaga_Demanda");
        b.HasOne<Usuario>().WithMany().HasForeignKey(v => v.UsuarioResponsavelId).HasConstraintName("FK_Vaga_Usuario");
    }
}

internal sealed class ConfiguracaoHistoricoDemanda : IEntityTypeConfiguration<HistoricoDemanda>
{
    public void Configure(EntityTypeBuilder<HistoricoDemanda> b)
    {
        b.ToTable("HistoricoDemanda");
        b.HasKey(h => h.Id);
        b.Property(h => h.UsuarioId);
        b.Property(h => h.PerfilUsuario).HasConversion<string>().HasMaxLength(50);
        b.Property(h => h.EnderecoIp).HasMaxLength(45).IsUnicode(false);
        b.Property(h => h.DataHora);
        b.Property(h => h.Evento).HasColumnName("Acao").HasConversion<string>().HasMaxLength(100);
        b.Property(h => h.EtapaAnterior).HasColumnName("EtapaAnteriorId");
        b.Property(h => h.EtapaNova).HasColumnName("EtapaNovaId");
        b.Property(h => h.StatusAnterior).HasColumnName("StatusAnteriorId");
        b.Property(h => h.StatusNovo).HasColumnName("StatusNovoId");
        b.Property(h => h.Observacao).HasMaxLength(1000);

        b.HasIndex("DemandaId", nameof(HistoricoDemanda.DataHora)).HasDatabaseName("IX_HistDemanda_Demanda");
        b.HasOne<Usuario>().WithMany().HasForeignKey(h => h.UsuarioId).HasConstraintName("FK_HistDemanda_Usuario");
        b.HasOne<LinhaEtapa>().WithMany().HasForeignKey(h => h.EtapaAnterior).HasConstraintName("FK_HistDemanda_EtapaAnt");
        b.HasOne<LinhaEtapa>().WithMany().HasForeignKey(h => h.EtapaNova).HasConstraintName("FK_HistDemanda_EtapaNova");
        b.HasOne<LinhaStatus>().WithMany().HasForeignKey(h => h.StatusAnterior).HasConstraintName("FK_HistDemanda_StatusAnt");
        b.HasOne<LinhaStatus>().WithMany().HasForeignKey(h => h.StatusNovo).HasConstraintName("FK_HistDemanda_StatusNovo");
    }
}
