using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Interfaces;

public interface ITokenService
{
    LoginRespostaDTO GerarToken(Usuario usuario);
}

public interface IUsuarioService
{
    Task<List<Usuario>> Listar();
    Task<Usuario?> BuscarPorId(int id);
    Task<Usuario?> Autenticar(string email, string senha);
    Task Cadastrar(Usuario usuario, string senha);
    Task Atualizar(int id, string nome, string email, string? novaSenha, string perfil, bool ativo);
    Task Desativar(int id);
    Task<int> Contar();
    Task<int> ContarAtivos();
}

public interface IArtistaService
{
    Task<List<ArtistaResumoDTO>> Listar(string? busca);
    Task<ArtistaDetalheDTO?> BuscarDetalhe(int id);
    Task<bool> Existe(int id);
    Task<bool> PossuiVinculos(int id);
    Task<Artista> Cadastrar(Artista artista);
    Task Atualizar(int id, Artista dados);
    Task Excluir(int id);
    Task<int> Contar();
}

public interface IGeneroService
{
    Task<List<Genero>> Listar();
    Task<Genero?> BuscarPorId(int id);
    Task<bool> PossuiMusicas(int id);
    Task Cadastrar(Genero genero);
    Task Atualizar(int id, Genero dados);
    Task Excluir(int id);
    Task<int> Contar();
}

public interface IAlbumService
{
    Task<List<AlbumRespostaDTO>> Listar(int? artistaId, string? busca);
    Task<AlbumRespostaDTO?> BuscarPorId(int id);
    Task<int?> ArtistaDoAlbum(int id);
    Task<bool> PossuiMusicas(int id);
    Task<int> Cadastrar(Album album);
    Task Atualizar(int id, Album dados);
    Task Excluir(int id);
    Task<int> Contar();
}

public interface IMusicaService
{
    Task<PaginaDTO<MusicaRespostaDTO>> Listar(FiltroMusicas filtro);
    Task<MusicaRespostaDTO?> BuscarPorId(int id);
    Task<int> Cadastrar(Musica musica);
    Task Atualizar(int id, Musica dados);
    Task Excluir(int id);
    Task<int> Contar();
}

public class FiltroMusicas
{
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 12;
    /// <summary>Pesquisa no título da música OU no nome do artista.</summary>
    public string? Busca { get; set; }
    public string? Titulo { get; set; }
    public string? Artista { get; set; }
    public int? ArtistaId { get; set; }
    public int? AlbumId { get; set; }
    public int? GeneroId { get; set; }
}

public interface IComentarioService
{
    Task<List<ComentarioRespostaDTO>> ListarPorMusica(int musicaId);
    Task<ComentarioRespostaDTO?> BuscarResposta(int id);
    Task<ComentarioRefrao?> BuscarPorId(int id);
    Task<bool> MusicaExiste(int musicaId);
    Task<int> Cadastrar(ComentarioRefrao comentario);
    Task Atualizar(int id, string texto);
    Task Excluir(int id);
}

public interface ISugestaoService
{
    Task<List<SugestaoRespostaDTO>> Listar(string? status, int? usuarioId);
    Task<SugestaoRespostaDTO?> BuscarResposta(int id);
    Task<Sugestao?> BuscarPorId(int id);
    Task<int> Cadastrar(Sugestao sugestao);
    /// <summary>Cadastra a música e marca a sugestão como aprovada (na mesma transação).</summary>
    Task<int> Aprovar(int id, Musica musica, string? resposta);
    Task Recusar(int id, string resposta);
    Task Excluir(int id);
    Task<int> ContarPendentes();
}
