using Contratacao.Web.Domain.Auditoria;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Qqp;
using Contratacao.Web.Domain.Usuarios;
using Contratacao.Web.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Web.Infrastructure.Carga;

/// <summary>
/// Carga dos dados que não cabem na migration: catálogo QQP e RACs, lidos dos CSV da pasta dados
/// (a carga lê o arquivo, não valores copiados para o código), e o Admin inicial, lido da configuração.
/// Pode rodar várias vezes: só insere o que ainda não existe.
/// </summary>
internal sealed class CargaInicial(
    ContratacaoDbContext contexto,
    IRelogio relogio,
    IPasswordHasher<Usuario> hasher,
    OpcoesAdminInicial admin,
    string pastaDados)
{
    internal const string ArquivoQqp = "tb_qqp_SESI.csv";
    internal const string ArquivoRacs = "tb_racs.csv";

    private const string SemClassificacao = "-";

    internal async Task ExecutarAsync(CancellationToken cancelamento)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancelamento);

        await CarregarQqpAsync(cancelamento);
        await CarregarRacsAsync(cancelamento);
        await CriarAdminInicialAsync(cancelamento);

        await contexto.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);
    }

    private async Task CarregarQqpAsync(CancellationToken cancelamento)
    {
        if (await contexto.ItensQqp.AnyAsync(cancelamento))
        {
            return;
        }

        // O repositório traz o arquivo com valores alterados; em produção, quem instala o troca pelo real.
        var arquivo = Path.Combine(pastaDados, ArquivoQqp);
        if (!File.Exists(arquivo))
        {
            throw new InvalidOperationException(
                $"Arquivo {ArquivoQqp} não encontrado em {pastaDados}. Copie-o para a pasta dados/ na raiz do projeto.");
        }

        var linhas = LeitorCsv.Ler(arquivo, colunasEsperadas: 8);
        var regioes = await contexto.QqpRegioes.ToDictionaryAsync(r => r.Nome, r => r.Id, cancelamento);

        var funcoes = new Dictionary<string, QqpFuncao>();
        var classificacoes = new Dictionary<string, QqpClassificacao>();
        var niveis = new Dictionary<string, QqpNivel>();
        var cargas = new Dictionary<short, QqpCargaHoraria>();

        foreach (var campos in linhas)
        {
            Obter(funcoes, campos[2].Trim(), nome => new QqpFuncao(nome));
            if (campos[3].Trim() != SemClassificacao)
            {
                Obter(classificacoes, campos[3].Trim(), nome => new QqpClassificacao(nome));
            }

            Obter(niveis, campos[4].Trim(), nome => new QqpNivel(nome, ConversorValores.OrdemNivel(nome)));
            Obter(cargas, short.Parse(campos[5].Trim(), System.Globalization.CultureInfo.InvariantCulture), horas => new QqpCargaHoraria(horas));
        }

        // Ao serem adicionadas, as linhas recebem o Id (Guid sequencial) usado pelos itens abaixo.
        contexto.AddRange(funcoes.Values);
        contexto.AddRange(classificacoes.Values);
        contexto.AddRange(niveis.Values);
        contexto.AddRange(cargas.Values);

        foreach (var campos in linhas)
        {
            var regiao = campos[1].Trim();
            if (!regioes.TryGetValue(regiao, out var regiaoId))
            {
                throw new InvalidDataException($"Região QQP desconhecida: {regiao}");
            }

            // A classificação "-" é de funções que não têm classificação (Cliente).
            var classificacao = campos[3].Trim();
            Guid? classificacaoId = classificacao == SemClassificacao ? null : classificacoes[classificacao].Id;

            // Códigos 466 e 467 repetem a combinação; são importados assim mesmo, com o código como chave.
            contexto.ItensQqp.Add(new ItemQqp(
                codigo: int.Parse(campos[0].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                regiaoId: regiaoId,
                funcaoId: funcoes[campos[2].Trim()].Id,
                classificacaoId: classificacaoId,
                nivelId: niveis[campos[4].Trim()].Id,
                cargaHorariaId: cargas[short.Parse(campos[5].Trim(), System.Globalization.CultureInfo.InvariantCulture)].Id,
                pisoSalarial: ConversorValores.Reais(campos[6]),
                precoUnitario: ConversorValores.Reais(campos[7])));
        }

        contexto.LogsAuditoria.Add(LogAuditoria.DoSistema(
            "ItemQqp", null, "CargaInicial", $"{linhas.Count} itens de {ArquivoQqp}", relogio.AgoraUtc));
    }

    private async Task CarregarRacsAsync(CancellationToken cancelamento)
    {
        if (await contexto.Racs.AnyAsync(cancelamento))
        {
            return;
        }

        var linhas = LeitorCsv.Ler(Path.Combine(pastaDados, ArquivoRacs), colunasEsperadas: 3);

        // Espaços no início dos nomes são removidos (seção 23).
        contexto.Racs.AddRange(linhas.Select(campos => new Rac(campos[1].Trim(), campos[2].Trim())));
        contexto.LogsAuditoria.Add(LogAuditoria.DoSistema(
            "Rac", null, "CargaInicial", $"{linhas.Count} RACs de {ArquivoRacs}", relogio.AgoraUtc));
    }

    private async Task CriarAdminInicialAsync(CancellationToken cancelamento)
    {
        if (await contexto.Usuarios.AnyAsync(u => u.Perfil == Perfil.Admin, cancelamento))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(admin.Nome) || string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrEmpty(admin.Senha))
        {
            throw new InvalidOperationException(
                "Configure o Admin inicial antes de preparar o banco: AdminInicial:Nome, AdminInicial:Email e AdminInicial:Senha " +
                "(dotnet user-secrets set \"AdminInicial:Email\" \"admin@ucl.br\" --project src/Contratacao.Web).");
        }

        var usuario = Usuario.CriarAdminInicial(admin.Nome, admin.Email, relogio.AgoraUtc);
        usuario.DefinirSenhaHash(hasher.HashPassword(usuario, admin.Senha));
        contexto.Usuarios.Add(usuario);

        contexto.LogsAuditoria.Add(LogAuditoria.DoSistema(
            "Usuario", usuario.Id, "AdminInicialCriado", usuario.Email, relogio.AgoraUtc));
    }

    private static void Obter<TChave, TValor>(Dictionary<TChave, TValor> mapa, TChave chave, Func<TChave, TValor> criar)
        where TChave : notnull
    {
        if (!mapa.ContainsKey(chave))
        {
            mapa[chave] = criar(chave);
        }
    }
}
