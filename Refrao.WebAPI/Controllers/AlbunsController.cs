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
public class AlbunsController : ControllerBase
{
    private readonly IAlbumService _albumService;
    private readonly IArtistaService _artistaService;

    public AlbunsController(IAlbumService albumService, IArtistaService artistaService)
    {
        _albumService = albumService;
        _artistaService = artistaService;
    }

    /// <summary>Lista os álbuns com suas músicas. Filtros: artistaId e busca (nome do álbum ou do artista).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? artistaId = null, [FromQuery] string? busca = null) =>
        Ok(await _albumService.Listar(artistaId, busca));

    /// <summary>Consulta um álbum pelo ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var album = await _albumService.BuscarPorId(id);
        return album is null ? NotFound("Álbum não encontrado.") : Ok(album);
    }

    /// <summary>Cadastra um álbum (somente administrador).</summary>
    [HttpPost]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Cadastrar([FromBody] AlbumDTO dto)
    {
        if (!await _artistaService.Existe(dto.ArtistaId)) return BadRequest("O artista informado não existe.");

        int id;
        try
        {
            id = await _albumService.Cadastrar(Converter(dto));
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Este artista já possui um álbum com esse nome.");
        }
        return CreatedAtAction(nameof(BuscarPorId), new { id }, await _albumService.BuscarPorId(id));
    }

    /// <summary>Atualiza um álbum (somente administrador).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AlbumDTO dto)
    {
        var artistaAtual = await _albumService.ArtistaDoAlbum(id);
        if (artistaAtual is null) return NotFound("Álbum não encontrado.");
        if (!await _artistaService.Existe(dto.ArtistaId)) return BadRequest("O artista informado não existe.");
        if (artistaAtual != dto.ArtistaId && await _albumService.PossuiMusicas(id))
            return BadRequest("Não é possível trocar o artista de um álbum que já tem músicas.");

        try
        {
            await _albumService.Atualizar(id, Converter(dto));
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Este artista já possui um álbum com esse nome.");
        }
        return NoContent();
    }

    /// <summary>Exclui um álbum sem músicas (somente administrador).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Excluir(int id)
    {
        if (await _albumService.ArtistaDoAlbum(id) is null) return NotFound("Álbum não encontrado.");
        if (await _albumService.PossuiMusicas(id))
            return Conflict("Não é possível excluir: o álbum possui músicas. Exclua ou mova as músicas antes.");
        await _albumService.Excluir(id);
        return NoContent();
    }

    private static Album Converter(AlbumDTO dto) => new()
    {
        Nome = dto.Nome.Trim(),
        ArtistaId = dto.ArtistaId,
        DataLancamento = dto.DataLancamento!.Value,
        CapaUrl = string.IsNullOrWhiteSpace(dto.CapaUrl) ? null : dto.CapaUrl.Trim(),
        Descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim()
    };
}
