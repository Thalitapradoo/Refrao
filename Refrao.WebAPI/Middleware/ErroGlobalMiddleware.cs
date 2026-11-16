using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Middleware;

/// <summary>
/// Captura erros não tratados e devolve uma resposta JSON clara.
/// Falhas de conexão com o SQL Server viram 503 (serviço indisponível).
/// </summary>
public class ErroGlobalMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErroGlobalMiddleware> _logger;

    public ErroGlobalMiddleware(RequestDelegate next, ILogger<ErroGlobalMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception erro) when (!context.Response.HasStarted)
        {
            var problemaBanco = erro is SqlException
                || erro.GetBaseException() is SqlException
                || (erro is InvalidOperationException && erro.InnerException is SqlException)
                || erro is DbUpdateException;

            int status;
            string titulo;
            if (problemaBanco && erro is not DbUpdateException)
            {
                status = StatusCodes.Status503ServiceUnavailable;
                titulo = "Não foi possível acessar o banco de dados. Verifique se o SQL Server está em execução e tente novamente.";
                _logger.LogError(erro, "Erro de conexão com o banco de dados.");
            }
            else if (erro is DbUpdateException)
            {
                status = StatusCodes.Status409Conflict;
                titulo = "A operação não pôde ser concluída porque viola uma regra do banco de dados.";
                _logger.LogWarning(erro, "Erro ao salvar no banco de dados.");
            }
            else
            {
                status = StatusCodes.Status500InternalServerError;
                titulo = "Ocorreu um erro inesperado no servidor.";
                _logger.LogError(erro, "Erro não tratado.");
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { status, title = titulo },
                options: null, contentType: "application/problem+json");
        }
    }
}
