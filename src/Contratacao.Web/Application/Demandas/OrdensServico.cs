using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Demandas;

/// <summary>
/// OS de cada contrato (Cliente, revisão de 06/10/2026), mantidas pelo Admin. Sem OS ativa no contrato do
/// corredor, o Solicitante não consegue enviar a demanda, porque o campo é obrigatório.
/// </summary>
internal sealed class ManterOrdensServico(
    ICatalogosDemanda catalogos, IContratos contratos, IAuditoria auditoria, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal Task<IReadOnlyList<OpcaoOs>> ListarAsync(CancellationToken cancelamento) => catalogos.OrdensServicoAsync(cancelamento);

    internal async Task CadastrarAsync(Ator admin, Guid contratoId, string numero, CancellationToken cancelamento)
    {
        var os = OrdemServico.Cadastrar(admin, contratoId, numero);
        var contrato = (await contratos.ListarAtivosAsync(cancelamento)).SingleOrDefault(c => c.Id == contratoId)
            ?? throw new RegraNegocioException("Escolha um contrato ativo.");
        if (await catalogos.OrdemServicoExisteAsync(contratoId, os.Numero, cancelamento))
        {
            throw new RegraNegocioException($"A OS {os.Numero} já existe no contrato {contrato.Numero}.");
        }

        catalogos.Adicionar(os);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(OrdemServico), os.Id, "Cadastro", null,
            $"OS {os.Numero}; contrato: {contrato.Numero}", null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }

    internal async Task AlterarSituacaoAsync(Ator admin, Guid id, bool ativo, CancellationToken cancelamento)
    {
        var os = await catalogos.OrdemServicoAsync(id, cancelamento) ?? throw new RegraNegocioException("OS não encontrada.");

        os.AlterarSituacao(admin, ativo);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(OrdemServico), os.Id, ativo ? "Reativacao" : "Desativacao",
            ativo ? "Inativo" : "Ativo", ativo ? "Ativo" : "Inativo", null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }
}
