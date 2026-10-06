using System.Net;
using System.Text.RegularExpressions;
using Contratacao.Tests.Integracao.Apoio;
using Contratacao.Tests.Unitarios.Anexos;
using Contratacao.Web.Application.Usuarios;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Tests.Integracao;

/// <summary>Telas da demanda pela aplicação web: envio com anexos, validação pelo Gestor e pelo SESI, correção e acesso.</summary>
// public: o xUnit exige classes de teste públicas (regra xUnit1000).
public sealed partial class TelasDemandaTests(BancoFixture banco) : IClassFixture<BancoFixture>
{
    private const string Senha = "senha-segura-1";
    private const string NovaSenha = "senha-propria-2";

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Solicitante_envia_gestor_devolve_solicitante_corrige_gestor_aprova_e_sesi_aceita()
    {
        var (gestorEmail, sesiEmail) = await CadastrarEquipeAsync(IdsFixos.ContratoNorte);

        await using var solicitante = new Navegador(banco);
        await CadastrarSolicitanteAsync(solicitante);
        var formulario = await solicitante.HtmlAsync("/Demandas/Nova");
        Assert.Contains("id=\"itens-qqp\"", formulario, StringComparison.Ordinal);
        Assert.Contains("Gerência Fictícia de Testes", formulario, StringComparison.Ordinal);

        var campos = await CamposAsync();
        using var envio = await solicitante.EnviarComArquivosAsync("/Demandas/Nova", campos,
            ("DeAcordoVp2", "de-acordo.pdf", RegraArquivoTests.Pdf), ("OutrosAnexos", "foto.png", RegraArquivoTests.Png));
        Assert.Equal(HttpStatusCode.Redirect, envio.StatusCode);
        var caminho = envio.Headers.Location!.OriginalString;
        Assert.Matches(CaminhoDetalhe(), caminho);

        var detalhe = await solicitante.HtmlAsync(caminho);
        Assert.Matches(NumeroNaPagina(), detalhe);
        Assert.Contains("localStorage.removeItem", detalhe, StringComparison.Ordinal); // rascunho apagado após o envio
        Assert.Contains("de-acordo.pdf", detalhe, StringComparison.Ordinal);
        Assert.DoesNotContain("handler=Aprovar", detalhe, StringComparison.Ordinal);

        await using var gestor = new Navegador(banco);
        await gestor.EntrarPelaPrimeiraVezAsync(gestorEmail, Senha, NovaSenha);
        var paraGestor = await gestor.HtmlAsync(caminho);
        Assert.Contains("handler=Aprovar", paraGestor, StringComparison.Ordinal);
        var linkAnexo = AnexoNaPagina().Match(paraGestor);
        Assert.True(linkAnexo.Success);
        using var anexo = await gestor.AbrirAsync(linkAnexo.Value.Replace("&amp;", "&", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.OK, anexo.StatusCode);
        Assert.Equal("application/pdf", anexo.Content.Headers.ContentType?.MediaType);

        using var devolucao = await gestor.EnviarDeAsync(caminho, caminho + "?handler=DevolverGestor", ("Motivo", "Informe a formação exigida."));
        Assert.Equal(HttpStatusCode.Redirect, devolucao.StatusCode);
        Assert.Contains("Informe a formação exigida.", await gestor.HtmlAsync(caminho), StringComparison.Ordinal);

        var id = Guid.Parse(caminho.Split('/').Last());
        var correcao = await solicitante.HtmlAsync($"/Demandas/Corrigir/{id}");
        Assert.Contains("Informe a formação exigida.", correcao, StringComparison.Ordinal);
        using var corrigida = await solicitante.EnviarAsync($"/Demandas/Corrigir/{id}",
            [.. campos.Where(c => c.Campo != "Entrada.Formacao"), ("Entrada.Formacao", "Técnico em mecânica")]);
        Assert.Equal(HttpStatusCode.Redirect, corrigida.StatusCode);
        Assert.Contains("Técnico em mecânica", await solicitante.HtmlAsync(caminho), StringComparison.Ordinal);

        using var aprovacao = await gestor.EnviarDeAsync(caminho, caminho + "?handler=Aprovar");
        Assert.Equal(HttpStatusCode.Redirect, aprovacao.StatusCode);

        await using var sesi = new Navegador(banco);
        await sesi.EntrarPelaPrimeiraVezAsync(sesiEmail, Senha, NovaSenha);
        Assert.Contains(caminho, await sesi.HtmlAsync("/Demandas"), StringComparison.Ordinal);
        using var aceite = await sesi.EnviarDeAsync(caminho, caminho + "?handler=Aceitar");
        Assert.Equal(HttpStatusCode.Redirect, aceite.StatusCode);

        await using var contexto = banco.NovoContexto();
        var demanda = await BancoFixture.CarregarDemandaAsync(contexto, id);
        Assert.Equal((Etapa.Recrutamento, StatusDemanda.EmAndamento), (demanda.Etapa, demanda.Status));
        Assert.Equal("Técnico em mecânica", demanda.Formacao);
        Assert.All(demanda.Historico, h => Assert.Equal(Navegador.Ip, h.EnderecoIp));
        Assert.Single(demanda.Alteracoes, a => a.Campo == "Formacao");
    }

    [Fact]
    public async Task Envio_sem_vp2_volta_ao_formulario_com_a_mensagem()
    {
        await using var solicitante = new Navegador(banco);
        await CadastrarSolicitanteAsync(solicitante);

        using var envio = await solicitante.EnviarComArquivosAsync("/Demandas/Nova", await CamposAsync());
        var html = await envio.Content.ReadAsStringAsync(Cancelamento);

        Assert.Equal(HttpStatusCode.OK, envio.StatusCode);
        Assert.Contains("Anexe o De acordo VP-2", html, StringComparison.Ordinal);
        Assert.Contains("data-restaurar=\"nao\"", html, StringComparison.Ordinal); // não sobrescreve com o rascunho
    }

    [Fact]
    public async Task Sesi_de_outro_contrato_nao_abre_a_demanda_e_so_o_solicitante_abre_o_formulario()
    {
        var (gestorEmail, _) = await CadastrarEquipeAsync(IdsFixos.ContratoNorte);
        var (_, sesiSudesteEmail) = await CadastrarEquipeAsync(IdsFixos.ContratoSudeste);

        await using var solicitante = new Navegador(banco);
        await CadastrarSolicitanteAsync(solicitante);
        using var envio = await solicitante.EnviarComArquivosAsync("/Demandas/Nova", await CamposAsync(), ("DeAcordoVp2", "vp2.pdf", RegraArquivoTests.Pdf));
        var caminho = envio.Headers.Location!.OriginalString;

        await using var sesi = new Navegador(banco);
        await sesi.EntrarPelaPrimeiraVezAsync(sesiSudesteEmail, Senha, NovaSenha);
        using var negado = await sesi.AbrirAsync(caminho);
        Assert.Equal(HttpStatusCode.NotFound, negado.StatusCode);
        Assert.DoesNotContain(caminho, await sesi.HtmlAsync("/Demandas"), StringComparison.Ordinal);

        await using var gestor = new Navegador(banco);
        await gestor.EntrarPelaPrimeiraVezAsync(gestorEmail, Senha, NovaSenha);
        foreach (var pagina in new[] { "/Demandas/Nova", "/Demandas/Custo" })
        {
            using var proibida = await gestor.AbrirAsync(pagina);
            Assert.StartsWith("http://localhost/AcessoNegado", proibida.Headers.Location?.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Previa_do_custo_e_calculada_pelo_servidor()
    {
        await using var solicitante = new Navegador(banco);
        await CadastrarSolicitanteAsync(solicitante);
        var dados = await banco.DadosAsync();
        decimal preco;
        await using (var contexto = banco.NovoContexto())
        {
            preco = (await contexto.ItensQqp.SingleAsync(i => i.Id == dados.ItemQqpId, Cancelamento)).PrecoUnitario;
        }

        var html = await solicitante.HtmlAsync(
            $"/Demandas/Custo?Entrada.ItemQqpId={dados.ItemQqpId}&Entrada.QuantidadeSolicitada=3&Entrada.Notebook=true&Entrada.Celular=true");

        var esperado = (3 * (preco + 444.35m + 118.64m)).ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        Assert.Contains(esperado, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_cadastra_e_desativa_gerente_executivo()
    {
        await using var admin = new Navegador(banco);
        await admin.EntrarAsync(BancoFixture.Admin.Email!.Trim().ToLowerInvariant(), BancoFixture.Admin.Senha!);
        var nome = $"Gerência {Guid.NewGuid():N}";

        using var cadastro = await admin.EnviarAsync("/Admin/GerentesExecutivos", ("Nome", nome));
        Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);
        Assert.Contains(nome, await admin.HtmlAsync("/Admin/GerentesExecutivos"), StringComparison.Ordinal);

        await using var contexto = banco.NovoContexto();
        var gerente = await contexto.GerentesExecutivos.SingleAsync(g => g.Nome == nome, Cancelamento);
        using var desativacao = await admin.EnviarDeAsync("/Admin/GerentesExecutivos", "/Admin/GerentesExecutivos?handler=Situacao", ("id", gerente.Id.ToString()), ("ativo", "false"));
        Assert.Equal(HttpStatusCode.Redirect, desativacao.StatusCode);

        await using var releitura = banco.NovoContexto();
        Assert.False((await releitura.GerentesExecutivos.SingleAsync(g => g.Id == gerente.Id, Cancelamento)).Ativo);
        Assert.True(await releitura.LogsAuditoria.AnyAsync(l => l.EntidadeId == gerente.Id && l.Acao == "Desativacao", Cancelamento));
    }

    [Fact]
    public async Task Admin_cadastra_os_no_contrato_e_o_formulario_a_oferece_so_para_esse_contrato()
    {
        await using var admin = new Navegador(banco);
        await admin.EntrarAsync(BancoFixture.Admin.Email!.Trim().ToLowerInvariant(), BancoFixture.Admin.Senha!);
        var numero = Random.Shared.Next(100, 99999).ToString(System.Globalization.CultureInfo.InvariantCulture);

        using var cadastro = await admin.EnviarAsync("/Admin/OrdensServico", ("ContratoId", IdsFixos.ContratoSudeste.ToString()), ("Numero", numero));
        Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);

        using var repetida = await admin.EnviarAsync("/Admin/OrdensServico", ("ContratoId", IdsFixos.ContratoSudeste.ToString()), ("Numero", numero));
        Assert.Contains($"A OS {numero} já existe no contrato 5900118506.", await repetida.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);

        await using var contexto = banco.NovoContexto();
        var os = await contexto.OrdensServico.SingleAsync(o => o.Numero == numero, Cancelamento);
        Assert.True(await contexto.LogsAuditoria.AnyAsync(l => l.EntidadeId == os.Id && l.Acao == "Cadastro", Cancelamento));

        await using var solicitante = new Navegador(banco);
        await CadastrarSolicitanteAsync(solicitante);
        var formulario = await solicitante.HtmlAsync("/Demandas/Nova");
        Assert.Contains($"<option value=\"{os.Id}\" data-contrato-id=\"{IdsFixos.ContratoSudeste}\"", formulario, StringComparison.Ordinal);
    }

    /// <summary>Gestor do contrato (cadastrado pelo Admin) e um Funcionário SESI da equipe dele, ambos com senha inicial.</summary>
    private async Task<(string Gestor, string Sesi)> CadastrarEquipeAsync(Guid contrato)
    {
        var admin = await banco.AdminAsync();
        var gestorEmail = $"gestor.{Guid.NewGuid():N}@ucl.br";
        var gestor = await banco.ExecutarAsync<CadastrarGestor, Usuario>(c => c.ExecutarAsync(admin, "Gestor de Testes", gestorEmail, Senha, [contrato], Cancelamento));

        var sesiEmail = $"sesi.{Guid.NewGuid():N}@ucl.br";
        var ator = await banco.AtorAsync(gestor.Id);
        await banco.ExecutarAsync<CadastrarFuncionarioSesi, Usuario>(c => c.ExecutarAsync(ator, "SESI de Testes", sesiEmail, Senha, contrato, Cancelamento));
        return (gestorEmail, sesiEmail);
    }

    private static async Task CadastrarSolicitanteAsync(Navegador navegador)
    {
        using var cadastro = await navegador.EnviarAsync("/Cadastro",
            ("Entrada.Nome", "Solicitante de Testes"), ("Entrada.Email", $"sol.{Guid.NewGuid():N}@ucl.br"),
            ("Entrada.Senha", Senha), ("Entrada.ConfirmacaoSenha", Senha));
        Assert.Equal(HttpStatusCode.Redirect, cadastro.StatusCode);
    }

    /// <summary>Os campos do formulário, como o navegador os envia.</summary>
    private async Task<List<(string Campo, string Valor)>> CamposAsync()
    {
        var dados = await banco.DadosAsync();
        return
        [
            ("Entrada.TipoDemandaId", dados.TipoDemandaId.ToString()),
            ("Entrada.GerenteExecutivoId", dados.GerenteExecutivoId.ToString()),
            ("Entrada.LocalidadeVaga", dados.LocalidadeVaga),
            ("Entrada.CorredorId", dados.CorredorId.ToString()),
            ("Entrada.ModeloTrabalhoId", dados.ModeloTrabalhoId.ToString()),
            ("Entrada.QuantidadeSolicitada", "2"),
            ("Entrada.DescricaoAtividades", dados.DescricaoAtividades),
            ("Entrada.ItemQqpId", dados.ItemQqpId.ToString()),
            ("Entrada.Notebook", "true"),
            ("Entrada.Racs", dados.Racs.Single().ToString()),
            ("Entrada.OrdemServicoId", dados.OrdemServicoId.ToString()),
            ("Entrada.ColetorCusto", dados.ColetorCusto),
            ("Entrada.ResponsavelEfetivoNome", dados.ResponsavelEfetivoNome),
            ("Entrada.ResponsavelEfetivoEmail", dados.ResponsavelEfetivoEmail),
            ("Entrada.FiscalEfetivoNome", dados.FiscalEfetivoNome),
            ("Entrada.FiscalEfetivoEmail", dados.FiscalEfetivoEmail),
        ];
    }

    [GeneratedRegex(@"^/Demandas/Detalhe/[0-9a-f-]{36}$")]
    private static partial Regex CaminhoDetalhe();

    [GeneratedRegex(@"\d{4}-\d{6}")]
    private static partial Regex NumeroNaPagina();

    [GeneratedRegex(@"/Demandas/Detalhe/[0-9a-f-]{36}\?handler=Anexo&(amp;)?anexoId=[0-9a-f-]{36}")]
    private static partial Regex AnexoNaPagina();
}
