using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Context;
using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Services;

public class SugestaoService : ISugestaoService
{
    public const string Pendente = "Pendente";
    public const string Aprovada = "Aprovada";
    public const string Recusada = "Recusada";

    private readonly RefraoDbContext _context;

    public SugestaoService(RefraoDbContext context)
    {
        _context = context;
    }

    public async Task<List<SugestaoRespostaDTO>> Listar(string? status, int? usuarioId)
    {
        var query = _context.Sugestoes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);
        if (usuarioId is not null) query = query.Where(s => s.UsuarioId == usuarioId);

        return await Projetar(query
                .OrderBy(s => s.Status == Pendente ? 0 : 1)
                .ThenByDescending(s => s.DataCriacao))
            .ToListAsync();
    }

    public Task<SugestaoRespostaDTO?> BuscarResposta(int id) =>
        Projetar(_context.Sugestoes.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync();

    public Task<Sugestao?> BuscarPorId(int id) =>
        _context.Sugestoes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

    public async Task<int> Cadastrar(Sugestao sugestao)
    {
        sugestao.Status = Pendente;
        await _context.Sugestoes.AddAsync(sugestao);
        await _context.SaveChangesAsync();
        return sugestao.Id;
    }

    public async Task<int> Aprovar(int id, Musica musica, string? resposta)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync();

        await _context.Musicas.AddAsync(musica);
        await _context.SaveChangesAsync();

        var sugestao = await _context.Sugestoes.FirstAsync(s => s.Id == id);
        sugestao.Status = Aprovada;
        sugestao.MusicaId = musica.Id;
        sugestao.RespostaAdmin = string.IsNullOrWhiteSpace(resposta) ? "Sugestão aprovada! A música entrou no catálogo." : resposta.Trim();
        sugestao.DataResposta = DateTime.Now;
        await _context.SaveChangesAsync();

        await transacao.CommitAsync();
        return musica.Id;
    }

    public async Task Recusar(int id, string resposta)
    {
        var sugestao = await _context.Sugestoes.FindAsync(id);
        if (sugestao is null) return;
        sugestao.Status = Recusada;
        sugestao.RespostaAdmin = resposta.Trim();
        sugestao.DataResposta = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        var sugestao = await _context.Sugestoes.FindAsync(id);
        if (sugestao is null) return;
        _context.Sugestoes.Remove(sugestao);
        await _context.SaveChangesAsync();
    }

    public Task<int> ContarPendentes() => _context.Sugestoes.CountAsync(s => s.Status == Pendente);

    private static IQueryable<SugestaoRespostaDTO> Projetar(IQueryable<Sugestao> query) =>
        query.Select(s => new SugestaoRespostaDTO(s.Id, s.UsuarioId, s.Usuario.Nome, s.Titulo, s.ArtistaNome,
            s.AlbumNome, s.Observacao, s.Status, s.RespostaAdmin, s.MusicaId, s.DataCriacao, s.DataResposta));
}
