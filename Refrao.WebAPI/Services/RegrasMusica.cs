using Refrao.WebAPI.DTO;
using Refrao.WebAPI.Interfaces;
using Refrao.WebAPI.Models;

namespace Refrao.WebAPI.Services;

/// <summary>Regras de cadastro de música usadas no CRUD e na aprovação de sugestões.</summary>
public static class RegrasMusica
{
    public static async Task<string?> ValidarRelacionamentos(MusicaDTO dto, IArtistaService artistas,
        IGeneroService generos, IAlbumService albuns)
    {
        if (!await artistas.Existe(dto.ArtistaId)) return "O artista informado não existe.";
        if (await generos.BuscarPorId(dto.GeneroId) is null) return "O gênero informado não existe.";
        if (dto.AlbumId is not null)
        {
            var artistaDoAlbum = await albuns.ArtistaDoAlbum(dto.AlbumId.Value);
            if (artistaDoAlbum is null) return "O álbum informado não existe.";
            if (artistaDoAlbum != dto.ArtistaId) return "O álbum informado não pertence a este artista.";
        }
        return null;
    }

    public static Musica Converter(MusicaDTO dto) => new()
    {
        Titulo = dto.Titulo.Trim(),
        ArtistaId = dto.ArtistaId,
        AlbumId = dto.AlbumId,
        GeneroId = dto.GeneroId,
        Duracao = dto.Duracao,
        DataLancamento = dto.DataLancamento!.Value,
        CapaUrl = string.IsNullOrWhiteSpace(dto.CapaUrl) ? null : dto.CapaUrl.Trim(),
        Descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim(),
        InicioRefrao = dto.InicioRefrao,
        LinkLetra = string.IsNullOrWhiteSpace(dto.LinkLetra) ? null : dto.LinkLetra.Trim()
    };
}
