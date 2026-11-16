using System.ComponentModel.DataAnnotations;

namespace Refrao.WebAPI.DTO;

public class LoginDTO
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    public string Senha { get; set; } = string.Empty;
}

/// <summary>Cadastro público: sempre cria um usuário comum (não existe campo de perfil).</summary>
public class RegistroDTO
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Senha), ErrorMessage = "A confirmação de senha não confere.")]
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}

public record LoginRespostaDTO(string Token, DateTime ExpiracaoUtc, UsuarioRespostaDTO Usuario);
