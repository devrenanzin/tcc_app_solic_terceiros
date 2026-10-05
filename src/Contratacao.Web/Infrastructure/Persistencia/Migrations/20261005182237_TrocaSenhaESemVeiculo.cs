using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Contratacao.Web.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    internal partial class TrocaSenhaESemVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Veiculo");

            migrationBuilder.DeleteData(
                table: "ItemEquipamento",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0000-000000000004"));

            migrationBuilder.AddColumn<bool>(
                name: "DeveTrocarSenha",
                table: "Usuario",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeveTrocarSenha",
                table: "Usuario");

            migrationBuilder.CreateTable(
                name: "Veiculo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculo", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ItemEquipamento",
                columns: new[] { "Id", "Ativo", "Nome", "Valor" },
                values: new object[] { new Guid("00000007-0000-0000-0000-000000000004"), true, "Rastreador", 345.13m });

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

            migrationBuilder.CreateIndex(
                name: "UQ_Veiculo_Tipo",
                table: "Veiculo",
                column: "Tipo",
                unique: true);
        }
    }
}
