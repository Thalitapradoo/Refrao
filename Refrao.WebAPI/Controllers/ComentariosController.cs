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

/// <summary>"Por que esse refrão gruda": comentários dos usuários sobre as músicas.</summary>
[Route("api/[controller]")]
[ApiController]
public class ComentariosController : ControllerBase
{
    private readonly IComentarioService _comentarioService;

    public ComentariosController(IComentarioService comentarioService)
    {
        _comentarioService = comentarioService;
    }

    /// <summary>Lista os comentários de uma música (público).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int musicaId)
    {
        if (musicaId < 1) return BadRequest("Informe o musicaId.");
        if (!await _comentarioService.MusicaExiste(musicaId)) return NotFound("Música não encontrada.");
        return Ok(await _comentarioService.ListarPorMusica(musicaId));
    }

    /// <summary>Escreve um comentário (qualquer usuário logado; um por música).</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Cadastrar([FromBody] ComentarioDTO dto)
    {
        if (IdLogado() is not { } usuarioId) return Unauthorized();
        if (!await _comentarioService.MusicaExiste(dto.MusicaId)) return BadRequest("A música informada não existe.");

        int id;
        try
        {
            id = await _comentarioService.Cadastrar(new ComentarioRefrao
            {
                MusicaId = dto.MusicaId,
                UsuarioId = usuarioId,
                Texto = dto.Texto.Trim()
            });
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Você já comentou esta música. Edite o seu comentário.");
        }

        return Created($"/api/Comentarios?musicaId={dto.MusicaId}", await _comentarioService.BuscarResposta(id));
    }

    /// <summary>Edita o próprio comentário.</summary>
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Atualizar(int id, [FromBody] ComentarioEdicaoDTO dto)
    {
        var comentario = await _comentarioService.BuscarPorId(id);
        if (comentario is null) return NotFound("Comentário não encontrado.");
        if (comentario.UsuarioId != IdLogado()) return Forbid();

        await _comentarioService.Atualizar(id, dto.Texto.Trim());
        return NoContent();
    }

    /// <summary>Exclui um comentário (o autor ou um administrador).</summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Excluir(int id)
    {
        var comentario = await _comentarioService.BuscarPorId(id);
        if (comentario is null) return NotFound("Comentário não encontrado.");
        if (comentario.UsuarioId != IdLogado() && !User.IsInRole(Perfis.Administrador)) return Forbid();

        await _comentarioService.Excluir(id);
        return NoContent();
    }

    private int? IdLogado() =>
        int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
}
