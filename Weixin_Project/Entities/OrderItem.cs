using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Weixin_Project.Entities
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; } // 所属订单ID
        [ForeignKey("OrderId")]
        public Order Order { get; set; } // 关联的订单

        public int ProductId { get; set; } // 商品ID
        [ForeignKey("ProductId")]
        public Product Product { get; set; } // 关联的商品 (Product实体已在您提供的代码中)

        [Range(1, int.MaxValue, ErrorMessage = "商品数量必须大于0")] // 数量范围验证
        public int Quantity { get; set; } // 商品数量

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // 商品单价 (下单时的价格，可能与当前商品价格不同)
    }
}