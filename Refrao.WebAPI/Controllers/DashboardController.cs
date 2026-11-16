using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Refrao.WebAPI.Configuracoes;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;

namespace Refrao.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = Perfis.Administrador)]
public class DashboardController : ControllerBase
{
    private readonly IMusicaService _musicas;
    private readonly IArtistaService _artistas;
    private readonly IAlbumService _albuns;
    private readonly IGeneroService _generos;
    private readonly IUsuarioService _usuarios;
    private readonly ISugestaoService _sugestoes;

    public DashboardController(IMusicaService musicas, IArtistaService artistas, IAlbumService albuns,
        IGeneroService generos, IUsuarioService usuarios, ISugestaoService sugestoes)
    {
        _musicas = musicas;
        _artistas = artistas;
        _albuns = albuns;
        _generos = generos;
        _usuarios = usuarios;
        _sugestoes = sugestoes;
    }

    /// <summary>Totais do catálogo para o painel administrativo.</summary>
    [HttpGet]
    public async Task<IActionResult> Totais() => Ok(new DashboardDTO(
        await _musicas.Contar(),
        await _artistas.Contar(),
        await _albuns.Contar(),
        await _generos.Contar(),
        await _usuarios.Contar(),
        await _usuarios.ContarAtivos(),
        await _sugestoes.ContarPendentes()));
}
