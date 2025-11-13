using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10439052_CLDV_POE.Models
{
    [Table("Cart")]
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? CustomerUsername { get; set; }

        [MaxLength(100)]
        public string? ProductId { get; set; }

        public int Quantity { get; set; }
    }
}

