using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratacao.Web.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    internal partial class LoginEmailEPerfilIpNasAlteracoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "Usuario",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            // O login passa a ser o e-mail completo (Cliente).
            migrationBuilder.Sql("UPDATE Usuario SET Login = Email");

            // HistoricoAlteracao ainda não tem linhas; o valor padrão "" nunca chega a ser gravado.
            migrationBuilder.AddColumn<string>(
                name: "EnderecoIp",
                table: "HistoricoAlteracao",
                type: "varchar(45)",
                unicode: false,
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerfilUsuario",
                table: "HistoricoAlteracao",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnderecoIp",
                table: "HistoricoAlteracao");

            migrationBuilder.DropColumn(
                name: "PerfilUsuario",
                table: "HistoricoAlteracao");

            // Volta ao login anterior (parte antes do @) para caber em nvarchar(100).
            migrationBuilder.Sql("UPDATE Usuario SET Login = LEFT(Email, CHARINDEX('@', Email) - 1)");

            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "Usuario",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);
        }
    }
}
