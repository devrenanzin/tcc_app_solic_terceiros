using Contratacao.Web.Application.Demandas;
using Contratacao.Web.Application.Usuarios;

namespace Contratacao.Web.Application;

internal static class ConfiguracaoAplicacao
{
    internal static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        services.AddScoped<AutenticarUsuario>();
        services.AddScoped<CadastrarSolicitante>();
        services.AddScoped<ObterAtor>();
        services.AddScoped<TrocarSenha>();
        services.AddScoped<CadastrarGestor>();
        services.AddScoped<CadastrarFuncionarioSesi>();
        services.AddScoped<AlterarSituacaoUsuario>();
        services.AddScoped<DefinirContratosGestor>();
        services.AddScoped<AlterarContratoFuncionarioSesi>();
        services.AddScoped<TransferirVinculo>();

        services.AddScoped<ConsultarFormulario>();
        services.AddScoped<CalcularPreviaCusto>();
        services.AddScoped<EnviarDemanda>();
        services.AddScoped<CorrigirDemanda>();
        services.AddScoped<AprovarDemanda>();
        services.AddScoped<DevolverDemandaPeloGestor>();
        services.AddScoped<DevolverDemandaPeloSesi>();
        services.AddScoped<AceitarDemanda>();
        services.AddScoped<ConsultarDemandas>();
        services.AddScoped<ManterGerentesExecutivos>();
        return services;
    }
}
