using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Context;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Services;

public class AlbumService : IAlbumService
{
    private readonly RefraoDbContext _context;

    public AlbumService(RefraoDbContext context)
    {
        _context = context;
    }

    public async Task<List<AlbumRespostaDTO>> Listar(int? artistaId, string? busca)
    {
        var query = _context.Albuns.AsNoTracking();
        if (artistaId is not null) query = query.Where(a => a.ArtistaId == artistaId);
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(a => a.Nome.Contains(busca) || a.Artista.Nome.Contains(busca));

        var albuns = await query
            .Include(a => a.Artista)
            .Include(a => a.Musicas).ThenInclude(m => m.Genero)
            .OrderBy(a => a.Artista.Nome).ThenBy(a => a.DataLancamento)
            .AsSplitQuery()
            .ToListAsync();

        return albuns.Select(Converter).ToList();
    }

    public async Task<AlbumRespostaDTO?> BuscarPorId(int id)
    {
        var album = await _context.Albuns.AsNoTracking()
            .Include(a => a.Artista)
            .Include(a => a.Musicas).ThenInclude(m => m.Genero)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);
        return album is null ? null : Converter(album);
    }

    public Task<int?> ArtistaDoAlbum(int id) =>
        _context.Albuns.Where(a => a.Id == id).Select(a => (int?)a.ArtistaId).FirstOrDefaultAsync();

    public Task<bool> PossuiMusicas(int id) => _context.Musicas.AnyAsync(m => m.AlbumId == id);

    public async Task<int> Cadastrar(Album album)
    {
        await _context.Albuns.AddAsync(album);
        await _context.SaveChangesAsync();
        return album.Id;
    }

    public async Task Atualizar(int id, Album dados)
    {
        var album = await _context.Albuns.FindAsync(id);
        if (album is null) return;
        album.Nome = dados.Nome;
        album.ArtistaId = dados.ArtistaId;
        album.DataLancamento = dados.DataLancamento;
        album.CapaUrl = dados.CapaUrl;
        album.Descricao = dados.Descricao;
        await _context.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        var album = await _context.Albuns.FindAsync(id);
        if (album is null) return;
        _context.Albuns.Remove(album);
        await _context.SaveChangesAsync();
    }

    public Task<int> Contar() => _context.Albuns.CountAsync();

    private static AlbumRespostaDTO Converter(Album a) => new(
        a.Id, a.Nome, a.DataLancamento, a.CapaUrl, a.Descricao, a.ArtistaId, a.Artista.Nome,
        a.Musicas.Count,
        a.Musicas
            .OrderBy(m => m.DataLancamento).ThenBy(m => m.Titulo)
            .Select(m => new FaixaDTO(m.Id, m.Titulo, m.Duracao, m.Genero.Nome))
            .ToList());
}
