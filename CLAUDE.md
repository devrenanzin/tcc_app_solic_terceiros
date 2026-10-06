# Sistema de Solicitação e Acompanhamento de Contratação

Aplicação web em C#/.NET para registrar, validar e acompanhar demandas de contratação, do pedido do Solicitante até a contratação finalizada pelo SESI, com SLA de 45 dias, farol, linha do tempo e auditoria completa.

## Fonte de verdade

`docs/REQUISITOS_v3.1.md` é a única fonte de requisitos. Leia o documento inteiro antes de começar qualquer etapa e volte a ele sempre que houver dúvida. Seções que mais orientam o código:

| Assunto | Seção do documento |
| --- | --- |
| Perfis, hierarquia e quem vê o quê | 4 e 4.5 (inclui RN11, autenticação) |
| Etapas, status e eventos | 5 |
| Fluxo, devoluções e máquina de estados | 6–7 e Apêndice A.1 |
| Casos de uso UC01 a UC20 | 8 |
| Formulário, custo (RN12) e contrato por corredor (RN13) | 8.1 |
| SLA e farol | 9–11 |
| Auditoria, congelamento de datas, cancelamento | 12–17 |
| Anexos | 18 |
| Permissões por funcionalidade e por campo | 19–20 |
| Modelos de dados (lógico, físico, catálogos, seed) | 23 |
| Transições permitidas | 24 |
| Arquitetura, pacotes e implantação | 27 e Apêndice A |
| Etapas de implementação e comandos | Guia de implementação |
| Suposições e decisões pendentes | Última seção |

Os diagramas existem como código PlantUML e Mermaid dentro do documento; use-os como referência de estrutura.

## Regras inegociáveis

1. **Não invente regra de negócio.** Se algo não está no documento, pare e pergunte antes de implementar.
2. **Suposições marcadas.** Todo código que depende de uma suposição da lista de suposições do documento (as que ainda estão abertas) leva o comentário `// SUPOSIÇÃO (S<n>)` e entra na lista de suposições do README.
3. **Tipos `internal`.** Todo tipo C# é `internal`. Exceção pública só onde o framework exige, com comentário dizendo por quê. O projeto de testes acessa os tipos por `InternalsVisibleTo`.
4. **Regras no domínio.** Máquina de estados, SLA, farol, custo e permissões ficam em `Domain` e são testadas sem banco. Controllers e páginas não contêm regra.
5. **Tempo controlado.** Nada de `DateTime.Now` no domínio: use um relógio injetável. Datas gravadas em UTC; contagem de prazo no fuso `America/Sao_Paulo`.
6. **Nada se apaga.** Demanda enviada, histórico, anexo e auditoria nunca são excluídos. Sem exclusão em cascata e sem nenhuma exclusão física: o rascunho existe só no navegador (UC02, UC17).
7. **Tudo auditado.** Toda ação relevante gera registro em HistoricoDemanda, HistoricoAlteracao ou LogAuditoria, com usuário, perfil, IP e data/hora.
8. **Dados pessoais e valores do cliente.** Nunca coloque nomes ou e-mails reais em código, seed ou testes. Use dados fictícios com e-mails `@ucl.br`. Os valores do QQP também não vão para o repositório: o arquivo `dados/tb_qqp_SESI.csv` fica fora do Git, e nenhum valor dele é copiado para código, testes ou documentos. Nomes reais (como os gerentes executivos) entram só no banco, pela tela ou direto no banco local, nunca no Git.

## Decisões já tomadas pelo cliente

- **Stack:** .NET 10, ASP.NET Core, EF Core com SQL Server, xUnit.
- **Interface:** tecnologia livre, a mais adequada ao ASP.NET Core e à regra de tipos internal. O front-end deve ser bonito, bem estruturado, responsivo e consistente entre os perfis.
- **Login:** e-mail do domínio `@ucl.br` e senha própria do sistema, guardada só como hash. O domínio imita o Google Workspace da UCL, mas não há integração real com ele.
- **Anexos:** imagem, PDF e e-mail, até 10 MB, numa pasta do servidor, nunca apagados. O anexo "De acordo VP-2" é obrigatório para enviar a demanda.
- **Contrato:** definido pelo corredor (RN13), nunca escolhido pelo Solicitante. A Contratada vem do contrato; hoje só o SESI. Cada OS pertence a um contrato (lista mantida pelo Admin); o coletor de custo é livre, porque a mesma OS pode ter coletores diferentes. Informações contratuais = OS, corredor e contrato.
- **Cadastro:** o Solicitante se cadastra sozinho com e-mail `@ucl.br`; o Admin e qualquer Gestor ativo o desativam e reativam. O Gestor cadastra os Funcionários SESI e vincula cada um ao grupo de um dos seus contratos (outro contrato, só pelo Admin); o SESI só vê e trata demandas desse contrato. Gestor e Funcionário SESI trocam a senha inicial no primeiro acesso.
- **Formulário e custo:** sem veículo e rastreador (melhoria futura). Custo mensal = Quantidade × (Preço QQP + equipamentos por pessoa). Período temporário em meses. Correção que muda item QQP, quantidade ou equipamentos recalcula o custo inteiro com os valores atuais; sem essas mudanças, ficam os valores do envio.
- **Gestores e contratos:** o Admin vincula cada Gestor a um ou mais contratos (GestorContrato). Todo Gestor vê todas as demandas, mas só os Gestores do contrato da demanda a validam, devolvem e cancelam.
- **Rascunho:** só no navegador do Solicitante, por 3 dias desde o último salvamento. Anexos são escolhidos no envio. A demanda passa a existir no sistema no envio.
- **Gestor da demanda:** o Gestor que valida. O Gestor só aprova ou devolve; não edita campos.
- **Correções:** só o Solicitante corrige (UC16 removido). Devolução do SESI informa o tipo: erro do solicitante volta ao SESI; erro contratual (ou troca de corredor que muda o contrato) volta à Validação do Gestor.
- **Contratada:** hoje só o SESI; CNPJ opcional.
- **Revisão de 05/10/2026:** a lista completa está na seção "Revisão de 05/10/2026" do documento de requisitos.
- **Estrutura:** um projeto web único com camadas em pastas, mais um projeto de testes.

## Estrutura esperada do repositório

```
CLAUDE.md
README.md
docs/REQUISITOS_v3.1.md
dados/tb_qqp_SESI.csv        catálogo QQP (1.019 itens), lido pela seed; NÃO vai para o Git (valores confidenciais)
dados/tb_racs.csv            13 RACs, lidas pela seed
src/Contratacao.Web/
  Domain/                    entidades, máquina de estados, SLA, farol, custo
  Application/               um handler por caso de uso, autorização
  Infrastructure/            EF Core, migrations, anexos, auditoria
  Web/                       interface, endpoints, autenticação
tests/Contratacao.Tests/     unitários, integração e arquitetura
```

A regra de dependência (Domain não depende de nada; Application só de Domain) é verificada por testes de arquitetura.

## Como trabalhar

- Implemente uma etapa por vez, na ordem do Guia de implementação (0 a 8), e **pare ao fim de cada etapa** para revisão.
- Uma etapa só termina com build verde, testes passando, migration gerada quando o modelo mudar e README atualizado.
- Faça um commit por etapa, com mensagem em português descrevendo o que foi entregue.
- Ao terminar cada etapa, informe: o que foi feito, como testar, quais suposições foram usadas e o que ficou pendente.

## Pendências que ainda podem bloquear

| Pendência | Bloqueia |
| --- | --- |
| Códigos 466 e 467 do QQP repetem a mesma combinação | Nada: importados assim mesmo, com o código como chave |
| LGPD: classificação dos dados pessoais, retenção e perfis autorizados | Antes da produção |

As etapas 0 a 4 estão concluídas. Pergunte sobre o restante quando chegar nele.

## Comandos

```bash
dotnet tool install --global dotnet-ef
dotnet user-secrets set "ConnectionStrings:Contratacao" "<connection string>" --project src/Contratacao.Web
dotnet ef database update --project src/Contratacao.Web
dotnet test
dotnet run --project src/Contratacao.Web
```
