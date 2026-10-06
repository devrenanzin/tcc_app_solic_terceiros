using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratacao.Web.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    internal partial class GerentesPorCorredor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GerenteExecutivoCorredor",
                columns: table => new
                {
                    CorredorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GerenteExecutivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GerenteExecutivoCorredor", x => new { x.GerenteExecutivoId, x.CorredorId });
                    table.ForeignKey(
                        name: "FK_GerenteCorredor_Corredor",
                        column: x => x.CorredorId,
                        principalTable: "Corredor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GerenteCorredor_Gerente",
                        column: x => x.GerenteExecutivoId,
                        principalTable: "GerenteExecutivo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GerenteExecutivoCorredor_CorredorId",
                table: "GerenteExecutivoCorredor",
                column: "CorredorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GerenteExecutivoCorredor");
        }
    }
}
