using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Weixin_Project.Entities
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; } // 购物车项ID

        public int CartId { get; set; } // 所属购物车的ID
        [ForeignKey("CartId")]
        public Cart Cart { get; set; } // 导航属性，关联的购物车

        public int ProductId { get; set; } // 商品ID
        [ForeignKey("ProductId")]
        public Product Product { get; set; } // 导航属性，关联的商品

        [Range(1, int.MaxValue, ErrorMessage = "商品数量必须大于0")]
        public int Quantity { get; set; } // 商品数量

        public DateTime DateAdded { get; set; } = DateTime.UtcNow; // 添加到购物车的时间
    }
}