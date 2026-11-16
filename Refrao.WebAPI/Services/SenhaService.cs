namespace Refrao.WebAPI.Services;

/// <summary>Hash de senha com BCrypt (mesmo pacote usado no projeto Plot!).</summary>
public static class SenhaService
{
    public static string GerarHash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public static bool Verificar(string senhaInformada, string hashBanco)
    {
        if (string.IsNullOrEmpty(senhaInformada) || string.IsNullOrEmpty(hashBanco)) return false;
        if (!hashBanco.StartsWith("$2", StringComparison.Ordinal)) return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(senhaInformada, hashBanco);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
