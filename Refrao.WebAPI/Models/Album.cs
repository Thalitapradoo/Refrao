using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Models;

[Table("Albuns")]
[Index("ArtistaId", "Nome", Name = "UQ_Albuns_Artista_Nome", IsUnique = true)]
public partial class Album
{
    [Key]
    public int Id { get; set; }

    [StringLength(150)]
    [Unicode(false)]
    public string Nome { get; set; } = null!;

    public int ArtistaId { get; set; }

    public DateOnly DataLancamento { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? CapaUrl { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string? Descricao { get; set; }

    [ForeignKey("ArtistaId")]
    [InverseProperty("Albuns")]
    public virtual Artista Artista { get; set; } = null!;

    [InverseProperty("Album")]
    public virtual ICollection<Musica> Musicas { get; set; } = new List<Musica>();
}
