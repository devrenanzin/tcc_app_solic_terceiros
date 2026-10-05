# Sistema de Solicitação e Acompanhamento de Contratação

Aplicação web em C#/.NET para registrar, validar e acompanhar demandas de contratação, do pedido do Solicitante até a contratação finalizada pelo SESI, com SLA de 45 dias, farol, linha do tempo e auditoria completa.

Trabalho de Conclusão de Curso (TCC) — Renan Teles dos Santos.

Os requisitos estão em [docs/REQUISITOS_v3.1.md](docs/REQUISITOS_v3.1.md), a única fonte de requisitos do projeto.

## Situação

| Etapa | Entrega | Situação |
| --- | --- | --- |
| 0 — Estrutura | Solution, camadas, testes de arquitetura, provas de tipos internal | Concluída |
| 1 — Domínio | Máquina de estados da demanda, devoluções e correções, cancelamento, SLA, farol, número AAAA-NNNNNN | Concluída |
| 2 — Persistência | DbContext, mapeamento do modelo físico, migration inicial, carga inicial (catálogos, QQP, RACs, Admin) | Concluída |
| 3 — Usuários e acesso | Login, autocadastro do Solicitante, cadastro de Gestores e Funcionários SESI, contratos, desativação, transferência de vínculo, telas | Concluída |
| 4 a 8 | Ver o Guia de implementação no documento de requisitos | Pendentes |

## Usuários e acesso (Etapa 3)

| Tela | Quem acessa | O que faz |
| --- | --- | --- |
| `/Entrar` | Todos | UC01: e-mail @ucl.br e senha. Usuário desativado não entra. |
| `/Cadastro` | Público | UC13: autocadastro do Solicitante. |
| `/Admin/Gestores` | Admin | UC12: cadastra Gestores, define os contratos de cada um, desativa e reativa. |
| `/Admin/Transferencias` | Admin | UC19: move Funcionários SESI de um Gestor para outro, com justificativa. |
| `/Equipe` | Gestor | UC14: cadastra Funcionários SESI na sua equipe, troca o contrato, desativa e reativa. Só a própria equipe. |
| `/Solicitantes` | Admin e Gestor | Desativa e reativa Solicitantes. |

- **Camadas.** As regras de quem gerencia quem ficam em `Usuario` (Domain). Cada caso de uso é uma classe em `Application/Usuarios`, que usa portas (`IUsuarios`, `IContratos`, `IAuditoria`, `IHashSenha`) implementadas em `Infrastructure/Persistencia/Repositorios.cs`. As páginas só chamam os casos de uso.
- **Sessão.** Cookie com id, nome, e-mail e perfil. A cada requisição o sistema confere se o usuário continua ativo e com o mesmo perfil; quem for desativado perde a sessão na página seguinte. Os contratos do Gestor e do SESI são relidos do banco a cada ação.
- **Auditoria.** Cadastros, desativações, reativações, troca de contratos e transferências vão para `LogAuditoria`, com usuário, perfil e IP.
- **Acesso por perfil.** As pastas `Admin`, `Equipe` e `Solicitantes` exigem o perfil certo; as demais páginas exigem login, exceto `Entrar`, `Cadastro` e `AcessoNegado`.

## Domínio (Etapa 1)

| Pasta | Conteúdo |
| --- | --- |
| `Domain/Demandas` | `Demanda` com a máquina de estados (seções 6–7 e 24): envio, aprovação, devoluções do Gestor e do SESI, correção, aceite, vaga, entrevistas, exames, finalização e cancelamento. Cada ação valida etapa, status e permissão do ator (perfil, atividade e contrato) e grava o histórico. Também `EtapaDemanda` (data de conclusão imutável), `SolicitacaoCorrecao`, `Vaga`, `HistoricoDemanda` e `NumeroDemanda`. |
| `Domain/Prazos` | `Sla` (45 dias corridos sobre a data de Brasília, sem pausa nem reinício) e `RegraFarol`. |
| `Domain/Comum` | `IRelogio`, `ICalendarioSla` com `CalendarioBrasilia` (fuso America/Sao_Paulo) e `RegraNegocioException`. |
| `Domain/Usuarios` | `Perfil` e `Ator` (quem executa: perfil, contratos e IP), `Usuario`, `GestorContrato` e `EmailUcl` (RN11). |
| `Domain/Contratos`, `Catalogos`, `Qqp` | Contratada, contrato, corredor e os catálogos do formulário e do QQP. |
| `Domain/Auditoria`, `Anexos`, `Parametros` | `LogAuditoria`, `HistoricoAlteracao`, `Anexo`, `ParametroSistema` e `SequenciaNumeroDemanda`. |

O relógio real (`Infrastructure/Tempo/RelogioSistema`) é registrado em `ConfiguracaoInfraestrutura`. Os campos do formulário, o custo e a verificação do anexo VP-2 no envio entram na Etapa 4.

Os testes ficam em `tests/Contratacao.Tests/Unitarios` e rodam sem banco. A matriz de transições testa as 10 ações em 10 situações da demanda (100 casos).

## Persistência (Etapa 2)

- `Infrastructure/Persistencia/ContratacaoDbContext` com um mapeamento por tabela em `Configuracoes/`, seguindo o script da seção 23: nomes de chaves, tamanhos, tipos e checks. A tabela `DemandaRac` e as colunas do formulário entram na Etapa 4.
- Perfil, etapa e status são enums no domínio e viram `uniqueidentifier` fixos (`IdsFixos`) nas tabelas `Perfil`, `Etapa` e `Status`.
- Datas gravadas e lidas como UTC; `DataLimiteSLA` é `date`.
- Nenhuma chave estrangeira exclui em cascata. A demanda tem `RowVersion` para concorrência otimista (RNF10).
- **Carga inicial.** Os catálogos fixos (perfis, etapas, status, prazo de 45 dias, modelos de trabalho, tipo de demanda, veículos, equipamentos, Contratada SESI, contratos, regiões e corredores) vão na migration `Inicial`. O comando `preparar-banco` aplica as migrations e carrega o que depende de arquivo ou configuração: o catálogo QQP e as RACs, lidos dos CSV de `dados/`, e o Admin inicial, lido dos user-secrets. Ele pode rodar várias vezes sem duplicar nada.

Os testes de integração (`tests/Contratacao.Tests/Integracao`) criam um banco temporário, aplicam a migration, fazem a carga e conferem os dados, o fluxo completo de uma demanda gravada e relida, a concorrência, a ausência de cascata e os checks.

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
  Integracao/      telas, casos de uso, migrations, carga inicial e persistência (SQL Server)
  Unitarios/       regras do domínio e leitura dos CSV, sem banco
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

A prova de migrations usa um modelo descartável no projeto de testes (`tests/Contratacao.Tests/Integracao/Prova`). As migrations do sistema ficam em `Infrastructure/Persistencia/Migrations`.

## Executar localmente

Pré-requisitos: SDK do .NET 10, SQL Server (LocalDB, Developer Edition ou container Docker) e a ferramenta do EF Core.

A connection string e o Admin inicial ficam fora do código, nos user-secrets (use um e-mail fictício @ucl.br):

```bash
dotnet tool install --global dotnet-ef

dotnet user-secrets set "ConnectionStrings:Contratacao" "Server=(localdb)\MSSQLLocalDB;Database=Contratacao;Trusted_Connection=True;TrustServerCertificate=True" --project src/Contratacao.Web
dotnet user-secrets set "AdminInicial:Nome" "Administrador" --project src/Contratacao.Web
dotnet user-secrets set "AdminInicial:Email" "admin@ucl.br" --project src/Contratacao.Web
dotnet user-secrets set "AdminInicial:Senha" "<senha inicial>" --project src/Contratacao.Web

# cria o banco, aplica as migrations e faz a carga inicial
dotnet run --project src/Contratacao.Web -- preparar-banco

dotnet test
dotnet run --project src/Contratacao.Web
```

`dotnet ef database update --project src/Contratacao.Web` também aplica as migrations, mas não faz a carga do QQP, das RACs e do Admin.

Os testes de integração criam e apagam bancos temporários em `(localdb)\MSSQLLocalDB`. Para usar outro servidor, defina a variável `CONTRATACAO_TESTES_SQL` com a connection string sem o nome do banco, por exemplo `Server=localhost;Trusted_Connection=True;TrustServerCertificate=True`.

## Suposições em uso

Cada suposição usada no código (as abertas na seção "Suposições e pendências" do documento de requisitos) é listada aqui e marcada com `// SUPOSIÇÃO (S<n>)`.

| # | Suposição | Onde |
| --- | --- | --- |
| S4 | Depois do aceite, qualquer Funcionário SESI ativo do contrato registra vaga, entrevistas, exames e finalização; o responsável SESI é a referência | `Demanda.ExigirSesiDoContrato` |
| S7 | 45 dias corridos; dia limite em laranja; demanda cancelada em cinza | `Sla.Iniciar`, `RegraFarol` |
| S9 | Classificação "-" na planilha QQP significa "sem classificação" (ClassificacaoId nulo) | `CargaInicial.CarregarQqpAsync` |
| S2 | A equipe de um Gestor desativado continua ativa e pode ser transferida pelo Admin | `Usuario.TransferirPara` |
| S21 | Senha com no mínimo 8 e no máximo 128 caracteres | `Senha` |
| S22 | Quem cadastra Gestor ou Funcionário SESI define a senha inicial; a troca de senha pelo próprio usuário não está nos casos de uso | `CadastrarGestor`, `CadastrarFuncionarioSesi` |
| S23 | O Gestor pode vincular o Funcionário SESI a qualquer contrato ativo, não só aos seus | `CadastrarFuncionarioSesi`, `AlterarContratoFuncionarioSesi` |

## Pendências

- **Cobrança de veículo e rastreador (S14):** se são cobrados uma vez por demanda, e não por vaga. Bloqueia o cálculo de custo da Etapa 4. Valores e custo mensal já confirmados.
- **QQP 466 e 467:** repetem a mesma combinação; são importados assim mesmo, com o código como chave.
