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
public class GenerosController : ControllerBase
{
    private readonly IGeneroService _generoService;

    public GenerosController(IGeneroService generoService)
    {
        _generoService = generoService;
    }

    /// <summary>Lista os gêneros em ordem alfabética.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _generoService.Listar());

    /// <summary>Consulta um gênero pelo ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var genero = await _generoService.BuscarPorId(id);
        return genero is null ? NotFound("Gênero não encontrado.") : Ok(genero);
    }

    /// <summary>Cadastra um gênero (somente administrador).</summary>
    [HttpPost]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Cadastrar([FromBody] GeneroDTO dto)
    {
        var genero = new Genero { Nome = dto.Nome.Trim(), Descricao = Limpar(dto.Descricao) };
        try
        {
            await _generoService.Cadastrar(genero);
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Já existe um gênero com esse nome.");
        }
        return CreatedAtAction(nameof(BuscarPorId), new { id = genero.Id }, genero);
    }

    /// <summary>Atualiza um gênero (somente administrador).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] GeneroDTO dto)
    {
        if (await _generoService.BuscarPorId(id) is null) return NotFound("Gênero não encontrado.");
        try
        {
            await _generoService.Atualizar(id, new Genero { Nome = dto.Nome.Trim(), Descricao = Limpar(dto.Descricao) });
        }
        catch (DbUpdateException erro) when (ErrosBanco.Duplicado(erro))
        {
            return Conflict("Já existe um gênero com esse nome.");
        }
        return NoContent();
    }

    /// <summary>Exclui um gênero sem músicas associadas (somente administrador).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Excluir(int id)
    {
        if (await _generoService.BuscarPorId(id) is null) return NotFound("Gênero não encontrado.");
        if (await _generoService.PossuiMusicas(id))
            return Conflict("Não é possível excluir: existem músicas com este gênero. Altere o gênero delas antes.");
        await _generoService.Excluir(id);
        return NoContent();
    }

    private static string? Limpar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
