namespace Refrao.WebAPI.DTO;

public record PaginaDTO<T>(List<T> Items, int Total, int Pagina, int TamanhoPagina);

public record MusicaRespostaDTO(
    int Id,
    string Titulo,
    int Duracao,
    DateOnly DataLancamento,
    string? CapaUrl,
    string? Descricao,
    int ArtistaId,
    string ArtistaNome,
    int? AlbumId,
    string? AlbumNome,
    string? AlbumCapaUrl,
    int GeneroId,
    string GeneroNome,
    int? InicioRefrao,
    string? LinkLetra,
    int TotalComentarios);

public record FaixaDTO(int Id, string Titulo, int Duracao, string GeneroNome);

public record AlbumRespostaDTO(
    int Id,
    string Nome,
    DateOnly DataLancamento,
    string? CapaUrl,
    string? Descricao,
    int ArtistaId,
    string ArtistaNome,
    int TotalMusicas,
    List<FaixaDTO> Musicas);

public record ArtistaResumoDTO(
    int Id,
    string Nome,
    string? Biografia,
    string Nacionalidade,
    string? ImagemUrl,
    int TotalAlbuns,
    int TotalMusicas);

public record ArtistaDetalheDTO(
    int Id,
    string Nome,
    string? Biografia,
    string Nacionalidade,
    string? ImagemUrl,
    List<AlbumRespostaDTO> Albuns,
    List<MusicaRespostaDTO> Musicas);

public record DashboardDTO(int Musicas, int Artistas, int Albuns, int Generos, int Usuarios, int UsuariosAtivos, int SugestoesPendentes);

public record ComentarioRespostaDTO(int Id, int MusicaId, int UsuarioId, string UsuarioNome, string Texto, DateTime DataCriacao);

public record SugestaoRespostaDTO(
    int Id,
    int UsuarioId,
    string UsuarioNome,
    string Titulo,
    string ArtistaNome,
    string? AlbumNome,
    string? Observacao,
    string Status,
    string? RespostaAdmin,
    int? MusicaId,
    DateTime DataCriacao,
    DateTime? DataResposta);
