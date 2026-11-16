using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Configuracoes;

/// <summary>Identifica erros do SQL Server para devolver o status HTTP adequado.</summary>
public static class ErrosBanco
{
    /// <summary>2601/2627: violação de UNIQUE (registro duplicado).</summary>
    public static bool Duplicado(DbUpdateException erro) =>
        erro.GetBaseException() is SqlException { Number: 2601 or 2627 };

    /// <summary>547: violação de FOREIGN KEY ou CHECK.</summary>
    public static bool Restricao(DbUpdateException erro) =>
        erro.GetBaseException() is SqlException { Number: 547 };
}
