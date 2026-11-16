using System.ComponentModel.DataAnnotations;

namespace Refrao.WebAPI.DTO;

public class ArtistaDTO
{
    [Required(ErrorMessage = "Informe o nome do artista.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "A biografia deve ter no máximo 2000 caracteres.")]
    public string? Biografia { get; set; }

    [Required(ErrorMessage = "Informe a nacionalidade.")]
    [StringLength(60, ErrorMessage = "A nacionalidade deve ter no máximo 60 caracteres.")]
    public string Nacionalidade { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ImagemUrl { get; set; }
}

public class GeneroDTO
{
    [Required(ErrorMessage = "Informe o nome do gênero.")]
    [StringLength(50, ErrorMessage = "O nome deve ter no máximo 50 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Descricao { get; set; }
}

public class AlbumDTO : IValidatableObject
{
    [Required(ErrorMessage = "Informe o nome do álbum.")]
    [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Informe o artista do álbum.")]
    public int ArtistaId { get; set; }

    [Required(ErrorMessage = "Informe a data de lançamento.")]
    public DateOnly? DataLancamento { get; set; }

    [StringLength(500)]
    public string? CapaUrl { get; set; }

    [StringLength(1000)]
    public string? Descricao { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataLancamento is { } d && (d.Year < 1900 || d > DateOnly.FromDateTime(DateTime.Today).AddYears(1)))
            yield return new ValidationResult("A data de lançamento deve estar entre 1900 e o próximo ano.", new[] { nameof(DataLancamento) });
    }
}

public class MusicaDTO : IValidatableObject
{
    [Required(ErrorMessage = "A música precisa de um título.")]
    [StringLength(150, ErrorMessage = "O título deve ter no máximo 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A música precisa de um artista.")]
    public int ArtistaId { get; set; }

    /// <summary>Opcional (single avulso). Se informado, o álbum precisa ser do mesmo artista.</summary>
    public int? AlbumId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Informe o gênero.")]
    public int GeneroId { get; set; }

    /// <summary>Duração em segundos (1 a 3600).</summary>
    [Range(1, 3600, ErrorMessage = "A duração deve estar entre 1 segundo e 60 minutos.")]
    public int Duracao { get; set; }

    [Required(ErrorMessage = "Informe a data de lançamento.")]
    public DateOnly? DataLancamento { get; set; }

    [StringLength(500)]
    public string? CapaUrl { get; set; }

    [StringLength(1000)]
    public string? Descricao { get; set; }

    /// <summary>Segundo em que o refrão começa (opcional). Precisa ser menor que a duração.</summary>
    [Range(0, 3599, ErrorMessage = "O início do refrão deve ser um segundo válido da música.")]
    public int? InicioRefrao { get; set; }

    /// <summary>Link para a letra em um site licenciado (opcional, começando com http:// ou https://).</summary>
    [StringLength(500)]
    public string? LinkLetra { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InicioRefrao is { } inicio && inicio >= Duracao)
            yield return new ValidationResult("O refrão tem que começar antes do fim da música.", new[] { nameof(InicioRefrao) });
        if (!string.IsNullOrWhiteSpace(LinkLetra)
            && !(Uri.TryCreate(LinkLetra.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            yield return new ValidationResult("O link da letra precisa ser um endereço válido começando com http:// ou https://.", new[] { nameof(LinkLetra) });
        if (string.IsNullOrWhiteSpace(Titulo))
            yield return new ValidationResult("A música precisa de um título.", new[] { nameof(Titulo) });
        if (DataLancamento is { } d && (d.Year < 1900 || d > DateOnly.FromDateTime(DateTime.Today).AddYears(1)))
            yield return new ValidationResult("A data de lançamento deve estar entre 1900 e o próximo ano.", new[] { nameof(DataLancamento) });
    }
}
