using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Demandas;

/// <summary>
/// Catálogo de gerentes executivos (seção 23): vazio na carga inicial, cadastrado pelo Admin. Sem ele o
/// formulário da demanda não pode ser enviado, porque o campo é obrigatório.
/// </summary>
internal sealed class ManterGerentesExecutivos(ICatalogosDemanda catalogos, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal Task<IReadOnlyList<GerenteExecutivo>> ListarAsync(CancellationToken cancelamento) => catalogos.GerentesExecutivosAsync(cancelamento);

    internal async Task CadastrarAsync(Ator admin, string nome, CancellationToken cancelamento)
    {
        var gerente = GerenteExecutivo.Cadastrar(admin, nome);
        catalogos.Adicionar(gerente);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(GerenteExecutivo), gerente.Id, "Cadastro", null, gerente.Nome, null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }

    internal async Task AlterarSituacaoAsync(Ator admin, Guid id, bool ativo, CancellationToken cancelamento)
    {
        var gerente = await catalogos.GerenteExecutivoAsync(id, cancelamento)
            ?? throw new RegraNegocioException("Gerente executivo não encontrado.");

        gerente.AlterarSituacao(admin, ativo);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(GerenteExecutivo), gerente.Id, ativo ? "Reativacao" : "Desativacao",
            ativo ? "Inativo" : "Ativo", ativo ? "Ativo" : "Inativo", null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }
}
