using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Infrastructure.Persistencia.Catalogos;

// Linhas das tabelas Perfil, Etapa e Status. No domínio esses valores são enums; estas classes existem
// só para criar as tabelas e as chaves estrangeiras do modelo físico (seção 23).

internal sealed class LinhaPerfil
{
    internal Perfil Id { get; set; }
    internal string Nome { get; set; } = string.Empty;
    internal string? Descricao { get; set; }
}

internal sealed class LinhaEtapa
{
    internal Etapa Id { get; set; }
    internal string Nome { get; set; } = string.Empty;
    internal short Ordem { get; set; }
    internal bool Ativa { get; set; }
}

internal sealed class LinhaStatus
{
    internal StatusDemanda Id { get; set; }
    internal string Nome { get; set; } = string.Empty;
    internal bool Ativo { get; set; }
}
