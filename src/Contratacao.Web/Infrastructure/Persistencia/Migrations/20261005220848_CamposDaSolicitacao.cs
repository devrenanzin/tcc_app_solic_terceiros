using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratacao.Web.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    internal partial class CamposDaSolicitacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreaSolicitante",
                table: "Demanda",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoriaCnh",
                table: "Demanda",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Celular",
                table: "Demanda",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ColetorCusto",
                table: "Demanda",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContratoOs",
                table: "Demanda",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CorredorId",
                table: "Demanda",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "CustoTotal",
                table: "Demanda",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DescricaoAtividades",
                table: "Demanda",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ExigeCnh",
                table: "Demanda",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FiscalEfetivoEmail",
                table: "Demanda",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FiscalEfetivoNome",
                table: "Demanda",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Formacao",
                table: "Demanda",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GerenteExecutivoId",
                table: "Demanda",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ItemQqpId",
                table: "Demanda",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "LocalidadeVaga",
                table: "Demanda",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ModeloTrabalhoId",
                table: "Demanda",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "Notebook",
                table: "Demanda",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "Demanda",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "PeriodoTemporarioMeses",
                table: "Demanda",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PisoSalarialQqp",
                table: "Demanda",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoUnitarioQqp",
                table: "Demanda",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<short>(
                name: "QuantidadeSolicitada",
                table: "Demanda",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelEfetivoEmail",
                table: "Demanda",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelEfetivoNome",
                table: "Demanda",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SegundaTela",
                table: "Demanda",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Temporaria",
                table: "Demanda",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TipoDemandaId",
                table: "Demanda",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "ValorEquipamentosPorPessoa",
                table: "Demanda",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DemandaRac",
                columns: table => new
                {
                    RacId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandaRac", x => new { x.DemandaId, x.RacId });
                    table.ForeignKey(
                        name: "FK_DemandaRac_Demanda",
                        column: x => x.DemandaId,
                        principalTable: "Demanda",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DemandaRac_Rac",
                        column: x => x.RacId,
                        principalTable: "Rac",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_CorredorId",
                table: "Demanda",
                column: "CorredorId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_GerenteExecutivoId",
                table: "Demanda",
                column: "GerenteExecutivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_ItemQqpId",
                table: "Demanda",
                column: "ItemQqpId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_ModeloTrabalhoId",
                table: "Demanda",
                column: "ModeloTrabalhoId");

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_TipoDemandaId",
                table: "Demanda",
                column: "TipoDemandaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Demanda_CategoriaCnh",
                table: "Demanda",
                sql: "CategoriaCnh IN ('A','B','C','D','E','AB','AC','AD','AE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Demanda_Cnh",
                table: "Demanda",
                sql: "ExigeCnh = 0 OR CategoriaCnh IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Demanda_Quantidade",
                table: "Demanda",
                sql: "QuantidadeSolicitada > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Demanda_Temporaria",
                table: "Demanda",
                sql: "Temporaria = 0 OR PeriodoTemporarioMeses > 0");

            migrationBuilder.CreateIndex(
                name: "IX_DemandaRac_RacId",
                table: "DemandaRac",
                column: "RacId");

            migrationBuilder.AddForeignKey(
                name: "FK_Demanda_Corredor",
                table: "Demanda",
                column: "CorredorId",
                principalTable: "Corredor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Demanda_GerenteExecutivo",
                table: "Demanda",
                column: "GerenteExecutivoId",
                principalTable: "GerenteExecutivo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Demanda_ItemQqp",
                table: "Demanda",
                column: "ItemQqpId",
                principalTable: "ItemQqp",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Demanda_ModeloTrabalho",
                table: "Demanda",
                column: "ModeloTrabalhoId",
                principalTable: "ModeloTrabalho",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Demanda_TipoDemanda",
                table: "Demanda",
                column: "TipoDemandaId",
                principalTable: "TipoDemanda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Demanda_Corredor",
                table: "Demanda");

            migrationBuilder.DropForeignKey(
                name: "FK_Demanda_GerenteExecutivo",
                table: "Demanda");

            migrationBuilder.DropForeignKey(
                name: "FK_Demanda_ItemQqp",
                table: "Demanda");

            migrationBuilder.DropForeignKey(
                name: "FK_Demanda_ModeloTrabalho",
                table: "Demanda");

            migrationBuilder.DropForeignKey(
                name: "FK_Demanda_TipoDemanda",
                table: "Demanda");

            migrationBuilder.DropTable(
                name: "DemandaRac");

            migrationBuilder.DropIndex(
                name: "IX_Demanda_CorredorId",
                table: "Demanda");

            migrationBuilder.DropIndex(
                name: "IX_Demanda_GerenteExecutivoId",
                table: "Demanda");

            migrationBuilder.DropIndex(
                name: "IX_Demanda_ItemQqpId",
                table: "Demanda");

            migrationBuilder.DropIndex(
                name: "IX_Demanda_ModeloTrabalhoId",
                table: "Demanda");

            migrationBuilder.DropIndex(
                name: "IX_Demanda_TipoDemandaId",
                table: "Demanda");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Demanda_CategoriaCnh",
                table: "Demanda");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Demanda_Cnh",
                table: "Demanda");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Demanda_Quantidade",
                table: "Demanda");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Demanda_Temporaria",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "AreaSolicitante",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "CategoriaCnh",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "Celular",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "ColetorCusto",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "ContratoOs",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "CorredorId",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "CustoTotal",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "DescricaoAtividades",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "ExigeCnh",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "FiscalEfetivoEmail",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "FiscalEfetivoNome",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "Formacao",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "GerenteExecutivoId",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "ItemQqpId",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "LocalidadeVaga",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "ModeloTrabalhoId",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "Notebook",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "PeriodoTemporarioMeses",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "PisoSalarialQqp",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "PrecoUnitarioQqp",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "QuantidadeSolicitada",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "ResponsavelEfetivoEmail",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "ResponsavelEfetivoNome",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "SegundaTela",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "Temporaria",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "TipoDemandaId",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "ValorEquipamentosPorPessoa",
                table: "Demanda");
        }
    }
}
