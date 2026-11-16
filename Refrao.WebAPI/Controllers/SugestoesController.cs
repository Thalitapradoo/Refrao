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

/// <summary>
/// Sugestões de músicas. Qualquer usuário logado sugere; só o administrador aprova
/// (a música entra no catálogo) ou recusa. O usuário comum nunca cadastra música direto.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SugestoesController : ControllerBase
{
    private static readonly string[] StatusValidos = { SugestaoService.Pendente, SugestaoService.Aprovada, SugestaoService.Recusada };

    private readonly ISugestaoService _sugestaoService;
    private readonly IArtistaService _artistaService;
    private readonly IGeneroService _generoService;
    private readonly IAlbumService _albumService;

    public SugestoesController(ISugestaoService sugestaoService, IArtistaService artistaService,
        IGeneroService generoService, IAlbumService albumService)
    {
        _sugestaoService = sugestaoService;
        _artistaService = artistaService;
        _generoService = generoService;
        _albumService = albumService;
    }

    /// <summary>Lista todas as sugestões (somente administrador). Filtro opcional: status = Pendente, Aprovada ou Recusada.</summary>
    [HttpGet]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Listar([FromQuery] string? status = null)
    {
        if (status is not null && !StatusValidos.Contains(status))
            return BadRequest("Status inválido. Use Pendente, Aprovada ou Recusada.");
        return Ok(await _sugestaoService.Listar(status, null));
    }

    /// <summary>Sugestões enviadas pelo usuário logado.</summary>
    [HttpGet("minhas")]
    public async Task<IActionResult> Minhas()
    {
        if (IdLogado() is not { } id) return Unauthorized();
        return Ok(await _sugestaoService.Listar(null, id));
    }

    /// <summary>Consulta uma sugestão (o autor ou um administrador).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var sugestao = await _sugestaoService.BuscarResposta(id);
        if (sugestao is null) return NotFound("Sugestão não encontrada.");
        if (sugestao.UsuarioId != IdLogado() && !User.IsInRole(Perfis.Administrador)) return Forbid();
        return Ok(sugestao);
    }

    /// <summary>Envia uma sugestão de música (qualquer usuário logado).</summary>
    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] SugestaoDTO dto)
    {
        if (IdLogado() is not { } usuarioId) return Unauthorized();
        var id = await _sugestaoService.Cadastrar(new Sugestao
        {
            UsuarioId = usuarioId,
            Titulo = dto.Titulo.Trim(),
            ArtistaNome = dto.ArtistaNome.Trim(),
            AlbumNome = string.IsNullOrWhiteSpace(dto.AlbumNome) ? null : dto.AlbumNome.Trim(),
            Observacao = string.IsNullOrWhiteSpace(dto.Observacao) ? null : dto.Observacao.Trim()
        });
        return CreatedAtAction(nameof(BuscarPorId), new { id }, await _sugestaoService.BuscarResposta(id));
    }

    /// <summary>
    /// Aprova a sugestão (somente administrador): cadastra a música com os dados conferidos
    /// e marca a sugestão como aprovada.
    /// </summary>
    [HttpPost("{id:int}/aprovar")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Aprovar(int id, [FromBody] AprovacaoDTO dto)
    {
        var sugestao = await _sugestaoService.BuscarPorId(id);
        if (sugestao is null) return NotFound("Sugestão não encontrada.");
        if (sugestao.Status != SugestaoService.Pendente) return Conflict("Esta sugestão já foi respondida.");

        var erro = await RegrasMusica.ValidarRelacionamentos(dto.Musica, _artistaService, _generoService, _albumService);
        if (erro is not null) return BadRequest(erro);

        int musicaId;
        try
        {
            musicaId = await _sugestaoService.Aprovar(id, RegrasMusica.Converter(dto.Musica), dto.Resposta);
        }
        catch (DbUpdateException falha) when (ErrosBanco.Duplicado(falha))
        {
            return Conflict("Este artista já possui uma música com esse título. Recuse a sugestão informando que ela já está no catálogo.");
        }

        return Ok(new { musicaId, sugestao = await _sugestaoService.BuscarResposta(id) });
    }

    /// <summary>Recusa a sugestão com um motivo (somente administrador).</summary>
    [HttpPost("{id:int}/recusar")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Recusar(int id, [FromBody] RecusaDTO dto)
    {
        var sugestao = await _sugestaoService.BuscarPorId(id);
        if (sugestao is null) return NotFound("Sugestão não encontrada.");
        if (sugestao.Status != SugestaoService.Pendente) return Conflict("Esta sugestão já foi respondida.");

        await _sugestaoService.Recusar(id, dto.Resposta);
        return Ok(await _sugestaoService.BuscarResposta(id));
    }

    /// <summary>Cancela uma sugestão pendente (o autor) ou remove qualquer sugestão (administrador).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var sugestao = await _sugestaoService.BuscarPorId(id);
        if (sugestao is null) return NotFound("Sugestão não encontrada.");

        var ehAdmin = User.IsInRole(Perfis.Administrador);
        if (!ehAdmin && sugestao.UsuarioId != IdLogado()) return Forbid();
        if (!ehAdmin && sugestao.Status != SugestaoService.Pendente)
            return Conflict("Só é possível cancelar sugestões que ainda estão pendentes.");

        await _sugestaoService.Excluir(id);
        return NoContent();
    }

    private int? IdLogado() =>
        int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
}
