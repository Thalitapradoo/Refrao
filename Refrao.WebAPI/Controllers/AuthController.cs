using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Configuracoes;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Refrao.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly ITokenService _tokenService;

    public AuthController(IUsuarioService usuarioService, ITokenService tokenService)
    {
        _usuarioService = usuarioService;
        _tokenService = tokenService;
    }

    /// <summary>Login com e-mail e senha. Devolve o token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        var usuario = await _usuarioService.Autenticar(dto.Email.Trim().ToLowerInvariant(), dto.Senha);
        if (usuario is null) return Unauthorized("E-mail ou senha inválidos.");
        if (!usuario.Ativo) return Unauthorized("Esta conta está desativada. Procure um administrador.");

        return Ok(_tokenService.GerarToken(usuario));
    }

    /// <summary>Cadastro público. Sempre cria um usuário com perfil "Usuario".</summary>
    [HttpPost("registrar")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar([FromBody] RegistroDTO dto)
    {
        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Perfil = Perfis.Usuario,
            Ativo = true
        };

        try
        {
            await _usuarioService.Cadastrar(usuario, dto.Senha);
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Já existe uma conta com esse e-mail.");
        }

        var criado = await _usuarioService.BuscarPorId(usuario.Id);
        return CreatedAtAction(nameof(Perfil), null, UsuarioRespostaDTO.Converter(criado ?? usuario));
    }

    /// <summary>Dados do usuário autenticado.</summary>
    [HttpGet("perfil")]
    [Authorize]
    public async Task<IActionResult> Perfil()
    {
        if (!int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)) return Unauthorized();
        var usuario = await _usuarioService.BuscarPorId(id);
        return usuario is null ? NotFound("Usuário não encontrado.") : Ok(UsuarioRespostaDTO.Converter(usuario));
    }
}
