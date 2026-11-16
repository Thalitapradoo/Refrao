using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Models;

/// <summary>"Por que esse refrão gruda": comentário de um usuário sobre uma música.</summary>
[Table("ComentariosRefrao")]
[Index("MusicaId", "UsuarioId", Name = "UQ_ComentariosRefrao_Musica_Usuario", IsUnique = true)]
public partial class ComentarioRefrao
{
    [Key]
    public int Id { get; set; }

    public int MusicaId { get; set; }

    public int UsuarioId { get; set; }

    [StringLength(300)]
    [Unicode(false)]
    public string Texto { get; set; } = null!;

    [Precision(0)]
    public DateTime DataCriacao { get; set; }

    [ForeignKey("MusicaId")]
    [InverseProperty("Comentarios")]
    public virtual Musica Musica { get; set; } = null!;

    [ForeignKey("UsuarioId")]
    public virtual Usuario Usuario { get; set; } = null!;
}
