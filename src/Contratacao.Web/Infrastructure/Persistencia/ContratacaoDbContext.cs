using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Parametros;
using Contratacao.Web.Domain.Qqp;
using Contratacao.Web.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Web.Infrastructure.Persistencia;

/// <summary>Banco do sistema (seção 23). O script do modelo físico é o alvo das migrations.</summary>
internal sealed class ContratacaoDbContext(DbContextOptions<ContratacaoDbContext> options) : DbContext(options)
{
    internal DbSet<Demanda> Demandas => Set<Demanda>();
    internal DbSet<Usuario> Usuarios => Set<Usuario>();
    internal DbSet<GestorContrato> GestoresContratos => Set<GestorContrato>();
    internal DbSet<Contratada> Contratadas => Set<Contratada>();
    internal DbSet<Contrato> Contratos => Set<Contrato>();
    internal DbSet<Corredor> Corredores => Set<Corredor>();
    internal DbSet<TipoDemanda> TiposDemanda => Set<TipoDemanda>();
    internal DbSet<GerenteExecutivo> GerentesExecutivos => Set<GerenteExecutivo>();
    internal DbSet<ModeloTrabalho> ModelosTrabalho => Set<ModeloTrabalho>();
    internal DbSet<ItemEquipamento> ItensEquipamento => Set<ItemEquipamento>();
    internal DbSet<Rac> Racs => Set<Rac>();
    internal DbSet<QqpRegiao> QqpRegioes => Set<QqpRegiao>();
    internal DbSet<QqpFuncao> QqpFuncoes => Set<QqpFuncao>();
    internal DbSet<QqpClassificacao> QqpClassificacoes => Set<QqpClassificacao>();
    internal DbSet<QqpNivel> QqpNiveis => Set<QqpNivel>();
    internal DbSet<QqpCargaHoraria> QqpCargasHorarias => Set<QqpCargaHoraria>();
    internal DbSet<ItemQqp> ItensQqp => Set<ItemQqp>();
    internal DbSet<Anexo> Anexos => Set<Anexo>();
    internal DbSet<HistoricoAlteracao> HistoricosAlteracao => Set<HistoricoAlteracao>();
    internal DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();
    internal DbSet<ParametroSistema> Parametros => Set<ParametroSistema>();
    internal DbSet<SequenciaNumeroDemanda> SequenciasNumeroDemanda => Set<SequenciaNumeroDemanda>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<ConversorDataUtc>();
        configurationBuilder.Properties<Perfil>().HaveConversion<ConversorPerfil>();
        configurationBuilder.Properties<Etapa>().HaveConversion<ConversorEtapa>();
        configurationBuilder.Properties<StatusDemanda>().HaveConversion<ConversorStatus>();
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContratacaoDbContext).Assembly);

        // Nada se apaga em cascata (RN06): todas as chaves estrangeiras com ON DELETE NO ACTION.
        foreach (var chave in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            chave.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
