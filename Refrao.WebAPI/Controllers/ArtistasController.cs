using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Configuracoes;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArtistasController : ControllerBase
{
    private readonly IArtistaService _artistaService;

    public ArtistasController(IArtistaService artistaService)
    {
        _artistaService = artistaService;
    }

    /// <summary>Lista os artistas (com total de álbuns e músicas). Filtro opcional por nome.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca = null) =>
        Ok(await _artistaService.Listar(busca));

    /// <summary>Detalhes do artista com seus álbuns e músicas.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var artista = await _artistaService.BuscarDetalhe(id);
        return artista is null ? NotFound("Artista não encontrado.") : Ok(artista);
    }

    /// <summary>Cadastra um artista (somente administrador).</summary>
    [HttpPost]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Cadastrar([FromBody] ArtistaDTO dto)
    {
        Artista artista;
        try
        {
            artista = await _artistaService.Cadastrar(Converter(dto));
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Já existe um artista com esse nome.");
        }
        return CreatedAtAction(nameof(BuscarPorId), new { id = artista.Id }, await _artistaService.BuscarDetalhe(artista.Id));
    }

    /// <summary>Atualiza um artista (somente administrador).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] ArtistaDTO dto)
    {
        if (!await _artistaService.Existe(id)) return NotFound("Artista não encontrado.");
        try
        {
            await _artistaService.Atualizar(id, Converter(dto));
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Já existe um artista com esse nome.");
        }
        return NoContent();
    }

    /// <summary>Exclui um artista sem álbuns nem músicas (somente administrador).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Excluir(int id)
    {
        if (!await _artistaService.Existe(id)) return NotFound("Artista não encontrado.");
        if (await _artistaService.PossuiVinculos(id))
            return Conflict("Não é possível excluir: o artista possui álbuns ou músicas cadastrados. Exclua-os antes.");
        await _artistaService.Excluir(id);
        return NoContent();
    }

    private static Artista Converter(ArtistaDTO dto) => new()
    {
        Nome = dto.Nome.Trim(),
        Biografia = string.IsNullOrWhiteSpace(dto.Biografia) ? null : dto.Biografia.Trim(),
        Nacionalidade = dto.Nacionalidade.Trim(),
        ImagemUrl = string.IsNullOrWhiteSpace(dto.ImagemUrl) ? null : dto.ImagemUrl.Trim()
    };
}
