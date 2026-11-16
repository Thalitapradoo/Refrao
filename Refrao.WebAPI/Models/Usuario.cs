using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Refrao.WebAPI.Models;

[Table("Usuarios")]
[Index("Email", Name = "UQ_Usuarios_Email", IsUnique = true)]
public partial class Usuario
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string Nome { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string SenhaHash { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string Perfil { get; set; } = null!;

    [Precision(0)]
    public DateTime DataCadastro { get; set; }

    public bool Ativo { get; set; }
}
