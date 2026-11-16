using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Models;

[Table("Musicas")]
[Index("ArtistaId", "Titulo", Name = "UQ_Musicas_Artista_Titulo", IsUnique = true)]
public partial class Musica
{
    [Key]
    public int Id { get; set; }

    [StringLength(150)]
    [Unicode(false)]
    public string Titulo { get; set; } = null!;

    public int ArtistaId { get; set; }

    public int? AlbumId { get; set; }

    public int GeneroId { get; set; }

    /// <summary>Duração em segundos.</summary>
    public int Duracao { get; set; }

    public DateOnly DataLancamento { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? CapaUrl { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string? Descricao { get; set; }

    /// <summary>Segundo da música em que o refrão começa (opcional).</summary>
    public int? InicioRefrao { get; set; }

    /// <summary>Link para a letra em um site licenciado (opcional).</summary>
    [StringLength(500)]
    [Unicode(false)]
    public string? LinkLetra { get; set; }

    [ForeignKey("ArtistaId")]
    [InverseProperty("Musicas")]
    public virtual Artista Artista { get; set; } = null!;

    [ForeignKey("AlbumId")]
    [InverseProperty("Musicas")]
    public virtual Album? Album { get; set; }

    [ForeignKey("GeneroId")]
    [InverseProperty("Musicas")]
    public virtual Genero Genero { get; set; } = null!;

    [InverseProperty("Musica")]
    public virtual ICollection<ComentarioRefrao> Comentarios { get; set; } = new List<ComentarioRefrao>();
}
