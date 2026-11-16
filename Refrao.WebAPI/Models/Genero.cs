using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Models;

[Table("Generos")]
[Index("Nome", Name = "UQ_Generos_Nome", IsUnique = true)]
public partial class Genero
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Nome { get; set; } = null!;

    [StringLength(500)]
    [Unicode(false)]
    public string? Descricao { get; set; }

    [JsonIgnore]
    [InverseProperty("Genero")]
    public virtual ICollection<Musica> Musicas { get; set; } = new List<Musica>();
}
