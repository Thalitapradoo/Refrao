using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Models;

/// <summary>Sugestão de música enviada por um usuário. O administrador aprova ou recusa.</summary>
[Table("Sugestoes")]
public partial class Sugestao
{
    [Key]
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    [StringLength(150)]
    [Unicode(false)]
    public string Titulo { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string ArtistaNome { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string? AlbumNome { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Observacao { get; set; }

    /// <summary>Pendente, Aprovada ou Recusada.</summary>
    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [StringLength(300)]
    [Unicode(false)]
    public string? RespostaAdmin { get; set; }

    public int? MusicaId { get; set; }

    [Precision(0)]
    public DateTime DataCriacao { get; set; }

    [Precision(0)]
    public DateTime? DataResposta { get; set; }

    [ForeignKey("UsuarioId")]
    public virtual Usuario Usuario { get; set; } = null!;

    [ForeignKey("MusicaId")]
    public virtual Musica? Musica { get; set; }
}
