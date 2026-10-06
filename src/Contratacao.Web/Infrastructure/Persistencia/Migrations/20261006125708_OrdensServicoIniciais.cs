using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Contratacao.Web.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    internal partial class OrdensServicoIniciais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "OrdemServico",
                columns: new[] { "Id", "Ativo", "ContratoId", "Numero" },
                values: new object[,]
                {
                    { new Guid("0000000c-0000-0000-0000-000000000001"), true, new Guid("00000009-0000-0000-0000-000000000001"), "01" },
                    { new Guid("0000000c-0000-0000-0000-000000000002"), true, new Guid("00000009-0000-0000-0000-000000000001"), "02" },
                    { new Guid("0000000c-0000-0000-0000-000000000003"), true, new Guid("00000009-0000-0000-0000-000000000001"), "03" },
                    { new Guid("0000000c-0000-0000-0000-000000000004"), true, new Guid("00000009-0000-0000-0000-000000000001"), "04" },
                    { new Guid("0000000c-0000-0000-0000-000000000005"), true, new Guid("00000009-0000-0000-0000-000000000001"), "05" },
                    { new Guid("0000000c-0000-0000-0000-000000000006"), true, new Guid("00000009-0000-0000-0000-000000000001"), "06" },
                    { new Guid("0000000c-0000-0000-0000-000000000007"), true, new Guid("00000009-0000-0000-0000-000000000001"), "07" },
                    { new Guid("0000000c-0000-0000-0000-000000000008"), true, new Guid("00000009-0000-0000-0000-000000000001"), "08" },
                    { new Guid("0000000c-0000-0000-0000-000000000009"), true, new Guid("00000009-0000-0000-0000-000000000001"), "09" },
                    { new Guid("0000000c-0000-0000-0000-000000000010"), true, new Guid("00000009-0000-0000-0000-000000000001"), "10" },
                    { new Guid("0000000c-0000-0000-0000-000000000011"), true, new Guid("00000009-0000-0000-0000-000000000002"), "11" },
                    { new Guid("0000000c-0000-0000-0000-000000000012"), true, new Guid("00000009-0000-0000-0000-000000000002"), "12" },
                    { new Guid("0000000c-0000-0000-0000-000000000013"), true, new Guid("00000009-0000-0000-0000-000000000002"), "13" },
                    { new Guid("0000000c-0000-0000-0000-000000000014"), true, new Guid("00000009-0000-0000-0000-000000000002"), "14" },
                    { new Guid("0000000c-0000-0000-0000-000000000015"), true, new Guid("00000009-0000-0000-0000-000000000002"), "15" },
                    { new Guid("0000000c-0000-0000-0000-000000000016"), true, new Guid("00000009-0000-0000-0000-000000000002"), "16" },
                    { new Guid("0000000c-0000-0000-0000-000000000017"), true, new Guid("00000009-0000-0000-0000-000000000002"), "17" },
                    { new Guid("0000000c-0000-0000-0000-000000000018"), true, new Guid("00000009-0000-0000-0000-000000000002"), "18" },
                    { new Guid("0000000c-0000-0000-0000-000000000019"), true, new Guid("00000009-0000-0000-0000-000000000002"), "19" },
                    { new Guid("0000000c-0000-0000-0000-000000000020"), true, new Guid("00000009-0000-0000-0000-000000000002"), "20" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "OrdemServico",
                keyColumn: "Id",
                keyValue: new Guid("0000000c-0000-0000-0000-000000000020"));
        }
    }
}
