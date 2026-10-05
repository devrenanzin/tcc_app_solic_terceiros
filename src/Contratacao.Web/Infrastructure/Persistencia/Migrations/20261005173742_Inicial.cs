using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Contratacao.Web.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    internal partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contratada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CNPJ = table.Column<string>(type: "char(14)", unicode: false, fixedLength: true, maxLength: 14, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratada", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Etapa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Ordem = table.Column<short>(type: "smallint", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GerenteExecutivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GerenteExecutivo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemEquipamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEquipamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModeloTrabalho",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModeloTrabalho", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perfil",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfil", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QqpCargaHoraria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorasSemanais = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QqpCargaHoraria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QqpClassificacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QqpClassificacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QqpFuncao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QqpFuncao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QqpNivel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Ordem = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QqpNivel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QqpRegiao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QqpRegiao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rac",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rac", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SequenciaNumeroDemanda",
                columns: table => new
                {
                    Ano = table.Column<short>(type: "smallint", nullable: false),
                    UltimoNumero = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenciaNumeroDemanda", x => x.Ano);
                });

            migrationBuilder.CreateTable(
                name: "Status",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Status", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipoDemanda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoDemanda", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Veiculo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contrato",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contrato", x => x.Id);
                    table.CheckConstraint("CK_Contrato_Numero", "Numero LIKE '59%' AND Numero NOT LIKE '%[^0-9]%'");
                    table.ForeignKey(
                        name: "FK_Contrato_Contratada",
                        column: x => x.ContratadaId,
                        principalTable: "Contratada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemQqp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<int>(type: "int", nullable: false),
                    RegiaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuncaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassificacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NivelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CargaHorariaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PisoSalarial = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemQqp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemQqp_Carga",
                        column: x => x.CargaHorariaId,
                        principalTable: "QqpCargaHoraria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemQqp_Classificacao",
                        column: x => x.ClassificacaoId,
                        principalTable: "QqpClassificacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemQqp_Funcao",
                        column: x => x.FuncaoId,
                        principalTable: "QqpFuncao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemQqp_Nivel",
                        column: x => x.NivelId,
                        principalTable: "QqpNivel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemQqp_Regiao",
                        column: x => x.RegiaoId,
                        principalTable: "QqpRegiao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Corredor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegiaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Corredor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Corredor_Contrato",
                        column: x => x.ContratoId,
                        principalTable: "Contrato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Corredor_Regiao",
                        column: x => x.RegiaoId,
                        principalTable: "QqpRegiao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerfilId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GestorResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CriadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Login = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataUltimoAcesso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SenhaHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.CheckConstraint("CK_Usuario_Email_Dominio", "Email LIKE '%@ucl.br'");
                    table.ForeignKey(
                        name: "FK_Usuario_Contrato",
                        column: x => x.ContratoId,
                        principalTable: "Contrato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Usuario_CriadoPor",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Usuario_Gestor",
                        column: x => x.GestorResponsavelId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Usuario_Perfil",
                        column: x => x.PerfilId,
                        principalTable: "Perfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Demanda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsuarioSolicitanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GestorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsavelSesiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EtapaAtualId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusAtualId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataInicioSLA = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrazoDiasSla = table.Column<short>(type: "smallint", nullable: true),
                    DataLimiteSLA = table.Column<DateOnly>(type: "date", nullable: true),
                    DataFinalizacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demanda", x => x.Id);
                    table.CheckConstraint("CK_Demanda_Finalizacao", "DataFinalizacao IS NULL OR DataFinalizacao >= DataCriacao");
                    table.CheckConstraint("CK_Demanda_Sla", "DataLimiteSLA IS NULL OR DataLimiteSLA >= CAST(DataInicioSLA AS date)");
                    table.ForeignKey(
                        name: "FK_Demanda_Contratada",
                        column: x => x.ContratadaId,
                        principalTable: "Contratada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demanda_Contrato",
                        column: x => x.ContratoId,
                        principalTable: "Contrato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demanda_Etapa",
                        column: x => x.EtapaAtualId,
                        principalTable: "Etapa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demanda_Gestor",
                        column: x => x.GestorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demanda_ResponsavelSesi",
                        column: x => x.ResponsavelSesiId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demanda_Solicitante",
                        column: x => x.UsuarioSolicitanteId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demanda_Status",
                        column: x => x.StatusAtualId,
                        principalTable: "Status",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GestorContrato",
                columns: table => new
                {
                    GestorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestorContrato", x => new { x.GestorId, x.ContratoId });
                    table.ForeignKey(
                        name: "FK_GestorContrato_Contrato",
                        column: x => x.ContratoId,
                        principalTable: "Contrato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GestorContrato_Gestor",
                        column: x => x.GestorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PerfilUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EnderecoIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    Entidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Acao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NovoValor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Justificativa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Log_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParametroSistema",
                columns: table => new
                {
                    Chave = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlteradoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametroSistema", x => x.Chave);
                    table.ForeignKey(
                        name: "FK_Parametro_Usuario",
                        column: x => x.AlteradoPorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anexo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EtapaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioUploadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeArquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    TipoArquivo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tamanho = table.Column<long>(type: "bigint", nullable: false),
                    Identificador = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Categoria = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    DataUpload = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anexo", x => x.Id);
                    table.CheckConstraint("CK_Anexo_Categoria", "Categoria IN ('Geral', 'DeAcordoVP2')");
                    table.ForeignKey(
                        name: "FK_Anexo_Demanda",
                        column: x => x.DemandaId,
                        principalTable: "Demanda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anexo_Etapa",
                        column: x => x.EtapaId,
                        principalTable: "Etapa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anexo_Usuario",
                        column: x => x.UsuarioUploadId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EtapaDemanda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EtapaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataConclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DemandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapaDemanda", x => x.Id);
                    table.CheckConstraint("CK_EtapaDemanda_Datas", "DataConclusao IS NULL OR DataConclusao >= DataInicio");
                    table.ForeignKey(
                        name: "FK_EtapaDemanda_Demanda",
                        column: x => x.DemandaId,
                        principalTable: "Demanda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EtapaDemanda_Etapa",
                        column: x => x.EtapaId,
                        principalTable: "Etapa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EtapaDemanda_Status",
                        column: x => x.StatusId,
                        principalTable: "Status",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EtapaDemanda_Usuario",
                        column: x => x.UsuarioResponsavelId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoAlteracao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Campo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NovoValor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Justificativa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoAlteracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistAlt_Demanda",
                        column: x => x.DemandaId,
                        principalTable: "Demanda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistAlt_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoDemanda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Acao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerfilUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EnderecoIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EtapaAnteriorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusAnteriorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EtapaNovaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusNovoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DemandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoDemanda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistDemanda_Demanda",
                        column: x => x.DemandaId,
                        principalTable: "Demanda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistDemanda_EtapaAnt",
                        column: x => x.EtapaAnteriorId,
                        principalTable: "Etapa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistDemanda_EtapaNova",
                        column: x => x.EtapaNovaId,
                        principalTable: "Etapa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistDemanda_StatusAnt",
                        column: x => x.StatusAnteriorId,
                        principalTable: "Status",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistDemanda_StatusNovo",
                        column: x => x.StatusNovoId,
                        principalTable: "Status",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistDemanda_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacaoCorrecao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataResolucao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DemandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoCorrecao", x => x.Id);
                    table.CheckConstraint("CK_Correcao_Origem", "Origem IN ('Gestor', 'SESI')");
                    table.CheckConstraint("CK_Correcao_Tipo", "Tipo IN ('Solicitante', 'Contratual')");
                    table.CheckConstraint("CK_Correcao_TipoOrigem", "(Origem = 'SESI' AND Tipo IS NOT NULL) OR (Origem = 'Gestor' AND Tipo IS NULL)");
                    table.ForeignKey(
                        name: "FK_Correcao_Demanda",
                        column: x => x.DemandaId,
                        principalTable: "Demanda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Correcao_Usuario",
                        column: x => x.SolicitadoPorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vaga",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkExterno = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    DataAbertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vaga", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vaga_Demanda",
                        column: x => x.DemandaId,
                        principalTable: "Demanda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vaga_Usuario",
                        column: x => x.UsuarioResponsavelId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Contratada",
                columns: new[] { "Id", "Ativo", "CNPJ", "NomeFantasia", "RazaoSocial" },
                values: new object[] { new Guid("00000008-0000-0000-0000-000000000001"), true, null, null, "SESI" });

            migrationBuilder.InsertData(
                table: "Etapa",
                columns: new[] { "Id", "Ativa", "Nome", "Ordem" },
                values: new object[,]
                {
                    { new Guid("00000002-0000-0000-0000-000000000001"), true, "Solicitação", (short)1 },
                    { new Guid("00000002-0000-0000-0000-000000000002"), true, "Validação do Gestor", (short)2 },
                    { new Guid("00000002-0000-0000-0000-000000000003"), true, "Validação SESI", (short)3 },
                    { new Guid("00000002-0000-0000-0000-000000000004"), true, "Recrutamento", (short)4 },
                    { new Guid("00000002-0000-0000-0000-000000000005"), true, "Entrevistas", (short)5 },
                    { new Guid("00000002-0000-0000-0000-000000000006"), true, "Exames Médicos", (short)6 },
                    { new Guid("00000002-0000-0000-0000-000000000007"), true, "Contratação", (short)7 }
                });

            migrationBuilder.InsertData(
                table: "ItemEquipamento",
                columns: new[] { "Id", "Ativo", "Nome", "Valor" },
                values: new object[,]
                {
                    { new Guid("00000007-0000-0000-0000-000000000001"), true, "Notebook", 444.35m },
                    { new Guid("00000007-0000-0000-0000-000000000002"), true, "Segunda tela", 53.93m },
                    { new Guid("00000007-0000-0000-0000-000000000003"), true, "Celular", 118.64m },
                    { new Guid("00000007-0000-0000-0000-000000000004"), true, "Rastreador", 345.13m }
                });

            migrationBuilder.InsertData(
                table: "ModeloTrabalho",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { new Guid("00000004-0000-0000-0000-000000000001"), "Presencial" },
                    { new Guid("00000004-0000-0000-0000-000000000002"), "Híbrido" },
                    { new Guid("00000004-0000-0000-0000-000000000003"), "Remoto" }
                });

            migrationBuilder.InsertData(
                table: "ParametroSistema",
                columns: new[] { "Chave", "AlteradoPorId", "DataAlteracao", "Valor" },
                values: new object[] { "PrazoSlaDias", null, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "45" });

            migrationBuilder.InsertData(
                table: "Perfil",
                columns: new[] { "Id", "Descricao", "Nome" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000001"), "Criador/Admin: gerencia Gestores, parâmetros e auditoria", "Admin" },
                    { new Guid("00000001-0000-0000-0000-000000000002"), "Gestor do Contrato: valida, devolve e cancela demandas dos seus contratos", "Gestor" },
                    { new Guid("00000001-0000-0000-0000-000000000003"), "Usuário Solicitante: cria, envia e corrige demandas", "Solicitante" },
                    { new Guid("00000001-0000-0000-0000-000000000004"), "Funcionário SESI: conduz o processo das demandas do seu contrato", "FuncionarioSesi" }
                });

            migrationBuilder.InsertData(
                table: "QqpRegiao",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { new Guid("0000000a-0000-0000-0000-000000000001"), "QQP NORTE" },
                    { new Guid("0000000a-0000-0000-0000-000000000002"), "QQP SUDESTE" }
                });

            migrationBuilder.InsertData(
                table: "Status",
                columns: new[] { "Id", "Ativo", "Nome" },
                values: new object[,]
                {
                    { new Guid("00000003-0000-0000-0000-000000000001"), true, "Em análise" },
                    { new Guid("00000003-0000-0000-0000-000000000002"), true, "Aguardando correção" },
                    { new Guid("00000003-0000-0000-0000-000000000003"), true, "Aguardando responsável" },
                    { new Guid("00000003-0000-0000-0000-000000000004"), true, "Em andamento" },
                    { new Guid("00000003-0000-0000-0000-000000000005"), true, "Concluído" },
                    { new Guid("00000003-0000-0000-0000-000000000006"), true, "Cancelado" }
                });

            migrationBuilder.InsertData(
                table: "TipoDemanda",
                columns: new[] { "Id", "Ativo", "Nome" },
                values: new object[] { new Guid("00000005-0000-0000-0000-000000000001"), true, "Nova contratação" });

            migrationBuilder.InsertData(
                table: "Veiculo",
                columns: new[] { "Id", "Ativo", "Tipo", "Valor" },
                values: new object[,]
                {
                    { new Guid("00000006-0000-0000-0000-000000000001"), true, "Veículo 4x4", 9113.47m },
                    { new Guid("00000006-0000-0000-0000-000000000002"), true, "Veículo de passeio", 5292.29m },
                    { new Guid("00000006-0000-0000-0000-000000000003"), true, "Veículo van", 18874.06m },
                    { new Guid("00000006-0000-0000-0000-000000000004"), true, "Transporte", 539.26m }
                });

            migrationBuilder.InsertData(
                table: "Contrato",
                columns: new[] { "Id", "Ativo", "ContratadaId", "Numero" },
                values: new object[,]
                {
                    { new Guid("00000009-0000-0000-0000-000000000001"), true, new Guid("00000008-0000-0000-0000-000000000001"), "5900125082" },
                    { new Guid("00000009-0000-0000-0000-000000000002"), true, new Guid("00000008-0000-0000-0000-000000000001"), "5900118506" }
                });

            migrationBuilder.InsertData(
                table: "Corredor",
                columns: new[] { "Id", "Ativo", "ContratoId", "Nome", "RegiaoId" },
                values: new object[,]
                {
                    { new Guid("0000000b-0000-0000-0000-000000000001"), true, new Guid("00000009-0000-0000-0000-000000000001"), "Norte", new Guid("0000000a-0000-0000-0000-000000000001") },
                    { new Guid("0000000b-0000-0000-0000-000000000002"), true, new Guid("00000009-0000-0000-0000-000000000001"), "Pelotização", new Guid("0000000a-0000-0000-0000-000000000001") },
                    { new Guid("0000000b-0000-0000-0000-000000000003"), true, new Guid("00000009-0000-0000-0000-000000000001"), "C. Integrado", new Guid("0000000a-0000-0000-0000-000000000001") },
                    { new Guid("0000000b-0000-0000-0000-000000000004"), true, new Guid("00000009-0000-0000-0000-000000000002"), "Sudeste", new Guid("0000000a-0000-0000-0000-000000000002") },
                    { new Guid("0000000b-0000-0000-0000-000000000005"), true, new Guid("00000009-0000-0000-0000-000000000002"), "Sul", new Guid("0000000a-0000-0000-0000-000000000002") },
                    { new Guid("0000000b-0000-0000-0000-000000000006"), true, new Guid("00000009-0000-0000-0000-000000000002"), "Pelotização", new Guid("0000000a-0000-0000-0000-000000000002") },
                    { new Guid("0000000b-0000-0000-0000-000000000007"), true, new Guid("00000009-0000-0000-0000-000000000002"), "C. Integrado", new Guid("0000000a-0000-0000-0000-000000000002") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anexo_Demanda",
                table: "Anexo",
                column: "DemandaId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexo_EtapaId",
                table: "Anexo",
                column: "EtapaId");

            migrationBuilder.CreateIndex(
                name: "IX_Anexo_UsuarioUploadId",
                table: "Anexo",
                column: "UsuarioUploadId");

            migrationBuilder.CreateIndex(
                name: "UQ_Contratada_CNPJ",
                table: "Contratada",
                column: "CNPJ",
                unique: true,
                filter: "[CNPJ] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contrato_ContratadaId",
                table: "Contrato",
                column: "ContratadaId");

            migrationBuilder.CreateIndex(
                name: "UQ_Contrato_Numero",
                table: "Contrato",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Corredor_ContratoId",
                table: "Corredor",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Corredor_RegiaoId",
                table: "Corredor",
                column: "RegiaoId");

            migrationBuilder.CreateIndex(
                name: "UQ_Corredor_NomeRegiao",
                table: "Corredor",
                columns: new[] { "Nome", "RegiaoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_ContratadaId",
                table: "Demanda",
                column: "ContratadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_ContratoId",
                table: "Demanda",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_EtapaStatus",
                table: "Demanda",
                columns: new[] { "EtapaAtualId", "StatusAtualId" });

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_Gestor",
                table: "Demanda",
                column: "GestorId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_LimiteSla",
                table: "Demanda",
                column: "DataLimiteSLA");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_ResponsavelSesi",
                table: "Demanda",
                column: "ResponsavelSesiId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_Solicitante",
                table: "Demanda",
                column: "UsuarioSolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_StatusAtualId",
                table: "Demanda",
                column: "StatusAtualId");

            migrationBuilder.CreateIndex(
                name: "UQ_Demanda_Numero",
                table: "Demanda",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Etapa_Nome",
                table: "Etapa",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Etapa_Ordem",
                table: "Etapa",
                column: "Ordem",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EtapaDemanda_Demanda",
                table: "EtapaDemanda",
                column: "DemandaId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapaDemanda_EtapaId",
                table: "EtapaDemanda",
                column: "EtapaId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapaDemanda_StatusId",
                table: "EtapaDemanda",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapaDemanda_UsuarioResponsavelId",
                table: "EtapaDemanda",
                column: "UsuarioResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_GestorContrato_ContratoId",
                table: "GestorContrato",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistAlt_Demanda",
                table: "HistoricoAlteracao",
                columns: new[] { "DemandaId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAlteracao_UsuarioId",
                table: "HistoricoAlteracao",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistDemanda_Demanda",
                table: "HistoricoDemanda",
                columns: new[] { "DemandaId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoDemanda_EtapaAnteriorId",
                table: "HistoricoDemanda",
                column: "EtapaAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoDemanda_EtapaNovaId",
                table: "HistoricoDemanda",
                column: "EtapaNovaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoDemanda_StatusAnteriorId",
                table: "HistoricoDemanda",
                column: "StatusAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoDemanda_StatusNovoId",
                table: "HistoricoDemanda",
                column: "StatusNovoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoDemanda_UsuarioId",
                table: "HistoricoDemanda",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UQ_ItemEquipamento_Nome",
                table: "ItemEquipamento",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemQqp_Busca",
                table: "ItemQqp",
                columns: new[] { "RegiaoId", "FuncaoId", "ClassificacaoId", "NivelId", "CargaHorariaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemQqp_CargaHorariaId",
                table: "ItemQqp",
                column: "CargaHorariaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQqp_ClassificacaoId",
                table: "ItemQqp",
                column: "ClassificacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQqp_FuncaoId",
                table: "ItemQqp",
                column: "FuncaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQqp_NivelId",
                table: "ItemQqp",
                column: "NivelId");

            migrationBuilder.CreateIndex(
                name: "UQ_ItemQqp_Codigo",
                table: "ItemQqp",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Log_Entidade",
                table: "LogAuditoria",
                columns: new[] { "Entidade", "EntidadeId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_LogAuditoria_UsuarioId",
                table: "LogAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UQ_ModeloTrabalho_Nome",
                table: "ModeloTrabalho",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParametroSistema_AlteradoPorId",
                table: "ParametroSistema",
                column: "AlteradoPorId");

            migrationBuilder.CreateIndex(
                name: "UQ_Perfil_Nome",
                table: "Perfil",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_QqpCargaHoraria",
                table: "QqpCargaHoraria",
                column: "HorasSemanais",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_QqpClassificacao_Nome",
                table: "QqpClassificacao",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_QqpFuncao_Nome",
                table: "QqpFuncao",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_QqpNivel_Nome",
                table: "QqpNivel",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_QqpRegiao_Nome",
                table: "QqpRegiao",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Rac_Codigo",
                table: "Rac",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Correcao_Demanda",
                table: "SolicitacaoCorrecao",
                column: "DemandaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoCorrecao_SolicitadoPorId",
                table: "SolicitacaoCorrecao",
                column: "SolicitadoPorId");

            migrationBuilder.CreateIndex(
                name: "UQ_Status_Nome",
                table: "Status",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TipoDemanda_Nome",
                table: "TipoDemanda",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_ContratoId",
                table: "Usuario",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_CriadoPorUsuarioId",
                table: "Usuario",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_GestorResponsavelId",
                table: "Usuario",
                column: "GestorResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PerfilId",
                table: "Usuario",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "UQ_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Usuario_Login",
                table: "Usuario",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vaga_UsuarioResponsavelId",
                table: "Vaga",
                column: "UsuarioResponsavelId");

            migrationBuilder.CreateIndex(
                name: "UQ_Vaga_Demanda",
                table: "Vaga",
                column: "DemandaId",
                unique: true,
                filter: "[DemandaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_Veiculo_Tipo",
                table: "Veiculo",
                column: "Tipo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anexo");

            migrationBuilder.DropTable(
                name: "Corredor");

            migrationBuilder.DropTable(
                name: "EtapaDemanda");

            migrationBuilder.DropTable(
                name: "GerenteExecutivo");

            migrationBuilder.DropTable(
                name: "GestorContrato");

            migrationBuilder.DropTable(
                name: "HistoricoAlteracao");

            migrationBuilder.DropTable(
                name: "HistoricoDemanda");

            migrationBuilder.DropTable(
                name: "ItemEquipamento");

            migrationBuilder.DropTable(
                name: "ItemQqp");

            migrationBuilder.DropTable(
                name: "LogAuditoria");

            migrationBuilder.DropTable(
                name: "ModeloTrabalho");

            migrationBuilder.DropTable(
                name: "ParametroSistema");

            migrationBuilder.DropTable(
                name: "Rac");

            migrationBuilder.DropTable(
                name: "SequenciaNumeroDemanda");

            migrationBuilder.DropTable(
                name: "SolicitacaoCorrecao");

            migrationBuilder.DropTable(
                name: "TipoDemanda");

            migrationBuilder.DropTable(
                name: "Vaga");

            migrationBuilder.DropTable(
                name: "Veiculo");

            migrationBuilder.DropTable(
                name: "QqpCargaHoraria");

            migrationBuilder.DropTable(
                name: "QqpClassificacao");

            migrationBuilder.DropTable(
                name: "QqpFuncao");

            migrationBuilder.DropTable(
                name: "QqpNivel");

            migrationBuilder.DropTable(
                name: "QqpRegiao");

            migrationBuilder.DropTable(
                name: "Demanda");

            migrationBuilder.DropTable(
                name: "Etapa");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Status");

            migrationBuilder.DropTable(
                name: "Contrato");

            migrationBuilder.DropTable(
                name: "Perfil");

            migrationBuilder.DropTable(
                name: "Contratada");
        }
    }
}
