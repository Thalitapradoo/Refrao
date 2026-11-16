using Microsoft.EntityFrameworkCore;
using Refrao.WebAPI.Context;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Services;

public class UsuarioService : IUsuarioService
{
    private readonly RefraoDbContext _context;

    public UsuarioService(RefraoDbContext context)
    {
        _context = context;
    }

    public Task<List<Usuario>> Listar() =>
        _context.Usuarios.AsNoTracking().OrderBy(u => u.Nome).ToListAsync();

    public Task<Usuario?> BuscarPorId(int id) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public async Task<Usuario?> Autenticar(string email, string senha)
    {
        var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        return usuario is not null && SenhaService.Verificar(senha, usuario.SenhaHash) ? usuario : null;
    }

    public async Task Cadastrar(Usuario usuario, string senha)
    {
        usuario.SenhaHash = SenhaService.GerarHash(senha);
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task Atualizar(int id, string nome, string email, string? novaSenha, string perfil, bool ativo)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return;

        usuario.Nome = nome;
        usuario.Email = email;
        usuario.Perfil = perfil;
        usuario.Ativo = ativo;
        if (!string.IsNullOrEmpty(novaSenha))
            usuario.SenhaHash = SenhaService.GerarHash(novaSenha);

        await _context.SaveChangesAsync();
    }

    public async Task Desativar(int id)
    {
        // O trigger TR_Usuarios_SoftDelete transforma o DELETE em Ativo = 0.
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return;
        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
    }

    public Task<int> Contar() => _context.Usuarios.CountAsync();

    public Task<int> ContarAtivos() => _context.Usuarios.CountAsync(u => u.Ativo);
}
