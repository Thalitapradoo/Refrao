using System.ComponentModel.DataAnnotations;

namespace Refrao.WebAPI.DTO;

public class ComentarioDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Informe a música.")]
    public int MusicaId { get; set; }

    [Required(ErrorMessage = "Escreva por que esse refrão gruda.")]
    [StringLength(300, MinimumLength = 3, ErrorMessage = "O comentário deve ter entre 3 e 300 caracteres.")]
    public string Texto { get; set; } = string.Empty;
}

public class ComentarioEdicaoDTO
{
    [Required(ErrorMessage = "Escreva por que esse refrão gruda.")]
    [StringLength(300, MinimumLength = 3, ErrorMessage = "O comentário deve ter entre 3 e 300 caracteres.")]
    public string Texto { get; set; } = string.Empty;
}

public class SugestaoDTO
{
    [Required(ErrorMessage = "Informe o título da música.")]
    [StringLength(150, ErrorMessage = "O título deve ter no máximo 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o artista.")]
    [StringLength(100, ErrorMessage = "O nome do artista deve ter no máximo 100 caracteres.")]
    public string ArtistaNome { get; set; } = string.Empty;

    [StringLength(150)]
    public string? AlbumNome { get; set; }

    [StringLength(500, ErrorMessage = "A observação deve ter no máximo 500 caracteres.")]
    public string? Observacao { get; set; }
}

/// <summary>Aprovação: o administrador confere os dados e a música é cadastrada no catálogo.</summary>
public class AprovacaoDTO
{
    [Required(ErrorMessage = "Informe os dados da música.")]
    public MusicaDTO Musica { get; set; } = new();

    [StringLength(300)]
    public string? Resposta { get; set; }
}

public class RecusaDTO
{
    [Required(ErrorMessage = "Explique o motivo da recusa.")]
    [StringLength(300, MinimumLength = 3, ErrorMessage = "O motivo deve ter entre 3 e 300 caracteres.")]
    public string Resposta { get; set; } = string.Empty;
}
