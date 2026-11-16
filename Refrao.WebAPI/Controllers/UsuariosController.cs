using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Configuracoes;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;
using Refrao.WebAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Refrao.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    /// <summary>Lista todos os usuários (somente administrador).</summary>
    [HttpGet]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Listar() =>
        Ok((await _usuarioService.Listar()).Select(UsuarioRespostaDTO.Converter));

    /// <summary>Consulta um usuário (o próprio usuário ou um administrador).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        if (!PodeAcessar(id)) return Forbid();
        var usuario = await _usuarioService.BuscarPorId(id);
        return usuario is null ? NotFound("Usuário não encontrado.") : Ok(UsuarioRespostaDTO.Converter(usuario));
    }

    /// <summary>Cadastra um usuário escolhendo o perfil (somente administrador).</summary>
    [HttpPost]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Cadastrar([FromBody] UsuarioCadastroDTO dto)
    {
        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Perfil = dto.Perfil,
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
        return CreatedAtAction(nameof(BuscarPorId), new { id = usuario.Id }, UsuarioRespostaDTO.Converter(criado ?? usuario));
    }

    /// <summary>
    /// Atualiza um usuário. O próprio usuário edita nome, e-mail e senha (informando a senha atual).
    /// Somente o administrador altera Perfil e Ativo, e pode editar outros usuários.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] UsuarioAtualizacaoDTO dto)
    {
        if (!PodeAcessar(id)) return Forbid();

        var atual = await _usuarioService.BuscarPorId(id);
        if (atual is null) return NotFound("Usuário não encontrado.");

        var ehAdmin = User.IsInRole(Perfis.Administrador);
        var ehProprio = IdLogado() == id;
        var novoPerfil = dto.Perfil ?? atual.Perfil;
        var novoAtivo = dto.Ativo ?? atual.Ativo;

        if (!ehAdmin && (novoPerfil != atual.Perfil || novoAtivo != atual.Ativo))
            return Forbid();

        if (ehAdmin && ehProprio && (novoPerfil != Perfis.Administrador || !novoAtivo))
            return BadRequest("Você não pode remover o seu próprio acesso de administrador nem desativar a própria conta.");

        if (!string.IsNullOrEmpty(dto.NovaSenha) && ehProprio
            && (string.IsNullOrEmpty(dto.SenhaAtual) || !SenhaService.Verificar(dto.SenhaAtual, atual.SenhaHash)))
            return BadRequest("Para trocar a senha, informe corretamente a senha atual.");

        try
        {
            await _usuarioService.Atualizar(id, dto.Nome.Trim(), dto.Email.Trim().ToLowerInvariant(),
                dto.NovaSenha, novoPerfil, novoAtivo);
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Já existe uma conta com esse e-mail.");
        }

        return NoContent();
    }

    /// <summary>Desativa um usuário (somente administrador). O registro é mantido no banco.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Desativar(int id)
    {
        if (IdLogado() == id) return BadRequest("Você não pode desativar a própria conta.");
        if (await _usuarioService.BuscarPorId(id) is null) return NotFound("Usuário não encontrado.");
        await _usuarioService.Desativar(id);
        return NoContent();
    }

    private int? IdLogado() =>
        int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    private bool PodeAcessar(int id) => User.IsInRole(Perfis.Administrador) || IdLogado() == id;
}
