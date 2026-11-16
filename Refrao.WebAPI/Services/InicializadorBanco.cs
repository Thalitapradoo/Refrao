using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Refrao.WebAPI.Services;

/// <summary>
/// Prepara o banco RefrãoDB na inicialização da API:
/// 1) se o banco não existe, executa Database/RefrãoDatabase.sql (criação + dados iniciais);
/// 2) sempre executa Database/RefrãoAtualizacao_v2.sql, que só aplica o que ainda faltar
///    (colunas do refrão, comentários e sugestões) — assim quem já tinha o banco recebe a v2.
/// Desligue com "Banco:CriarAutomaticamente": false no appsettings.json.
/// </summary>
public static class InicializadorBanco
{
    private const string NomeBanco = "RefrãoDB";

    public static async Task CriarSeNecessario(IConfiguration configuracao, ILogger logger)
    {
        if (!configuracao.GetValue("Banco:CriarAutomaticamente", false)) return;

        var conexaoTexto = configuracao.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Configure a ConnectionString DefaultConnection.");

        var construtor = new SqlConnectionStringBuilder(conexaoTexto);
        if (!string.Equals(construtor.InitialCatalog, NomeBanco, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Preparação automática ignorada: a conexão aponta para {Banco}, mas os scripts usam {Script}.",
                construtor.InitialCatalog, NomeBanco);
            return;
        }

        construtor.InitialCatalog = "master";
        await using var conexao = new SqlConnection(construtor.ConnectionString);
        await conexao.OpenAsync();

        bool existe;
        await using (var verificar = new SqlCommand("SELECT DB_ID(@nome)", conexao))
        {
            verificar.Parameters.AddWithValue("@nome", NomeBanco);
            var resultado = await verificar.ExecuteScalarAsync();
            existe = resultado is not null && resultado is not DBNull;
        }

        if (!existe)
        {
            logger.LogInformation("Criando o banco {Banco} a partir do script...", NomeBanco);
            try
            {
                await ExecutarScript(conexao, "RefraoDatabase.sql");
            }
            catch
            {
                await DesfazerCriacao(conexao, logger);
                throw;
            }
            logger.LogInformation("Banco {Banco} criado e populado com sucesso.", NomeBanco);
        }

        await ExecutarScript(conexao, "RefraoAtualizacao_v2.sql");
        logger.LogInformation("Banco {Banco} pronto (versão 2: refrão, comentários e sugestões).", NomeBanco);
    }

    private static async Task ExecutarScript(SqlConnection conexao, string arquivo)
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Database", arquivo);
        if (!File.Exists(caminho))
            throw new FileNotFoundException("Script do banco não encontrado.", caminho);

        var script = await File.ReadAllTextAsync(caminho);
        var lotes = Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        foreach (var lote in lotes)
        {
            if (string.IsNullOrWhiteSpace(lote)) continue;
            await using var comando = new SqlCommand(lote, conexao) { CommandTimeout = 180 };
            await comando.ExecuteNonQueryAsync();
        }

        await using var voltar = new SqlCommand("USE master;", conexao);
        await voltar.ExecuteNonQueryAsync();
    }

    private static async Task DesfazerCriacao(SqlConnection conexao, ILogger logger)
    {
        try
        {
            await using var desfazer = new SqlCommand(
                $"USE master; IF DB_ID(N'{NomeBanco}') IS NOT NULL BEGIN " +
                $"ALTER DATABASE [{NomeBanco}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{NomeBanco}]; END", conexao);
            await desfazer.ExecuteNonQueryAsync();
        }
        catch (Exception erro)
        {
            logger.LogWarning(erro, "Não foi possível remover o banco criado parcialmente.");
        }
    }
}
