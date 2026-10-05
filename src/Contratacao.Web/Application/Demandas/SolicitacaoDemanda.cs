using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Application.Demandas;

/// <summary>UC02 — Enviar a demanda: número AAAA-NNNNNN, contrato do corredor, custo e anexos escolhidos no envio.</summary>
internal sealed class EnviarDemanda(
    IDemandas demandas,
    ICatalogosDemanda catalogos,
    IAnexos anexos,
    IArmazenamentoArquivos armazenamento,
    IAuditoria auditoria,
    IUnidadeDeTrabalho unidade,
    IRelogio relogio,
    ICalendarioSla calendario)
{
    internal async Task<Demanda> ExecutarAsync(
        Ator solicitante,
        DadosSolicitacao dados,
        ArquivoRecebido? deAcordoVp2,
        IReadOnlyList<ArquivoRecebido> outrosAnexos,
        CancellationToken cancelamento)
    {
        if (!solicitante.Eh(Perfil.Solicitante))
        {
            throw new RegraNegocioException("Só um Solicitante ativo envia demanda.");
        }

        var (conferidos, referencias) = await Formulario.ConferirAsync(catalogos, dados, cancelamento);

        var arquivos = new List<(ArquivoRecebido Arquivo, ArquivoConferido Conferido, CategoriaAnexo Categoria)>();
        if (deAcordoVp2 is not null)
        {
            arquivos.Add((deAcordoVp2, await Formulario.ConferirArquivoAsync(deAcordoVp2, cancelamento), CategoriaAnexo.DeAcordoVP2));
        }

        foreach (var arquivo in outrosAnexos)
        {
            arquivos.Add((arquivo, await Formulario.ConferirArquivoAsync(arquivo, cancelamento), CategoriaAnexo.Geral));
        }

        // O número só é reservado se a demanda for aceita: a transação desfaz a sequência em caso de erro.
        return await unidade.EmTransacaoAsync(async () =>
        {
            var agora = relogio.AgoraUtc;

            // SUPOSIÇÃO (S3): o sequencial reinicia a cada ano; o ano é o do envio, no horário de Brasília.
            var ano = calendario.DataLocal(agora).Year;
            var numero = NumeroDemanda.Formatar(ano, await demandas.ProximoSequencialAsync(ano, cancelamento));

            var demanda = Demanda.Enviar(numero, solicitante, conferidos, referencias, deAcordoVp2 is not null, relogio);
            demandas.Adicionar(demanda);

            foreach (var (arquivo, conferido, categoria) in arquivos)
            {
                string identificador;
                await using (var conteudo = arquivo.Abrir())
                {
                    identificador = await armazenamento.GuardarAsync(conteudo, conferido.Extensao, cancelamento);
                }

                var anexo = Anexo.NoEnvio(demanda, solicitante, conferido, identificador, categoria, agora);
                anexos.Adicionar(anexo);
                auditoria.Registrar(LogAuditoria.De(solicitante, nameof(Anexo), anexo.Id, "Upload", null,
                    $"{demanda.Numero}; {categoria}; {conferido.NomeArquivo}; {conferido.Tamanho} bytes", null, agora));
            }

            await unidade.SalvarAsync(cancelamento);
            return demanda;
        }, cancelamento);
    }
}

/// <summary>UC06 — Corrigir a demanda devolvida, pelo Solicitante que a criou.</summary>
internal sealed class CorrigirDemanda(IDemandas demandas, ICatalogosDemanda catalogos, IUnidadeDeTrabalho unidade, IRelogio relogio)
{
    internal async Task ExecutarAsync(Ator solicitante, Guid demandaId, DadosSolicitacao dados, CancellationToken cancelamento)
    {
        var demanda = await demandas.ObterAsync(demandaId, cancelamento)
            ?? throw new RegraNegocioException("Demanda não encontrada.");
        var (conferidos, referencias) = await Formulario.ConferirAsync(catalogos, dados, cancelamento);

        demanda.Corrigir(solicitante, conferidos, referencias, relogio);
        await unidade.SalvarAsync(cancelamento);
    }
}

/// <summary>Opções das listas do formulário e itens do QQP para as listas em cascata.</summary>
internal sealed class ConsultarFormulario(ICatalogosDemanda catalogos)
{
    internal Task<CatalogosFormulario> CatalogosAsync(CancellationToken cancelamento) => catalogos.FormularioAsync(cancelamento);

    internal Task<IReadOnlyList<OpcaoQqp>> ItensQqpAsync(CancellationToken cancelamento) => catalogos.ItensQqpAsync(cancelamento);
}

/// <summary>Custo mostrado enquanto o Solicitante preenche (RN12); o custo gravado é recalculado no envio.</summary>
internal sealed record PreviaCusto(decimal? PisoSalarial, decimal? PrecoUnitario, decimal EquipamentosPorPessoa, decimal? Total);

internal sealed class CalcularPreviaCusto(ICatalogosDemanda catalogos)
{
    internal async Task<PreviaCusto> ExecutarAsync(
        Guid? itemQqpId, int? quantidade, bool notebook, bool segundaTela, bool celular, CancellationToken cancelamento)
    {
        var valores = await catalogos.EquipamentosAsync(cancelamento);
        var equipamentos = CustoDemanda.EquipamentosPorPessoa(notebook, segundaTela, celular, valores);
        var preco = itemQqpId is { } id && id != Guid.Empty ? await catalogos.PrecoQqpAsync(id, cancelamento) : null;

        decimal? total = preco is not null && quantidade is > 0 and <= short.MaxValue
            ? CustoDemanda.Total(quantidade.Value, preco.PrecoUnitario, equipamentos)
            : null;
        return new PreviaCusto(preco?.PisoSalarial, preco?.PrecoUnitario, equipamentos, total);
    }
}

/// <summary>Conferências comuns ao envio e à correção.</summary>
internal static class Formulario
{
    /// <summary>
    /// Confere os campos (domínio) e os catálogos escolhidos, e lê os valores atuais de corredor, contrato,
    /// preço QQP e equipamentos. Recusa com todas as mensagens de uma vez.
    /// </summary>
    internal static async Task<(DadosSolicitacao Dados, ReferenciasSolicitacao Referencias)> ConferirAsync(
        ICatalogosDemanda catalogos, DadosSolicitacao dados, CancellationToken cancelamento)
    {
        var conferidos = dados.Validar();

        var erros = await catalogos.ConferirEscolhasAsync(conferidos, cancelamento);
        if (erros.Count > 0)
        {
            throw new RegraNegocioException(string.Join(" ", erros));
        }

        var preco = await catalogos.PrecoQqpAsync(conferidos.ItemQqpId, cancelamento)
            ?? throw new RegraNegocioException("O item QQP escolhido não existe ou está inativo.");
        var equipamentos = await catalogos.EquipamentosAsync(cancelamento);
        var referencias = await catalogos.ReferenciasAsync(conferidos.CorredorId, preco, equipamentos, cancelamento)
            ?? throw new RegraNegocioException("O corredor escolhido não existe.");

        return (conferidos, referencias);
    }

    /// <summary>Lê o começo do arquivo e confere tamanho, extensão e conteúdo (seção 18).</summary>
    internal static async Task<ArquivoConferido> ConferirArquivoAsync(ArquivoRecebido arquivo, CancellationToken cancelamento)
    {
        var inicio = new byte[RegraArquivo.BytesParaConferir];
        int lidos;
        await using (var conteudo = arquivo.Abrir())
        {
            lidos = await conteudo.ReadAtLeastAsync(inicio, inicio.Length, throwOnEndOfStream: false, cancelamento);
        }

        return RegraArquivo.Conferir(arquivo.Nome, arquivo.Tamanho, inicio[..lidos]);
    }
}
