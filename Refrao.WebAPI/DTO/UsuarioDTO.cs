using System.ComponentModel.DataAnnotations;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.DTO;

/// <summary>Cadastro feito pelo administrador (pode escolher o perfil).</summary>
public class UsuarioCadastroDTO
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o perfil.")]
    [RegularExpression("^(Administrador|Usuario)$", ErrorMessage = "Perfil deve ser Administrador ou Usuario.")]
    public string Perfil { get; set; } = "Usuario";
}

public class UsuarioAtualizacaoDTO
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Obrigatória quando o próprio usuário troca a senha.</summary>
    public string? SenhaAtual { get; set; }

    /// <summary>Opcional: preencha só para trocar a senha.</summary>
    [MinLength(8, ErrorMessage = "A nova senha deve ter pelo menos 8 caracteres.")]
    public string? NovaSenha { get; set; }

    /// <summary>Somente administrador.</summary>
    [RegularExpression("^(Administrador|Usuario)$", ErrorMessage = "Perfil deve ser Administrador ou Usuario.")]
    public string? Perfil { get; set; }

    /// <summary>Somente administrador.</summary>
    public bool? Ativo { get; set; }
}

public record UsuarioRespostaDTO(int Id, string Nome, string Email, string Perfil, DateTime DataCadastro, bool Ativo)
{
    public static UsuarioRespostaDTO Converter(Usuario u) => new(u.Id, u.Nome, u.Email, u.Perfil, u.DataCadastro, u.Ativo);
}
