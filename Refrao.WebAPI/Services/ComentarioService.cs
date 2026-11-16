using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Context;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Services;

public class ComentarioService : IComentarioService
{
    private readonly RefraoDbContext _context;

    public ComentarioService(RefraoDbContext context)
    {
        _context = context;
    }

    public Task<List<ComentarioRespostaDTO>> ListarPorMusica(int musicaId) =>
        Projetar(_context.ComentariosRefrao.AsNoTracking().Where(c => c.MusicaId == musicaId))
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync();

    public Task<ComentarioRespostaDTO?> BuscarResposta(int id) =>
        Projetar(_context.ComentariosRefrao.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync();

    public Task<ComentarioRefrao?> BuscarPorId(int id) =>
        _context.ComentariosRefrao.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> MusicaExiste(int musicaId) => _context.Musicas.AnyAsync(m => m.Id == musicaId);

    public async Task<int> Cadastrar(ComentarioRefrao comentario)
    {
        await _context.ComentariosRefrao.AddAsync(comentario);
        await _context.SaveChangesAsync();
        return comentario.Id;
    }

    public async Task Atualizar(int id, string texto)
    {
        var comentario = await _context.ComentariosRefrao.FindAsync(id);
        if (comentario is null) return;
        comentario.Texto = texto;
        await _context.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        var comentario = await _context.ComentariosRefrao.FindAsync(id);
        if (comentario is null) return;
        _context.ComentariosRefrao.Remove(comentario);
        await _context.SaveChangesAsync();
    }

    private static IQueryable<ComentarioRespostaDTO> Projetar(IQueryable<ComentarioRefrao> query) =>
        query.Select(c => new ComentarioRespostaDTO(c.Id, c.MusicaId, c.UsuarioId, c.Usuario.Nome, c.Texto, c.DataCriacao));
}
