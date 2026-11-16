using Microsoft.IdentityModel.Tokens;
using Refrao.WebAPI.Configuracoes;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Refrao.WebAPI.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwt;

    public TokenService(JwtSettings jwt)
    {
        _jwt = jwt;
    }

    public LoginRespostaDTO GerarToken(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
            new Claim(ClaimTypes.Role, usuario.Perfil),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiracao = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiracao,
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));

        return new LoginRespostaDTO(new JwtSecurityTokenHandler().WriteToken(token), expiracao, UsuarioRespostaDTO.Converter(usuario));
    }
}
