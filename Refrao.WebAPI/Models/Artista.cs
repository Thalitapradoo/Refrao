using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Models;

[Table("Artistas")]
[Index("Nome", Name = "UQ_Artistas_Nome", IsUnique = true)]
public partial class Artista
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string Nome { get; set; } = null!;

    [StringLength(2000)]
    [Unicode(false)]
    public string? Biografia { get; set; }

    [StringLength(60)]
    [Unicode(false)]
    public string Nacionalidade { get; set; } = null!;

    [StringLength(500)]
    [Unicode(false)]
    public string? ImagemUrl { get; set; }

    [InverseProperty("Artista")]
    public virtual ICollection<Album> Albuns { get; set; } = new List<Album>();

    [InverseProperty("Artista")]
    public virtual ICollection<Musica> Musicas { get; set; } = new List<Musica>();
}
