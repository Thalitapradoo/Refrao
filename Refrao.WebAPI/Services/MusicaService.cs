using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Context;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Services;

public class MusicaService : IMusicaService
{
    private readonly RefraoDbContext _context;

    public MusicaService(RefraoDbContext context)
    {
        _context = context;
    }

    public async Task<PaginaDTO<MusicaRespostaDTO>> Listar(FiltroMusicas f)
    {
        var query = _context.Musicas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(f.Busca))
            query = query.Where(m => m.Titulo.Contains(f.Busca) || m.Artista.Nome.Contains(f.Busca));
        if (!string.IsNullOrWhiteSpace(f.Titulo))
            query = query.Where(m => m.Titulo.Contains(f.Titulo));
        if (!string.IsNullOrWhiteSpace(f.Artista))
            query = query.Where(m => m.Artista.Nome.Contains(f.Artista));
        if (f.ArtistaId is not null)
            query = query.Where(m => m.ArtistaId == f.ArtistaId);
        if (f.AlbumId is not null)
            query = query.Where(m => m.AlbumId == f.AlbumId);
        if (f.GeneroId is not null)
            query = query.Where(m => m.GeneroId == f.GeneroId);

        var total = await query.CountAsync();

        var itens = await Projetar(query
                .OrderBy(m => m.Artista.Nome)
                .ThenBy(m => m.DataLancamento)
                .ThenBy(m => m.Titulo)
                .Skip((f.Pagina - 1) * f.TamanhoPagina)
                .Take(f.TamanhoPagina))
            .ToListAsync();

        return new PaginaDTO<MusicaRespostaDTO>(itens, total, f.Pagina, f.TamanhoPagina);
    }

    public Task<MusicaRespostaDTO?> BuscarPorId(int id) =>
        Projetar(_context.Musicas.AsNoTracking().Where(m => m.Id == id)).FirstOrDefaultAsync();

    public async Task<int> Cadastrar(Musica musica)
    {
        await _context.Musicas.AddAsync(musica);
        await _context.SaveChangesAsync();
        return musica.Id;
    }

    public async Task Atualizar(int id, Musica dados)
    {
        var musica = await _context.Musicas.FindAsync(id);
        if (musica is null) return;
        musica.Titulo = dados.Titulo;
        musica.ArtistaId = dados.ArtistaId;
        musica.AlbumId = dados.AlbumId;
        musica.GeneroId = dados.GeneroId;
        musica.Duracao = dados.Duracao;
        musica.DataLancamento = dados.DataLancamento;
        musica.CapaUrl = dados.CapaUrl;
        musica.Descricao = dados.Descricao;
        musica.InicioRefrao = dados.InicioRefrao;
        musica.LinkLetra = dados.LinkLetra;
        await _context.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        var musica = await _context.Musicas.FindAsync(id);
        if (musica is null) return;
        _context.Musicas.Remove(musica);
        await _context.SaveChangesAsync();
    }

    public Task<int> Contar() => _context.Musicas.CountAsync();

    private static IQueryable<MusicaRespostaDTO> Projetar(IQueryable<Musica> query) =>
        query.Select(m => new MusicaRespostaDTO(
            m.Id,
            m.Titulo,
            m.Duracao,
            m.DataLancamento,
            m.CapaUrl,
            m.Descricao,
            m.ArtistaId,
            m.Artista.Nome,
            m.AlbumId,
            m.Album != null ? m.Album.Nome : null,
            m.Album != null ? m.Album.CapaUrl : null,
            m.GeneroId,
            m.Genero.Nome,
            m.InicioRefrao,
            m.LinkLetra,
            m.Comentarios.Count));
}
