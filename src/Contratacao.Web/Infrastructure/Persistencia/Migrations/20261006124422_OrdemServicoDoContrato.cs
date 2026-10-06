using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratacao.Web.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    internal partial class OrdemServicoDoContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContratoOs",
                table: "Demanda");

            migrationBuilder.AddColumn<Guid>(
                name: "OrdemServicoId",
                table: "Demanda",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "OrdemServico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContratoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdemServico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdemServico_Contrato",
                        column: x => x.ContratoId,
                        principalTable: "Contrato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Demanda_OrdemServicoId",
                table: "Demanda",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "UQ_OrdemServico_ContratoNumero",
                table: "OrdemServico",
                columns: new[] { "ContratoId", "Numero" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Demanda_OrdemServico",
                table: "Demanda",
                column: "OrdemServicoId",
                principalTable: "OrdemServico",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Demanda_OrdemServico",
                table: "Demanda");

            migrationBuilder.DropTable(
                name: "OrdemServico");

            migrationBuilder.DropIndex(
                name: "IX_Demanda_OrdemServicoId",
                table: "Demanda");

            migrationBuilder.DropColumn(
                name: "OrdemServicoId",
                table: "Demanda");

            migrationBuilder.AddColumn<string>(
                name: "ContratoOs",
                table: "Demanda",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");
        }
    }
}
