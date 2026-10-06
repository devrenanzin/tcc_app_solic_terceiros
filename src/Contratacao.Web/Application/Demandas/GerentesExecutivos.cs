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

    /// <summary>Cadastra o gerente com os corredores que ele atende (Cliente, revisão de 06/10/2026).</summary>
    internal async Task CadastrarAsync(Ator admin, string nome, IReadOnlyCollection<Guid> corredores, CancellationToken cancelamento)
    {
        var nomes = await NomesCorredoresAsync(corredores, cancelamento);
        var gerente = GerenteExecutivo.Cadastrar(admin, nome, corredores);
        catalogos.Adicionar(gerente);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(GerenteExecutivo), gerente.Id, "Cadastro", null,
            $"{gerente.Nome}; corredores: {nomes}", null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }

    internal async Task DefinirCorredoresAsync(Ator admin, Guid id, IReadOnlyCollection<Guid> corredores, CancellationToken cancelamento)
    {
        var gerente = await catalogos.GerenteExecutivoAsync(id, cancelamento)
            ?? throw new RegraNegocioException("Gerente executivo não encontrado.");
        var anteriores = await NomesCorredoresAsync([.. gerente.Corredores.Select(c => c.CorredorId)], cancelamento, exigirAtivos: false);
        var novos = await NomesCorredoresAsync(corredores, cancelamento);

        gerente.DefinirCorredores(admin, corredores);
        auditoria.Registrar(LogAuditoria.De(admin, nameof(GerenteExecutivo), gerente.Id, "Corredores", anteriores, novos, null, relogio.AgoraUtc));
        await unidade.SalvarAsync(cancelamento);
    }

    /// <summary>Nomes dos corredores escolhidos; recusa corredor inexistente ou inativo nos corredores novos.</summary>
    private async Task<string> NomesCorredoresAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelamento, bool exigirAtivos = true)
    {
        var corredores = (await catalogos.FormularioAsync(cancelamento)).Corredores;
        if (exigirAtivos && ids.Any(id => corredores.All(c => c.Id != id)))
        {
            throw new RegraNegocioException("Escolha só corredores ativos.");
        }

        return string.Join(", ", corredores.Where(c => ids.Contains(c.Id)).Select(c => $"{c.Nome} ({c.Regiao})"));
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
