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
| 4 — Solicitação e validação | Formulário com QQP em cascata e custo, rascunho no navegador, envio com número AAAA-NNNNNN e anexos, validação do Gestor e do SESI, devoluções e correção | Concluída |
| 5 — Processo SESI | Vaga com link, entrevistas, exames médicos e finalização, na sequência obrigatória; datas das etapas congeladas | Concluída |
| 6 — Cancelamento e anexos | Cancelamento pelo Gestor, operações excepcionais do Admin (cancelar, mudar etapa, alterar data congelada) e anexos na correção | Concluída |
| 7 — Telas | Painel de cada perfil, tela de acompanhamento com filtros e consulta da auditoria | Concluída |
| 8 — Notificações | Só se confirmadas pelo cliente (fora do MVP) | Pendente |

## Painéis, acompanhamento e auditoria (Etapa 7)

| Tela | Quem | O que mostra |
| --- | --- | --- |
| `/` (Início) | Todos | Painel do perfil com os quadros da seção 21–22 e a quantidade de cada um; cada quadro abre a lista já filtrada. O Solicitante vê também o quadro "Rascunho" quando há rascunho neste navegador. |
| `/Demandas` | Todos | Acompanhamento: número, vaga, solicitante, contratada e contrato, Gestor, aprovação, data limite, dias decorridos e restantes, farol, etapa e status, responsável SESI. Filtros: situação, etapa, status, farol, período (data de envio), número, contratada, Gestor, Solicitante e responsável SESI. **Abre só com as em andamento**; a situação troca para concluídas, canceladas ou todas. |
| `/Admin/Auditoria` | Admin | Log geral de auditoria, do mais recente ao mais antigo, com quem, perfil, IP, valores anteriores e novos e justificativa; filtros por entidade, ação e período, 50 por página. |

| Perfil | Quadros |
| --- | --- |
| Solicitante | Rascunho (deste navegador), Minhas demandas, Em andamento, Aguardando correção, Finalizadas, Canceladas |
| Gestor | Aguardando validação (só dos seus contratos), Correções pendentes, Aprovadas (à espera do SESI), Em processo SESI, Próximas do vencimento, Atrasadas |
| Funcionário SESI | Aguardando aceite, Recrutamento, Entrevistas, Exames médicos, Finalizadas, Próximas do vencimento, Atrasadas |
| Admin | Em andamento, Próximas do vencimento, Atrasadas, Finalizadas, Canceladas, e o atalho para a auditoria |

As regras dos quadros ficam em `Domain/Demandas/Quadros.cs`; os filtros, em `Application/Demandas/FiltroDemandas.cs`. "Próximas do vencimento" = farol amarelo ou laranja; "Atrasadas" = vermelho e não finalizada.

## Cancelamento, operações excepcionais e anexos (Etapa 6)

| Quem | O quê | Onde |
| --- | --- | --- |
| Gestor do contrato | UC18: cancela a demanda ainda não concluída, com justificativa. Ela fica Cancelado na etapa em que estava; nada é excluído | Tela da demanda, quadro "Cancelar demanda" |
| Admin | UC20: cancela em caráter excepcional; muda a etapa para trás ou para frente entre Recrutamento, Entrevistas e Exames Médicos (o SLA não reinicia); altera uma data congelada (início ou conclusão de etapa, abertura da vaga, início do SLA, que recalcula a data limite) | Tela da demanda, quadro "Operações excepcionais" |
| Solicitante | Anexa documentos depois do envio só ao corrigir uma demanda devolvida, inclusive um novo De acordo VP-2; os anteriores continuam | `/Demandas/Corrigir/{id}` |

- **Auditoria.** Todo cancelamento e toda operação excepcional exigem justificativa e vão para o LogAuditoria com valor anterior e novo, usuário, perfil e IP. Datas alteradas também aparecem na tabela "Alterações de campos e datas" da demanda, com a justificativa; a transição forçada aparece na linha do tempo.
- **Datas.** O Admin digita a nova data e hora no horário de Brasília; o sistema grava em UTC. Não aceita data no futuro, anterior ao envio nem conclusão antes do início da etapa.
- **Sair.** O botão Sair do menu encerra a sessão; `/Sair` mostra a confirmação, e `/Entrar` mostra quem está conectado e oferece trocar de conta.

## Processo SESI (Etapa 5)

Na tela da demanda (`/Demandas/Detalhe/{id}`), depois do aceite, o Funcionário SESI do contrato vê só o próximo passo:

| Etapa / situação | Ação | Caso de uso |
| --- | --- | --- |
| Recrutamento, sem vaga | Registrar vaga com o link da plataforma externa (http ou https); é um evento, a etapa não muda | UC08 |
| Recrutamento, com vaga | Iniciar entrevistas | UC09 |
| Entrevistas | Iniciar exames médicos | UC10 |
| Exames Médicos | Finalizar contratação (encerra o SLA; farol verde se dentro do prazo, vermelho se depois) | UC11 |

- **Regras no domínio.** `Demanda.RegistrarVaga`, `IniciarEntrevistas`, `IniciarExames` e `Finalizar` exigem a etapa certa e um SESI ativo do contrato (qualquer um do grupo; o responsável SESI é quem aceitou). `Demanda.AcoesDisponiveis` oferece só o próximo passo. Os casos de uso ficam em `Application/Demandas/ProcessoSesi.cs`.
- **Congelamento de datas (seção 12–15).** Concluída a passagem por uma etapa, `EtapaDemanda` recusa qualquer mudança de data, status ou responsável. A tela mostra o quadro "Etapas e datas", com início, conclusão (cadeado) e quem concluiu cada etapa. A alteração excepcional pelo Admin (UC20) entra na Etapa 6.

## Solicitação e validação (Etapa 4)

| Tela | Quem acessa | O que faz |
| --- | --- | --- |
| `/Demandas/Nova` | Solicitante | UC02: formulário da seção 8.1 em 7 grupos, cargo pelas listas em cascata do QQP, contrato mostrado pelo corredor (RN13), custo mensal calculado pelo servidor a cada mudança (RN12) e anexos (De acordo VP-2 obrigatório). O rascunho fica só neste navegador por 3 dias desde o último salvamento; o botão "Descartar rascunho" pede confirmação (UC17). |
| `/Demandas` | Todos | UC03: as demandas que o perfil vê (seção 4), com etapa, status, farol e custo. Os painéis por perfil ficam para a Etapa 7. |
| `/Demandas/Detalhe/{id}` | Quem vê a demanda | Dados, anexos (download), linha do tempo, alterações das correções, SLA, custo. O Gestor do contrato aprova ou devolve (UC04, UC05); o SESI do contrato aceita ou devolve com o tipo da inconsistência (UC05, UC07). Só aparecem os botões que o domínio permite. |
| `/Demandas/Corrigir/{id}` | Solicitante da demanda | UC06: o formulário preenchido, com o motivo da devolução. Cada campo alterado vai para o HistoricoAlteracao com usuário, perfil e IP. |
| `/Admin/GerentesExecutivos` | Admin | Cadastra, desativa e reativa os gerentes executivos e define os corredores que cada um atende; o formulário mostra só os gerentes do corredor escolhido. O catálogo começa vazio, porque são nomes reais e não vão para o repositório. |
| `/Admin/OrdensServico` | Admin | Cadastra, desativa e reativa as OS de cada contrato. O formulário mostra só as OS do contrato do corredor; o coletor de custo continua livre, porque a mesma OS pode ter coletores diferentes. |

- **Domínio.** `DadosSolicitacao` confere obrigatórios, condicionais (período em meses na vaga temporária, categoria da CNH) e os tamanhos do modelo físico. `CustoDemanda` tem a fórmula da RN12. `Demanda.Enviar` exige o De acordo VP-2 e o contrato do corredor; `Demanda.Corrigir` registra cada alteração e recalcula o custo inteiro com os valores atuais quando mudam o item QQP, a quantidade ou os equipamentos (Cliente). `FiltroVisibilidade` diz quem vê o quê; `Demanda.AcoesDisponiveis` diz quais botões cada um vê. `RegraArquivo` confere extensão, conteúdo e tamanho dos anexos.
- **Número AAAA-NNNNNN.** O sequencial recomeça do 1 a cada ano (Cliente). A tabela `SequenciaNumeroDemanda` é atualizada com `MERGE ... WITH (HOLDLOCK)` na mesma transação do envio: envios simultâneos não repetem número, e um envio recusado não gasta número.
- **Anexos.** Gravados na pasta `Anexos:Pasta` (padrão `src/Contratacao.Web/App_Data/anexos`, fora do Git) com nome gerado pelo sistema; o banco guarda nome original, tipo, tamanho, etapa e o caminho. Nada é apagado. Cada upload gera registro no LogAuditoria.
- **RACs.** A tabela `DemandaRac` é editada direto na correção (seção 8.1): a RAC desmarcada sai da tabela de ligação, e a mudança fica no HistoricoAlteracao. No banco a chave continua sem exclusão em cascata.
- **Concorrência.** Se duas pessoas agem sobre a mesma demanda, a segunda recebe "Esta demanda foi alterada por outra pessoa... Recarregue a página" (RNF10).
- **Antes de testar:** o Admin precisa cadastrar ao menos um gerente executivo em `/Admin/GerentesExecutivos`, porque o campo é obrigatório. As OS 01 a 10 (contrato Norte) e 11 a 20 (Sudeste) já vêm na carga inicial.

## Usuários e acesso (Etapa 3)

| Tela | Quem acessa | O que faz |
| --- | --- | --- |
| `/Entrar` | Todos | UC01: e-mail @ucl.br e senha. Usuário desativado não entra. |
| `/Cadastro` | Público | UC13: autocadastro do Solicitante. |
| `/Admin/Gestores` | Admin | UC12: cadastra Gestores, define os contratos de cada um, desativa e reativa. |
| `/Admin/FuncionariosSesi` | Admin | Lista todos os Funcionários SESI e troca o contrato de qualquer um, para qualquer contrato. |
| `/Admin/Transferencias` | Admin | UC19: move Funcionários SESI de um Gestor para outro, com justificativa. |
| `/Equipe` | Gestor | UC14: cadastra Funcionários SESI na sua equipe, troca o contrato (só entre os contratos do Gestor), desativa e reativa. Só a própria equipe. |
| `/Solicitantes` | Admin e Gestor | Desativa e reativa Solicitantes. |
| `/TrocarSenha` | Gestor e Funcionário SESI | Troca obrigatória da senha inicial no primeiro acesso; até trocar, toda página leva para cá. |

- **Camadas.** As regras de quem gerencia quem ficam em `Usuario` (Domain). Cada caso de uso é uma classe em `Application/Usuarios`, que usa portas (`IUsuarios`, `IContratos`, `IAuditoria`, `IHashSenha`) implementadas em `Infrastructure/Persistencia/Repositorios.cs`. As páginas só chamam os casos de uso.
- **Sessão.** Cookie com id, nome, e-mail e perfil. A cada requisição o sistema confere se o usuário continua ativo e com o mesmo perfil; quem for desativado perde a sessão na página seguinte. Os contratos do Gestor e do SESI são relidos do banco a cada ação.
- **Senha inicial.** Quem cadastra o Gestor ou o Funcionário SESI define a senha inicial (coluna `DeveTrocarSenha`); o `FiltroTrocaSenha` leva o usuário à troca antes de qualquer outra página. Solicitante e Admin inicial definem a própria senha e não passam pela troca.
- **Auditoria.** Cadastros, desativações, reativações, troca de contratos, trocas de senha e transferências vão para `LogAuditoria`, com usuário, perfil e IP.
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

O relógio real (`Infrastructure/Tempo/RelogioSistema`) é registrado em `ConfiguracaoInfraestrutura`.

Os testes ficam em `tests/Contratacao.Tests/Unitarios` e rodam sem banco. A matriz de transições testa as 10 ações em 10 situações da demanda (100 casos).

## Persistência (Etapa 2)

- `Infrastructure/Persistencia/ContratacaoDbContext` com um mapeamento por tabela em `Configuracoes/`, seguindo o script da seção 23: nomes de chaves, tamanhos, tipos e checks. As colunas do formulário e a tabela `DemandaRac` vieram na migration `CamposDaSolicitacao` (Etapa 4).
- Perfil, etapa e status são enums no domínio e viram `uniqueidentifier` fixos (`IdsFixos`) nas tabelas `Perfil`, `Etapa` e `Status`.
- Datas gravadas e lidas como UTC; `DataLimiteSLA` é `date`.
- Nenhuma chave estrangeira exclui em cascata. A demanda tem `RowVersion` para concorrência otimista (RNF10).
- **Carga inicial.** Os catálogos fixos (perfis, etapas, status, prazo de 45 dias, modelos de trabalho, tipo de demanda, equipamentos, Contratada SESI, contratos, regiões e corredores) vão na migration `Inicial`. O comando `preparar-banco` aplica as migrations e carrega o que depende de arquivo ou configuração: o catálogo QQP e as RACs, lidos dos CSV de `dados/`, e o Admin inicial, lido dos user-secrets. Ele pode rodar várias vezes sem duplicar nada.

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

**Catálogo QQP.** O arquivo `dados/tb_qqp_SESI.csv` do repositório tem piso e preço alterados, para não expor os valores reais (decisão do cliente). Para usar os valores reais, troque o arquivo pelo fornecido pelo cliente antes de preparar o banco, sem fazer commit dele.

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

Nenhuma no momento: o cliente confirmou ou substituiu todas (S1 a S23).

## Pendências

- **QQP 466 e 467:** repetem a mesma combinação; são importados assim mesmo, com o código como chave.
