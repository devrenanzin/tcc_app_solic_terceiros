# Sistema de Solicitação e Acompanhamento de Contratação

Aplicação web em C#/.NET para registrar, validar e acompanhar demandas de contratação, do pedido do Solicitante até a contratação finalizada pelo SESI, com SLA de 45 dias, farol, linha do tempo e auditoria completa.

Trabalho de Conclusão de Curso (TCC) — Renan Teles dos Santos.

Os requisitos estão em [docs/REQUISITOS_v3.1.md](docs/REQUISITOS_v3.1.md), a única fonte de requisitos do projeto.

## Situação

| Etapa | Entrega | Situação |
| --- | --- | --- |
| 0 — Estrutura | Solution, camadas, testes de arquitetura, provas de tipos internal | Concluída |
| 1 — Domínio | Máquina de estados da demanda, devoluções e correções, cancelamento, SLA, farol, número AAAA-NNNNNN | Concluída |
| 2 a 8 | Ver o Guia de implementação no documento de requisitos | Pendentes |

## Domínio (Etapa 1)

| Pasta | Conteúdo |
| --- | --- |
| `Domain/Demandas` | `Demanda` com a máquina de estados (seções 6–7 e 24): envio, aprovação, devoluções do Gestor e do SESI, correção, aceite, vaga, entrevistas, exames, finalização e cancelamento. Cada ação valida etapa, status e permissão do ator (perfil, atividade e contrato) e grava o histórico. Também `EtapaDemanda` (data de conclusão imutável), `SolicitacaoCorrecao`, `Vaga`, `HistoricoDemanda` e `NumeroDemanda`. |
| `Domain/Prazos` | `Sla` (45 dias corridos sobre a data de Brasília, sem pausa nem reinício) e `RegraFarol`. |
| `Domain/Comum` | `IRelogio`, `ICalendarioSla` com `CalendarioBrasilia` (fuso America/Sao_Paulo) e `RegraNegocioException`. |
| `Domain/Usuarios` | `Perfil` e `Ator` (quem executa: perfil, contratos e IP). |

O relógio real (`Infrastructure/Tempo/RelogioSistema`) é registrado em `ConfiguracaoInfraestrutura`. Os campos do formulário, o custo e a verificação do anexo VP-2 no envio entram na Etapa 4.

Os testes ficam em `tests/Contratacao.Tests/Unitarios` e rodam sem banco. A matriz de transições testa as 10 ações em 10 situações da demanda (100 casos).

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
  Unitarios/       regras do domínio, sem banco
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

Cada suposição usada no código (as abertas na seção "Suposições e pendências" do documento de requisitos) é listada aqui e marcada com `// SUPOSIÇÃO (S<n>)`.

| # | Suposição | Onde |
| --- | --- | --- |
| S4 | Depois do aceite, qualquer Funcionário SESI ativo do contrato registra vaga, entrevistas, exames e finalização; o responsável SESI é a referência | `Demanda.ExigirSesiDoContrato` |
| S7 | 45 dias corridos; dia limite em laranja; demanda cancelada em cinza | `Sla.Iniciar`, `RegraFarol` |

## Pendências

- **Gestor da demanda após nova aprovação:** quando a demanda volta ao Gestor por correção contratual e é aprovada de novo, ainda falta definir se `GestorId` fica com o Gestor da primeira ou da última aprovação. Hoje fica o da última (`Demanda.Aprovar`, marcado com `PENDENTE`).
