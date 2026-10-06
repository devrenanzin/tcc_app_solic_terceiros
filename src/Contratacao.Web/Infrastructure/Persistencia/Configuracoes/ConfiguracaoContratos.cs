using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Qqp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratacao.Web.Infrastructure.Persistencia.Configuracoes;

internal sealed class ConfiguracaoContratada : IEntityTypeConfiguration<Contratada>
{
    public void Configure(EntityTypeBuilder<Contratada> b)
    {
        b.ToTable("Contratada");
        b.HasKey(c => c.Id);
        b.Property(c => c.RazaoSocial).HasMaxLength(200).IsRequired();
        b.Property(c => c.NomeFantasia).HasMaxLength(200);
        b.Property(c => c.Cnpj).HasColumnName("CNPJ").HasMaxLength(14).IsFixedLength().IsUnicode(false);
        b.Property(c => c.Ativo);

        // CNPJ opcional (Cliente); quando informado, não se repete.
        b.HasIndex(c => c.Cnpj).IsUnique().HasFilter("[CNPJ] IS NOT NULL").HasDatabaseName("UQ_Contratada_CNPJ");

        b.HasData(new { Id = IdsFixos.ContratadaSesi, RazaoSocial = "SESI", Ativo = true });
    }
}

internal sealed class ConfiguracaoContrato : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> b)
    {
        b.ToTable("Contrato", t =>
            t.HasCheckConstraint("CK_Contrato_Numero", "Numero LIKE '59%' AND Numero NOT LIKE '%[^0-9]%'"));

        b.HasKey(c => c.Id);
        b.Property(c => c.ContratadaId);
        b.Property(c => c.Numero).HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(c => c.Ativo);

        b.HasIndex(c => c.Numero).IsUnique().HasDatabaseName("UQ_Contrato_Numero");
        b.HasOne<Contratada>().WithMany().HasForeignKey(c => c.ContratadaId).HasConstraintName("FK_Contrato_Contratada");

        // RN13, confirmado pelo cliente.
        b.HasData(
            new { Id = IdsFixos.ContratoNorte, ContratadaId = IdsFixos.ContratadaSesi, Numero = "5900125082", Ativo = true },
            new { Id = IdsFixos.ContratoSudeste, ContratadaId = IdsFixos.ContratadaSesi, Numero = "5900118506", Ativo = true });
    }
}

internal sealed class ConfiguracaoOrdemServico : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> b)
    {
        // OS de cada contrato (Cliente, revisão de 06/10/2026); vazia na carga inicial, cadastrada pelo Admin.
        b.ToTable("OrdemServico");
        b.HasKey(o => o.Id);
        b.Property(o => o.ContratoId);
        b.Property(o => o.Numero).HasMaxLength(OrdemServico.TamanhoMaximoNumero).IsRequired();
        b.Property(o => o.Ativo);

        b.HasIndex(o => new { o.ContratoId, o.Numero }).IsUnique().HasDatabaseName("UQ_OrdemServico_ContratoNumero");
        b.HasOne<Contrato>().WithMany().HasForeignKey(o => o.ContratoId).HasConstraintName("FK_OrdemServico_Contrato");
    }
}

internal sealed class ConfiguracaoCorredor : IEntityTypeConfiguration<Corredor>
{
    public void Configure(EntityTypeBuilder<Corredor> b)
    {
        b.ToTable("Corredor");
        b.HasKey(c => c.Id);
        b.Property(c => c.Nome).HasMaxLength(50).IsRequired();
        b.Property(c => c.RegiaoId);
        b.Property(c => c.ContratoId);
        b.Property(c => c.Ativo);

        b.HasIndex(c => new { c.Nome, c.RegiaoId }).IsUnique().HasDatabaseName("UQ_Corredor_NomeRegiao");
        b.HasOne<QqpRegiao>().WithMany().HasForeignKey(c => c.RegiaoId).HasConstraintName("FK_Corredor_Regiao");
        b.HasOne<Contrato>().WithMany().HasForeignKey(c => c.ContratoId).HasConstraintName("FK_Corredor_Contrato");

        // RN13: corredores da região Norte no contrato do Norte; da região Sudeste, no do Sudeste.
        var (norte, sudeste) = (IdsFixos.RegiaoNorte, IdsFixos.RegiaoSudeste);
        var (contratoNorte, contratoSudeste) = (IdsFixos.ContratoNorte, IdsFixos.ContratoSudeste);
        b.HasData(
            new { Id = IdsFixos.CorredorNorte, Nome = "Norte", RegiaoId = norte, ContratoId = contratoNorte, Ativo = true },
            new { Id = IdsFixos.CorredorPelotizacaoNorte, Nome = "Pelotização", RegiaoId = norte, ContratoId = contratoNorte, Ativo = true },
            new { Id = IdsFixos.CorredorIntegradoNorte, Nome = "C. Integrado", RegiaoId = norte, ContratoId = contratoNorte, Ativo = true },
            new { Id = IdsFixos.CorredorSudeste, Nome = "Sudeste", RegiaoId = sudeste, ContratoId = contratoSudeste, Ativo = true },
            new { Id = IdsFixos.CorredorSul, Nome = "Sul", RegiaoId = sudeste, ContratoId = contratoSudeste, Ativo = true },
            new { Id = IdsFixos.CorredorPelotizacaoSudeste, Nome = "Pelotização", RegiaoId = sudeste, ContratoId = contratoSudeste, Ativo = true },
            new { Id = IdsFixos.CorredorIntegradoSudeste, Nome = "C. Integrado", RegiaoId = sudeste, ContratoId = contratoSudeste, Ativo = true });
    }
}
