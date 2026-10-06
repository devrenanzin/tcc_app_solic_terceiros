using System.Globalization;
using Contratacao.Web.Application;
using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Domain.Anexos;
using Contratacao.Web.Domain.Catalogos;
using Contratacao.Web.Domain.Comum;
using Contratacao.Web.Domain.Contratos;
using Contratacao.Web.Domain.Demandas;
using Contratacao.Web.Domain.Parametros;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Web.Infrastructure.Persistencia;

internal sealed class RepositorioDemandas(ContratacaoDbContext contexto) : IDemandas
{
    public Task<Demanda?> ObterAsync(Guid id, CancellationToken cancelamento)
        => contexto.Demandas
            .Include(d => d.Etapas)
            .Include(d => d.Correcoes)
            .Include(d => d.Historico)
            .Include(d => d.Vaga)
            .Include(d => d.Racs)
            .Include(d => d.Alteracoes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(d => d.Id == id, cancelamento);

    // Ao ser adicionada, a demanda recebe o Id (Guid sequencial) usado em seguida nos anexos.
    public void Adicionar(Demanda demanda) => contexto.Demandas.Add(demanda);

    public async Task<IReadOnlyList<ResumoDemanda>> ListarAsync(FiltroVisibilidade filtro, CancellationToken cancelamento)
    {
        if (filtro.Nenhuma)
        {
            return [];
        }

        var consulta = contexto.Demandas.AsNoTracking();
        if (filtro.SolicitanteId is { } solicitante)
        {
            consulta = consulta.Where(d => d.UsuarioSolicitanteId == solicitante);
        }

        if (filtro.Contratos is { } contratos)
        {
            var lista = contratos.ToList();
            consulta = consulta.Where(d => lista.Contains(d.ContratoId));
        }

        var linhas = await (
            from d in consulta
            join u in contexto.Usuarios on d.UsuarioSolicitanteId equals u.Id
            join c in contexto.Contratos on d.ContratoId equals c.Id
            join i in contexto.ItensQqp on d.ItemQqpId equals i.Id
            join f in contexto.QqpFuncoes on i.FuncaoId equals f.Id
            orderby d.DataEnvio descending
            select new
            {
                d.Id, d.Numero, d.UsuarioSolicitanteId, Solicitante = u.Nome, Contrato = c.Numero, d.Etapa, d.Status,
                d.DataEnvio, d.LocalidadeVaga, Funcao = f.Nome, d.QuantidadeSolicitada, d.CustoTotal, d.Sla, d.DataFinalizacao,
            }).ToListAsync(cancelamento);

        return [.. linhas.Select(l => new ResumoDemanda(
            l.Id, l.Numero, l.UsuarioSolicitanteId, l.Solicitante, l.Contrato, l.Etapa, l.Status, l.DataEnvio,
            l.LocalidadeVaga, l.Funcao, l.QuantidadeSolicitada, l.CustoTotal, l.Sla, l.DataFinalizacao))];
    }

    public async Task<int> ProximoSequencialAsync(int ano, CancellationToken cancelamento)
    {
        // MERGE com HOLDLOCK: duas transações simultâneas no mesmo ano esperam uma pela outra, e o número
        // não se repete (critério de aceite 7). A linha do ano nasce no primeiro envio.
        var anoCurto = (short)ano;
        var numeros = await contexto.Database.SqlQuery<int>($"""
            MERGE SequenciaNumeroDemanda WITH (HOLDLOCK) AS s
            USING (SELECT {anoCurto} AS Ano) AS n ON s.Ano = n.Ano
            WHEN MATCHED THEN UPDATE SET UltimoNumero = s.UltimoNumero + 1
            WHEN NOT MATCHED THEN INSERT (Ano, UltimoNumero) VALUES (n.Ano, 1)
            OUTPUT inserted.UltimoNumero AS Value;
            """).ToListAsync(cancelamento);

        return numeros.Single();
    }
}

internal sealed class CatalogosDemanda(ContratacaoDbContext contexto) : ICatalogosDemanda
{
    public async Task<CatalogosFormulario> FormularioAsync(CancellationToken cancelamento)
    {
        var tipos = await contexto.TiposDemanda.AsNoTracking().Where(t => t.Ativo).OrderBy(t => t.Nome)
            .Select(t => new Opcao(t.Id, t.Nome)).ToListAsync(cancelamento);
        var gerentes = (await contexto.GerentesExecutivos.AsNoTracking().Include(g => g.Corredores).Where(g => g.Ativo).OrderBy(g => g.Nome)
            .ToListAsync(cancelamento))
            .Select(g => new OpcaoGerente(g.Id, g.Nome, [.. g.Corredores.Select(c => c.CorredorId)])).ToList();
        var corredores = await (
            from c in contexto.Corredores.AsNoTracking()
            join r in contexto.QqpRegioes on c.RegiaoId equals r.Id
            join ct in contexto.Contratos on c.ContratoId equals ct.Id
            where c.Ativo && ct.Ativo
            orderby r.Nome, c.Nome
            select new OpcaoCorredor(c.Id, c.Nome, r.Nome, ct.Id, ct.Numero)).ToListAsync(cancelamento);
        var modelos = await contexto.ModelosTrabalho.AsNoTracking().OrderBy(m => m.Nome)
            .Select(m => new Opcao(m.Id, m.Nome)).ToListAsync(cancelamento);
        var racs = await contexto.Racs.AsNoTracking().OrderBy(r => r.Codigo)
            .Select(r => new OpcaoRac(r.Id, r.Codigo, r.Nome)).ToListAsync(cancelamento);

        var ordens = (await OrdensServicoAsync(cancelamento)).Where(o => o.Ativo).ToList();

        return new CatalogosFormulario(tipos, gerentes, corredores, modelos, racs, ordens, await EquipamentosAsync(cancelamento));
    }

    public async Task<IReadOnlyList<OpcaoQqp>> ItensQqpAsync(CancellationToken cancelamento)
        => await (
            from i in contexto.ItensQqp.AsNoTracking()
            join r in contexto.QqpRegioes on i.RegiaoId equals r.Id
            join f in contexto.QqpFuncoes on i.FuncaoId equals f.Id
            join c in contexto.QqpClassificacoes on i.ClassificacaoId equals c.Id into classificacoes
            from c in classificacoes.DefaultIfEmpty()
            join n in contexto.QqpNiveis on i.NivelId equals n.Id
            join h in contexto.QqpCargasHorarias on i.CargaHorariaId equals h.Id
            where i.Ativo
            orderby i.Codigo
            select new OpcaoQqp(i.Id, i.Codigo, r.Nome, f.Nome, c != null ? c.Nome : null, n.Nome, n.Ordem, h.HorasSemanais,
                i.PisoSalarial, i.PrecoUnitario)).ToListAsync(cancelamento);

    public Task<PrecoQqp?> PrecoQqpAsync(Guid itemQqpId, CancellationToken cancelamento)
        => contexto.ItensQqp.AsNoTracking()
            .Where(i => i.Id == itemQqpId && i.Ativo)
            .Select(i => new PrecoQqp(i.Id, i.PisoSalarial, i.PrecoUnitario))
            .SingleOrDefaultAsync(cancelamento);

    public async Task<ValoresEquipamentos> EquipamentosAsync(CancellationToken cancelamento)
    {
        var valores = await contexto.ItensEquipamento.AsNoTracking().ToDictionaryAsync(i => i.Id, i => i.Valor, cancelamento);
        return new ValoresEquipamentos(
            valores[IdsFixos.EquipamentoNotebook], valores[IdsFixos.EquipamentoSegundaTela], valores[IdsFixos.EquipamentoCelular]);
    }

    public async Task<ReferenciasSolicitacao?> ReferenciasAsync(
        DadosSolicitacao dados, PrecoQqp qqp, ValoresEquipamentos equipamentos, CancellationToken cancelamento)
    {
        var corredor = await contexto.Corredores.AsNoTracking().SingleOrDefaultAsync(c => c.Id == dados.CorredorId, cancelamento);
        var os = await contexto.OrdensServico.AsNoTracking().SingleOrDefaultAsync(o => o.Id == dados.OrdemServicoId, cancelamento);
        var gerente = await contexto.GerentesExecutivos.AsNoTracking().Include(g => g.Corredores)
            .SingleOrDefaultAsync(g => g.Id == dados.GerenteExecutivoId, cancelamento);
        if (corredor is null || os is null || gerente is null)
        {
            return null;
        }

        var contrato = await contexto.Contratos.AsNoTracking().SingleAsync(c => c.Id == corredor.ContratoId, cancelamento);
        return new ReferenciasSolicitacao(corredor, contrato, os, gerente, qqp, equipamentos);
    }

    public async Task<IReadOnlyList<string>> ConferirEscolhasAsync(DadosSolicitacao dados, CancellationToken cancelamento)
    {
        var erros = new List<string>();
        if (!await contexto.TiposDemanda.AnyAsync(t => t.Id == dados.TipoDemandaId && t.Ativo, cancelamento))
        {
            erros.Add("Escolha um tipo de demanda ativo.");
        }

        if (!await contexto.ModelosTrabalho.AnyAsync(m => m.Id == dados.ModeloTrabalhoId, cancelamento))
        {
            erros.Add("Escolha o modelo de trabalho.");
        }

        var racs = dados.Racs.ToList();
        if (racs.Count > 0 && await contexto.Racs.CountAsync(r => racs.Contains(r.Id), cancelamento) != racs.Count)
        {
            erros.Add("Uma das RACs escolhidas não existe.");
        }

        return erros;
    }

    public async Task<DescricoesDemanda> DescreverAsync(Demanda demanda, CancellationToken cancelamento)
    {
        var alteracoes = demanda.Alteracoes;
        IEnumerable<Guid> IdsAlterados(string campo) => alteracoes
            .Where(a => a.Campo == campo)
            .SelectMany(a => new[] { a.ValorAnterior, a.NovoValor })
            .Select(v => Guid.TryParse(v, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty);

        var usuarios = new[] { demanda.UsuarioSolicitanteId }
            .Concat(new[] { demanda.GestorId, demanda.ResponsavelSesiId }.OfType<Guid>())
            .Concat(demanda.Historico.Select(h => h.UsuarioId))
            .Concat(demanda.Correcoes.Select(c => c.SolicitadoPorId))
            .Concat(alteracoes.Select(a => a.UsuarioId))
            .Distinct().ToList();
        var itens = IdsAlterados(nameof(DadosSolicitacao.ItemQqpId)).Append(demanda.ItemQqpId).Distinct().ToList();

        var descricoesItens = await (
            from i in contexto.ItensQqp.AsNoTracking()
            join r in contexto.QqpRegioes on i.RegiaoId equals r.Id
            join f in contexto.QqpFuncoes on i.FuncaoId equals f.Id
            join c in contexto.QqpClassificacoes on i.ClassificacaoId equals c.Id into classificacoes
            from c in classificacoes.DefaultIfEmpty()
            join n in contexto.QqpNiveis on i.NivelId equals n.Id
            join h in contexto.QqpCargasHorarias on i.CargaHorariaId equals h.Id
            where itens.Contains(i.Id)
            select new { i.Id, i.Codigo, Regiao = r.Nome, Funcao = f.Nome, Classificacao = c != null ? c.Nome : null, Nivel = n.Nome, h.HorasSemanais })
            .ToListAsync(cancelamento);

        return new DescricoesDemanda(
            await contexto.Usuarios.AsNoTracking().Where(u => usuarios.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, cancelamento),
            await contexto.Contratos.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Numero, cancelamento),
            await contexto.Contratadas.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.RazaoSocial, cancelamento),
            await contexto.TiposDemanda.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Nome, cancelamento),
            await contexto.GerentesExecutivos.AsNoTracking().ToDictionaryAsync(g => g.Id, g => g.Nome, cancelamento),
            await (from c in contexto.Corredores.AsNoTracking()
                   join r in contexto.QqpRegioes on c.RegiaoId equals r.Id
                   select new { c.Id, Nome = c.Nome + " (" + r.Nome + ")" }).ToDictionaryAsync(c => c.Id, c => c.Nome, cancelamento),
            await contexto.ModelosTrabalho.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m.Nome, cancelamento),
            descricoesItens.ToDictionary(i => i.Id, i => string.Join(" · ",
                $"Código {i.Codigo.ToString(CultureInfo.InvariantCulture)}",
                string.Join(" ", new[] { i.Funcao, i.Classificacao, i.Nivel }.OfType<string>()),
                $"{i.HorasSemanais.ToString(CultureInfo.InvariantCulture)} h",
                i.Regiao)),
            await contexto.Racs.AsNoTracking().ToDictionaryAsync(r => r.Id, r => $"{r.Codigo} — {r.Nome}", cancelamento),
            await contexto.OrdensServico.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Numero, cancelamento));
    }

    public async Task<IReadOnlyList<GerenteExecutivo>> GerentesExecutivosAsync(CancellationToken cancelamento)
        => await contexto.GerentesExecutivos.AsNoTracking().Include(g => g.Corredores).OrderBy(g => g.Nome).ToListAsync(cancelamento);

    public Task<GerenteExecutivo?> GerenteExecutivoAsync(Guid id, CancellationToken cancelamento)
        => contexto.GerentesExecutivos.Include(g => g.Corredores).SingleOrDefaultAsync(g => g.Id == id, cancelamento);

    public void Adicionar(GerenteExecutivo gerente) => contexto.GerentesExecutivos.Add(gerente);

    public async Task<IReadOnlyList<OpcaoOs>> OrdensServicoAsync(CancellationToken cancelamento)
        => await (
            from o in contexto.OrdensServico.AsNoTracking()
            join c in contexto.Contratos on o.ContratoId equals c.Id
            orderby c.Numero, o.Numero
            select new OpcaoOs(o.Id, o.Numero, c.Id, c.Numero, o.Ativo)).ToListAsync(cancelamento);

    public Task<OrdemServico?> OrdemServicoAsync(Guid id, CancellationToken cancelamento)
        => contexto.OrdensServico.SingleOrDefaultAsync(o => o.Id == id, cancelamento);

    public Task<bool> OrdemServicoExisteAsync(Guid contratoId, string numero, CancellationToken cancelamento)
        => contexto.OrdensServico.AnyAsync(o => o.ContratoId == contratoId && o.Numero == numero, cancelamento);

    public void Adicionar(OrdemServico ordemServico) => contexto.OrdensServico.Add(ordemServico);
}

internal sealed class RepositorioAnexos(ContratacaoDbContext contexto) : IAnexos
{
    public void Adicionar(Anexo anexo) => contexto.Anexos.Add(anexo);

    public async Task<IReadOnlyList<Anexo>> DaDemandaAsync(Guid demandaId, CancellationToken cancelamento)
        => await contexto.Anexos.AsNoTracking().Where(a => a.DemandaId == demandaId).OrderBy(a => a.DataUpload).ThenBy(a => a.NomeArquivo)
            .ToListAsync(cancelamento);

    public Task<Anexo?> ObterAsync(Guid id, CancellationToken cancelamento)
        => contexto.Anexos.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancelamento);
}

internal sealed class Parametros(ContratacaoDbContext contexto) : IParametros
{
    public async Task<int> PrazoSlaDiasAsync(CancellationToken cancelamento)
        => (await contexto.Parametros.AsNoTracking().SingleAsync(p => p.Chave == ParametroSistema.PrazoSlaDias, cancelamento)).ValorInteiro();
}
