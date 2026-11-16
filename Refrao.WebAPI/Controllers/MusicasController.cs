using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Configuracoes;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;
using Refrao.WebAPI.Services;

namespace Refrao.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MusicasController : ControllerBase
{
    private readonly IMusicaService _musicaService;
    private readonly IArtistaService _artistaService;
    private readonly IAlbumService _albumService;
    private readonly IGeneroService _generoService;

    public MusicasController(IMusicaService musicaService, IArtistaService artistaService,
        IAlbumService albumService, IGeneroService generoService)
    {
        _musicaService = musicaService;
        _artistaService = artistaService;
        _albumService = albumService;
        _generoService = generoService;
    }

    /// <summary>Lista as músicas com paginação, pesquisa e filtros.</summary>
    /// <param name="busca">Pesquisa no título da música OU no nome do artista.</param>
    /// <param name="titulo">Pesquisa só no título.</param>
    /// <param name="artista">Pesquisa só no nome do artista.</param>
    /// <param name="artistaId">Músicas de um artista.</param>
    /// <param name="albumId">Músicas de um álbum.</param>
    /// <param name="generoId">Filtra por gênero.</param>
    /// <param name="pagina">Página (a partir de 1).</param>
    /// <param name="tamanhoPagina">Itens por página (1 a 100).</param>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca = null, [FromQuery] string? titulo = null, [FromQuery] string? artista = null,
        [FromQuery] int? artistaId = null, [FromQuery] int? albumId = null, [FromQuery] int? generoId = null,
        [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 12)
    {
        if (pagina < 1 || tamanhoPagina is < 1 or > 100)
            return BadRequest("A página deve ser maior que zero e o tamanhoPagina deve estar entre 1 e 100.");

        return Ok(await _musicaService.Listar(new FiltroMusicas
        {
            Busca = busca, Titulo = titulo, Artista = artista,
            ArtistaId = artistaId, AlbumId = albumId, GeneroId = generoId,
            Pagina = pagina, TamanhoPagina = tamanhoPagina
        }));
    }

    /// <summary>Detalhes de uma música.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var musica = await _musicaService.BuscarPorId(id);
        return musica is null ? NotFound("Música não encontrada.") : Ok(musica);
    }

    /// <summary>Cadastra uma música (somente administrador).</summary>
    [HttpPost]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Cadastrar([FromBody] MusicaDTO dto)
    {
        var erro = await ValidarRelacionamentos(dto);
        if (erro is not null) return BadRequest(erro);

        int id;
        try
        {
            id = await _musicaService.Cadastrar(Converter(dto));
        }
        catch (DbUpdateException falha) when (ErrosBanco.Duplicado(falha))
        {
            return Conflict("Este artista já possui uma música com esse título.");
        }
        catch (DbUpdateException falha) when (ErrosBanco.Restricao(falha))
        {
            return BadRequest("Os dados da música não respeitam as regras do banco (artista, álbum ou gênero inválidos).");
        }

        return CreatedAtAction(nameof(BuscarPorId), new { id }, await _musicaService.BuscarPorId(id));
    }

    /// <summary>Atualiza uma música (somente administrador).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] MusicaDTO dto)
    {
        if (await _musicaService.BuscarPorId(id) is null) return NotFound("Música não encontrada.");

        var erro = await ValidarRelacionamentos(dto);
        if (erro is not null) return BadRequest(erro);

        try
        {
            await _musicaService.Atualizar(id, Converter(dto));
        }
        catch (DbUpdateException falha) when (ErrosBanco.Duplicado(falha))
        {
            return Conflict("Este artista já possui uma música com esse título.");
        }
        catch (DbUpdateException falha) when (ErrosBanco.Restricao(falha))
        {
            return BadRequest("Os dados da música não respeitam as regras do banco (artista, álbum ou gênero inválidos).");
        }

        return NoContent();
    }

    /// <summary>Exclui uma música (somente administrador).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Excluir(int id)
    {
        if (await _musicaService.BuscarPorId(id) is null) return NotFound("Música não encontrada.");
        await _musicaService.Excluir(id);
        return NoContent();
    }

    private Task<string?> ValidarRelacionamentos(MusicaDTO dto) =>
        RegrasMusica.ValidarRelacionamentos(dto, _artistaService, _generoService, _albumService);

    private static Musica Converter(MusicaDTO dto) => RegrasMusica.Converter(dto);
}
