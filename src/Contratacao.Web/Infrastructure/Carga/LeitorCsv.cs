using System.Text;

namespace Contratacao.Web.Infrastructure.Carga;

/// <summary>Lê os CSV da pasta dados: UTF-8, separados por vírgula, campos entre aspas e cabeçalho na 1ª linha.</summary>
internal static class LeitorCsv
{
    internal static IReadOnlyList<string[]> Ler(string caminho, int colunasEsperadas)
    {
        if (!File.Exists(caminho))
        {
            throw new FileNotFoundException($"Arquivo de dados não encontrado: {caminho}", caminho);
        }

        var linhas = File.ReadAllLines(caminho, Encoding.UTF8)
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select((linha, indice) =>
            {
                var campos = Separar(linha);
                if (campos.Length != colunasEsperadas)
                {
                    throw new InvalidDataException(
                        $"{Path.GetFileName(caminho)}, linha {indice + 2}: esperadas {colunasEsperadas} colunas, encontradas {campos.Length}.");
                }

                return campos;
            });

        return [.. linhas];
    }

    internal static string[] Separar(string linha)
    {
        var campos = new List<string>();
        var atual = new StringBuilder();
        var entreAspas = false;

        for (var i = 0; i < linha.Length; i++)
        {
            var c = linha[i];
            if (entreAspas)
            {
                if (c == '"' && i + 1 < linha.Length && linha[i + 1] == '"')
                {
                    atual.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    entreAspas = false;
                }
                else
                {
                    atual.Append(c);
                }
            }
            else if (c == '"')
            {
                entreAspas = true;
            }
            else if (c == ',')
            {
                campos.Add(atual.ToString());
                atual.Clear();
            }
            else
            {
                atual.Append(c);
            }
        }

        campos.Add(atual.ToString());
        return [.. campos];
    }
}
