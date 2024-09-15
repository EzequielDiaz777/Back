using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Back.Models
{
    public class Aplicacion
    {
        [Key]
        [Display(Name = "Id de la aplicación")]
        public int Id { get; set; }

        [Required]
        public int LoteProveedorId { get; set; }
        
        public int? AgenteId { get; set; }
        
        [Required]
        public int Dosis { get; set; }

        [Required]
        public Estado Estado { get; set; }
        
        [ForeignKey(nameof(LoteProveedorId))]
        public LoteProveedor? LoteProveedor { get; set; }

        [ForeignKey(nameof(AgenteId))]
        public Agente? Agente { get; set; }
    }

    public enum Estado
    {
        Pendiente,
        Aplicada,
        Cancelada
    }
}
