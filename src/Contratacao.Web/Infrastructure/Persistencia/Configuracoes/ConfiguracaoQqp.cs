using Contratacao.Web.Domain.Qqp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Web.Infrastructure.Persistencia.Configuracoes;

// Catálogo QQP (seção 23). Regiões vêm na migration porque os corredores dependem delas;
// o restante é carregado de dados/tb_qqp_SESI.csv pela preparação do banco.

internal sealed class ConfiguracaoQqpRegiao : IEntityTypeConfiguration<QqpRegiao>
{
    internal const string Norte = "QQP NORTE";
    internal const string Sudeste = "QQP SUDESTE";

    public void Configure(EntityTypeBuilder<QqpRegiao> b)
    {
        b.ToTable("QqpRegiao");
        b.HasKey(r => r.Id);
        b.Property(r => r.Nome).HasMaxLength(50).IsRequired();
        b.HasIndex(r => r.Nome).IsUnique().HasDatabaseName("UQ_QqpRegiao_Nome");

        b.HasData(
            new { Id = IdsFixos.RegiaoNorte, Nome = Norte },
            new { Id = IdsFixos.RegiaoSudeste, Nome = Sudeste });
    }
}

internal sealed class ConfiguracaoQqpFuncao : IEntityTypeConfiguration<QqpFuncao>
{
    public void Configure(EntityTypeBuilder<QqpFuncao> b)
    {
        b.ToTable("QqpFuncao");
        b.HasKey(f => f.Id);
        b.Property(f => f.Nome).HasMaxLength(100).IsRequired();
        b.HasIndex(f => f.Nome).IsUnique().HasDatabaseName("UQ_QqpFuncao_Nome");
    }
}

internal sealed class ConfiguracaoQqpClassificacao : IEntityTypeConfiguration<QqpClassificacao>
{
    public void Configure(EntityTypeBuilder<QqpClassificacao> b)
    {
        b.ToTable("QqpClassificacao");
        b.HasKey(c => c.Id);
        b.Property(c => c.Nome).HasMaxLength(50).IsRequired();
        b.HasIndex(c => c.Nome).IsUnique().HasDatabaseName("UQ_QqpClassificacao_Nome");
    }
}

internal sealed class ConfiguracaoQqpNivel : IEntityTypeConfiguration<QqpNivel>
{
    public void Configure(EntityTypeBuilder<QqpNivel> b)
    {
        b.ToTable("QqpNivel");
        b.HasKey(n => n.Id);
        b.Property(n => n.Nome).HasMaxLength(10).IsRequired();
        b.Property(n => n.Ordem);
        b.HasIndex(n => n.Nome).IsUnique().HasDatabaseName("UQ_QqpNivel_Nome");
    }
}

internal sealed class ConfiguracaoQqpCargaHoraria : IEntityTypeConfiguration<QqpCargaHoraria>
{
    public void Configure(EntityTypeBuilder<QqpCargaHoraria> b)
    {
        b.ToTable("QqpCargaHoraria");
        b.HasKey(c => c.Id);
        b.Property(c => c.HorasSemanais);
        b.HasIndex(c => c.HorasSemanais).IsUnique().HasDatabaseName("UQ_QqpCargaHoraria");
    }
}

internal sealed class ConfiguracaoItemQqp : IEntityTypeConfiguration<ItemQqp>
{
    public void Configure(EntityTypeBuilder<ItemQqp> b)
    {
        b.ToTable("ItemQqp");
        b.HasKey(i => i.Id);
        b.Property(i => i.Codigo);
        b.Property(i => i.RegiaoId);
        b.Property(i => i.FuncaoId);
        b.Property(i => i.ClassificacaoId);
        b.Property(i => i.NivelId);
        b.Property(i => i.CargaHorariaId);
        b.Property(i => i.PisoSalarial);
        b.Property(i => i.PrecoUnitario);
        b.Property(i => i.Ativo);

        b.HasIndex(i => i.Codigo).IsUnique().HasDatabaseName("UQ_ItemQqp_Codigo");
        b.HasIndex(i => new { i.RegiaoId, i.FuncaoId, i.ClassificacaoId, i.NivelId, i.CargaHorariaId })
            .HasDatabaseName("IX_ItemQqp_Busca");

        b.HasOne<QqpRegiao>().WithMany().HasForeignKey(i => i.RegiaoId).HasConstraintName("FK_ItemQqp_Regiao");
        b.HasOne<QqpFuncao>().WithMany().HasForeignKey(i => i.FuncaoId).HasConstraintName("FK_ItemQqp_Funcao");
        b.HasOne<QqpClassificacao>().WithMany().HasForeignKey(i => i.ClassificacaoId).HasConstraintName("FK_ItemQqp_Classificacao");
        b.HasOne<QqpNivel>().WithMany().HasForeignKey(i => i.NivelId).HasConstraintName("FK_ItemQqp_Nivel");
        b.HasOne<QqpCargaHoraria>().WithMany().HasForeignKey(i => i.CargaHorariaId).HasConstraintName("FK_ItemQqp_Carga");
    }
}
