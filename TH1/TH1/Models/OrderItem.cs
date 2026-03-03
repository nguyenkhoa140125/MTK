using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TH1.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }
        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }
    }
}
