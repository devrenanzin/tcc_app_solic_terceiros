using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratacao.Web.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    internal partial class GerenciadoraERiscos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Códigos dos riscos já carregados do CSV: "RAC 01" passa a "RISCO 01" (Cliente, revisão de 06/10/2026).
            migrationBuilder.Sql("UPDATE Rac SET Codigo = 'RISCO' + SUBSTRING(Codigo, 4, 10) WHERE Codigo LIKE 'RAC %'");

            migrationBuilder.UpdateData(
                table: "Contratada",
                keyColumn: "Id",
                keyValue: new Guid("00000008-0000-0000-0000-000000000001"),
                column: "RazaoSocial",
                value: "Gerenciadora");

            migrationBuilder.UpdateData(
                table: "Etapa",
                keyColumn: "Id",
                keyValue: new Guid("00000002-0000-0000-0000-000000000003"),
                column: "Nome",
                value: "Validação da Gerenciadora");

            migrationBuilder.UpdateData(
                table: "Perfil",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000004"),
                column: "Descricao",
                value: "Funcionário da Gerenciadora: conduz o processo das demandas do seu contrato");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Rac SET Codigo = 'RAC' + SUBSTRING(Codigo, 6, 10) WHERE Codigo LIKE 'RISCO %'");

            migrationBuilder.UpdateData(
                table: "Contratada",
                keyColumn: "Id",
                keyValue: new Guid("00000008-0000-0000-0000-000000000001"),
                column: "RazaoSocial",
                value: "SESI");

            migrationBuilder.UpdateData(
                table: "Etapa",
                keyColumn: "Id",
                keyValue: new Guid("00000002-0000-0000-0000-000000000003"),
                column: "Nome",
                value: "Validação SESI");

            migrationBuilder.UpdateData(
                table: "Perfil",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000004"),
                column: "Descricao",
                value: "Funcionário SESI: conduz o processo das demandas do seu contrato");
        }
    }
}
