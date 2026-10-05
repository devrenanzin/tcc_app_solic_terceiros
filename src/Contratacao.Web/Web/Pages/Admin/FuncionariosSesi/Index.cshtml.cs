using Contratacao.Web.Application;
using Contratacao.Web.Domain.Usuarios;

namespace Contratacao.Web.Web.Pages.Admin.FuncionariosSesi;

/// <summary>Todos os Funcionários SESI, com Gestor e contrato; o Admin troca o contrato de qualquer um.</summary>
internal sealed class IndexModel(IUsuarios usuarios) : PaginaBase
{
    internal IReadOnlyList<ResumoUsuario> Funcionarios { get; private set; } = [];

    public async Task OnGetAsync() => Funcionarios = await usuarios.ListarAsync(Perfil.FuncionarioSesi, null, Cancelamento);
}
