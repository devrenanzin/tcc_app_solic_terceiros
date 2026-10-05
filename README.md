# Sistema de Solicitação e Acompanhamento de Contratação

Aplicação web em C#/.NET para registrar, validar e acompanhar demandas de contratação, do pedido do Solicitante até a contratação finalizada pelo SESI, com SLA de 45 dias, farol, linha do tempo e auditoria completa.

Trabalho de Conclusão de Curso (TCC) — Renan Teles dos Santos.

Os requisitos estão em [docs/REQUISITOS_v3.1.md](docs/REQUISITOS_v3.1.md), a única fonte de requisitos do projeto.

## Situação

| Etapa | Entrega | Situação |
| --- | --- | --- |
| 0 — Estrutura | Solution, camadas, testes de arquitetura, provas de tipos internal | Concluída |
| 1 a 8 | Ver o Guia de implementação no documento de requisitos | Pendentes |

## Tecnologias

- .NET 10 (SDK fixado em `global.json`), ASP.NET Core, EF Core 10 com SQL Server, xUnit v3.
- Interface: Razor Pages renderizadas no servidor, htmx para atualizações parciais e Bootstrap 5.3 com tema próprio (`wwwroot/css/tema.css`). As bibliotecas ficam em `wwwroot/lib`, sem CDN.

## Estrutura

```
src/Contratacao.Web/
  Domain/          entidades, máquina de estados, SLA, farol, custo
  Application/     um handler por caso de uso, autorização
  Infrastructure/  EF Core, migrations, anexos, auditoria
  Web/             interface (Web/Pages), endpoints, autenticação
  wwwroot/         arquivos estáticos (exigência do framework, fora das camadas)
tests/Contratacao.Tests/
  Arquitetura/     regra de dependência e regra de tipos internal
  Integracao/      páginas, endpoints e migrations com tipos internal
```

Regra de dependência (verificada por testes): Domain não depende de nenhuma camada nem de ASP.NET Core ou EF Core; Application depende só de Domain; Infrastructure não depende de Web.

## Tipos internal

Todo tipo do projeto web é `internal`; os testes acessam os tipos por `InternalsVisibleTo`. Como cada parte foi resolvida:

| Parte | Solução |
| --- | --- |
| `Program` | O .NET 10 gera `public partial class Program`; a declaração `internal partial class Program;` em `Program.cs` mantém o tipo internal. |
| Razor Pages | PageModel `internal` funciona; as páginas ficam em `Web/Pages` (`RootDirectory`). |
| Endpoints mínimos | Handlers `internal` funcionam. |
| Migrations do EF Core | `Infrastructure/Persistencia/GeradorMigrationsInternal` faz o `dotnet ef` gerar `internal partial class`; o snapshot e o `.Designer` já saem sem modificador. |
| Controllers MVC | Não usados. |

**Exceções públicas:** só as classes de teste, porque o xUnit as exige públicas (regra xUnit1000). O teste `TiposInternalTests` falha se aparecer qualquer tipo público no projeto web.

A prova de migrations usa um modelo descartável no projeto de testes (`tests/Contratacao.Tests/Integracao/Prova`). A migration real do sistema será criada na Etapa 2.

## Executar localmente

Pré-requisitos: SDK do .NET 10, SQL Server (LocalDB, Developer Edition ou container Docker) e a ferramenta do EF Core.

```bash
dotnet tool install --global dotnet-ef

dotnet build
dotnet test
dotnet run --project src/Contratacao.Web
```

Os testes de integração de migrations criam e apagam um banco temporário em `(localdb)\MSSQLLocalDB`. Para usar outro servidor, defina a variável `CONTRATACAO_TESTES_SQL` com a connection string sem o nome do banco, por exemplo `Server=localhost;Trusted_Connection=True;TrustServerCertificate=True`.

A partir da Etapa 2, a connection string da aplicação ficará fora do código:

```bash
dotnet user-secrets set "ConnectionStrings:Contratacao" "Server=(localdb)\MSSQLLocalDB;Database=Contratacao;Trusted_Connection=True;TrustServerCertificate=True" --project src/Contratacao.Web
dotnet ef database update --project src/Contratacao.Web
```

## Suposições em uso

Nenhuma até a Etapa 0. Cada suposição usada no código (S1–S16 do documento de requisitos) será listada aqui e marcada com `// SUPOSIÇÃO (S<n>)`.
