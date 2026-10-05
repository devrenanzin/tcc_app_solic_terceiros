using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Infrastructure.Persistencia.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Web.Infrastructure.Persistencia.Configuracoes;

internal sealed class ConfiguracaoEtapa : IEntityTypeConfiguration<LinhaEtapa>
{
    public void Configure(EntityTypeBuilder<LinhaEtapa> b)
    {
        b.ToTable("Etapa");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.Nome).HasMaxLength(80).IsRequired();
        b.Property(e => e.Ordem);
        b.Property(e => e.Ativa);
        b.HasIndex(e => e.Nome).IsUnique().HasDatabaseName("UQ_Etapa_Nome");
        b.HasIndex(e => e.Ordem).IsUnique().HasDatabaseName("UQ_Etapa_Ordem");

        b.HasData(
            new LinhaEtapa { Id = Etapa.Solicitacao, Nome = "Solicitação", Ordem = 1, Ativa = true },
            new LinhaEtapa { Id = Etapa.ValidacaoGestor, Nome = "Validação do Gestor", Ordem = 2, Ativa = true },
            new LinhaEtapa { Id = Etapa.ValidacaoSesi, Nome = "Validação SESI", Ordem = 3, Ativa = true },
            new LinhaEtapa { Id = Etapa.Recrutamento, Nome = "Recrutamento", Ordem = 4, Ativa = true },
            new LinhaEtapa { Id = Etapa.Entrevistas, Nome = "Entrevistas", Ordem = 5, Ativa = true },
            new LinhaEtapa { Id = Etapa.ExamesMedicos, Nome = "Exames Médicos", Ordem = 6, Ativa = true },
            new LinhaEtapa { Id = Etapa.Contratacao, Nome = "Contratação", Ordem = 7, Ativa = true });
    }
}

internal sealed class ConfiguracaoStatus : IEntityTypeConfiguration<LinhaStatus>
{
    public void Configure(EntityTypeBuilder<LinhaStatus> b)
    {
        b.ToTable("Status");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.Nome).HasMaxLength(80).IsRequired();
        b.Property(s => s.Ativo);
        b.HasIndex(s => s.Nome).IsUnique().HasDatabaseName("UQ_Status_Nome");

        b.HasData(
            new LinhaStatus { Id = StatusDemanda.EmAnalise, Nome = "Em análise", Ativo = true },
            new LinhaStatus { Id = StatusDemanda.AguardandoCorrecao, Nome = "Aguardando correção", Ativo = true },
            new LinhaStatus { Id = StatusDemanda.AguardandoResponsavel, Nome = "Aguardando responsável", Ativo = true },
            new LinhaStatus { Id = StatusDemanda.EmAndamento, Nome = "Em andamento", Ativo = true },
            new LinhaStatus { Id = StatusDemanda.Concluido, Nome = "Concluído", Ativo = true },
            new LinhaStatus { Id = StatusDemanda.Cancelado, Nome = "Cancelado", Ativo = true });
    }
}

internal sealed class ConfiguracaoTipoDemanda : IEntityTypeConfiguration<TipoDemanda>
{
    public void Configure(EntityTypeBuilder<TipoDemanda> b)
    {
        b.ToTable("TipoDemanda");
        b.HasKey(t => t.Id);
        b.Property(t => t.Nome).HasMaxLength(100).IsRequired();
        b.Property(t => t.Ativo);
        b.HasIndex(t => t.Nome).IsUnique().HasDatabaseName("UQ_TipoDemanda_Nome");

        b.HasData(new { Id = IdsFixos.TipoNovaContratacao, Nome = "Nova contratação", Ativo = true });
    }
}

internal sealed class ConfiguracaoGerenteExecutivo : IEntityTypeConfiguration<GerenteExecutivo>
{
    public void Configure(EntityTypeBuilder<GerenteExecutivo> b)
    {
        // Vazio na carga inicial: são nomes de pessoas, cadastrados pelo Admin.
        b.ToTable("GerenteExecutivo");
        b.HasKey(g => g.Id);
        b.Property(g => g.Nome).HasMaxLength(150).IsRequired();
        b.Property(g => g.Ativo);
    }
}

internal sealed class ConfiguracaoModeloTrabalho : IEntityTypeConfiguration<ModeloTrabalho>
{
    public void Configure(EntityTypeBuilder<ModeloTrabalho> b)
    {
        b.ToTable("ModeloTrabalho");
        b.HasKey(m => m.Id);
        b.Property(m => m.Nome).HasMaxLength(30).IsRequired();
        b.HasIndex(m => m.Nome).IsUnique().HasDatabaseName("UQ_ModeloTrabalho_Nome");

        b.HasData(
            new { Id = IdsFixos.ModeloPresencial, Nome = "Presencial" },
            new { Id = IdsFixos.ModeloHibrido, Nome = "Híbrido" },
            new { Id = IdsFixos.ModeloRemoto, Nome = "Remoto" });
    }
}

internal sealed class ConfiguracaoVeiculo : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> b)
    {
        b.ToTable("Veiculo");
        b.HasKey(v => v.Id);
        b.Property(v => v.Tipo).HasMaxLength(100).IsRequired();
        b.Property(v => v.Valor);
        b.Property(v => v.Ativo);
        b.HasIndex(v => v.Tipo).IsUnique().HasDatabaseName("UQ_Veiculo_Tipo");

        // Lista Tb_veiculos informada pelo cliente (RN12).
        b.HasData(
            new { Id = IdsFixos.Veiculo4x4, Tipo = "Veículo 4x4", Valor = 9113.47m, Ativo = true },
            new { Id = IdsFixos.VeiculoPasseio, Tipo = "Veículo de passeio", Valor = 5292.29m, Ativo = true },
            new { Id = IdsFixos.VeiculoVan, Tipo = "Veículo van", Valor = 18874.06m, Ativo = true },
            new { Id = IdsFixos.VeiculoTransporte, Tipo = "Transporte", Valor = 539.26m, Ativo = true });
    }
}

internal sealed class ConfiguracaoItemEquipamento : IEntityTypeConfiguration<ItemEquipamento>
{
    public void Configure(EntityTypeBuilder<ItemEquipamento> b)
    {
        b.ToTable("ItemEquipamento");
        b.HasKey(i => i.Id);
        b.Property(i => i.Nome).HasMaxLength(50).IsRequired();
        b.Property(i => i.Valor);
        b.Property(i => i.Ativo);
        b.HasIndex(i => i.Nome).IsUnique().HasDatabaseName("UQ_ItemEquipamento_Nome");

        // Valores confirmados pelo cliente (revisão de 05/10/2026, item 37).
        b.HasData(
            new { Id = IdsFixos.EquipamentoNotebook, Nome = "Notebook", Valor = 444.35m, Ativo = true },
            new { Id = IdsFixos.EquipamentoSegundaTela, Nome = "Segunda tela", Valor = 53.93m, Ativo = true },
            new { Id = IdsFixos.EquipamentoCelular, Nome = "Celular", Valor = 118.64m, Ativo = true },
            new { Id = IdsFixos.EquipamentoRastreador, Nome = "Rastreador", Valor = 345.13m, Ativo = true });
    }
}

internal sealed class ConfiguracaoRac : IEntityTypeConfiguration<Rac>
{
    public void Configure(EntityTypeBuilder<Rac> b)
    {
        // Carregada do arquivo dados/tb_racs.csv pela preparação do banco.
        b.ToTable("Rac");
        b.HasKey(r => r.Id);
        b.Property(r => r.Codigo).HasMaxLength(10).IsRequired();
        b.Property(r => r.Nome).HasMaxLength(100).IsRequired();
        b.HasIndex(r => r.Codigo).IsUnique().HasDatabaseName("UQ_Rac_Codigo");
    }
}
