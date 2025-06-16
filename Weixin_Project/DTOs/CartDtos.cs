using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // 参考用

namespace Weixin_Project.DTOs
{
    // 用于API响应的购物车项视图模型
    public class CartItemViewModel
    {
        public int CartItemId { get; set; } // 购物车项自身的ID，方便前端做key或更新
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; } // 商品当前价格
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public int Stock { get; set; } // 商品当前库存 (重要，用于前端判断是否可购买)
        public decimal Subtotal => Price * Quantity; // 小计
    }

    // 用于API响应的购物车整体视图模型
    public class CartViewModel
    {
        public int CartId { get; set; } // 购物车ID
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal TotalAmount => Items.Sum(item => item.Subtotal); // 购物车总金额
        public int TotalItemsCount => Items.Sum(item => item.Quantity); // 购物车总商品数量
        public DateTime LastModifiedDate { get; set; }
    }

    // 添加商品到购物车的请求DTO
    public class AddToCartRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // 更新购物车中商品数量的请求DTO
    public class UpdateCartItemQuantityRequestDto
    {
        public int CartItemId { get; set; } // 使用 CartItem 的 ID 来定位要更新的项
        public int Quantity { get; set; } // 新的数量，如果为0，则表示从购物车移除该商品项
    }

    // 从购物车移除单个商品项的请求DTO 
    public class RemoveCartItemRequestDto
    {
        public int CartItemId { get; set; }
    }
    // 批量删除
    public class RemoveCartItemsRequestDto
    {
        [Required(ErrorMessage = "要删除的购物车项ID列表不能为空")]
        public List<int> CartItemIds { get; set; } = new List<int>();
    }
}