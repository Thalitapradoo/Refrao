using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Context;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Services;

public class GeneroService : IGeneroService
{
    private readonly RefraoDbContext _context;

    public GeneroService(RefraoDbContext context)
    {
        _context = context;
    }

    public Task<List<Genero>> Listar() =>
        _context.Generos.AsNoTracking().OrderBy(g => g.Nome).ToListAsync();

    public Task<Genero?> BuscarPorId(int id) =>
        _context.Generos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);

    public Task<bool> PossuiMusicas(int id) => _context.Musicas.AnyAsync(m => m.GeneroId == id);

    public async Task Cadastrar(Genero genero)
    {
        await _context.Generos.AddAsync(genero);
        await _context.SaveChangesAsync();
    }

    public async Task Atualizar(int id, Genero dados)
    {
        var genero = await _context.Generos.FindAsync(id);
        if (genero is null) return;
        genero.Nome = dados.Nome;
        genero.Descricao = dados.Descricao;
        await _context.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        var genero = await _context.Generos.FindAsync(id);
        if (genero is null) return;
        _context.Generos.Remove(genero);
        await _context.SaveChangesAsync();
    }

    public Task<int> Contar() => _context.Generos.CountAsync();
}
