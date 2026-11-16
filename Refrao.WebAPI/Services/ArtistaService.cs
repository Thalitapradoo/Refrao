using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Context;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Services;

public class ArtistaService : IArtistaService
{
    private readonly RefraoDbContext _context;
    private readonly IAlbumService _albumService;
    private readonly IMusicaService _musicaService;

    public ArtistaService(RefraoDbContext context, IAlbumService albumService, IMusicaService musicaService)
    {
        _context = context;
        _albumService = albumService;
        _musicaService = musicaService;
    }

    public async Task<List<ArtistaResumoDTO>> Listar(string? busca)
    {
        var query = _context.Artistas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(a => a.Nome.Contains(busca));

        return await query
            .OrderBy(a => a.Nome)
            .Select(a => new ArtistaResumoDTO(a.Id, a.Nome, a.Biografia, a.Nacionalidade, a.ImagemUrl,
                a.Albuns.Count, a.Musicas.Count))
            .ToListAsync();
    }

    public async Task<ArtistaDetalheDTO?> BuscarDetalhe(int id)
    {
        var artista = await _context.Artistas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (artista is null) return null;

        var albuns = await _albumService.Listar(id, null);
        var musicas = await _musicaService.Listar(new FiltroMusicas { ArtistaId = id, TamanhoPagina = 1000 });

        return new ArtistaDetalheDTO(artista.Id, artista.Nome, artista.Biografia, artista.Nacionalidade,
            artista.ImagemUrl, albuns, musicas.Items);
    }

    public Task<bool> Existe(int id) => _context.Artistas.AnyAsync(a => a.Id == id);

    public async Task<bool> PossuiVinculos(int id) =>
        await _context.Albuns.AnyAsync(a => a.ArtistaId == id) || await _context.Musicas.AnyAsync(m => m.ArtistaId == id);

    public async Task<Artista> Cadastrar(Artista artista)
    {
        await _context.Artistas.AddAsync(artista);
        await _context.SaveChangesAsync();
        return artista;
    }

    public async Task Atualizar(int id, Artista dados)
    {
        var artista = await _context.Artistas.FindAsync(id);
        if (artista is null) return;
        artista.Nome = dados.Nome;
        artista.Biografia = dados.Biografia;
        artista.Nacionalidade = dados.Nacionalidade;
        artista.ImagemUrl = dados.ImagemUrl;
        await _context.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        var artista = await _context.Artistas.FindAsync(id);
        if (artista is null) return;
        _context.Artistas.Remove(artista);
        await _context.SaveChangesAsync();
    }

    public Task<int> Contar() => _context.Artistas.CountAsync();
}
