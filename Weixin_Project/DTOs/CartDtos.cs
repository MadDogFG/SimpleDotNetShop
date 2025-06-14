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
        // [Required(ErrorMessage = "商品ID不能为空")] // FluentValidation处理
        // [Range(1, int.MaxValue, ErrorMessage = "无效的商品ID")]
        public int ProductId { get; set; }

        // [Required(ErrorMessage = "商品数量不能为空")]
        // [Range(1, 10, ErrorMessage = "添加数量必须在1到10之间")] // 假设一次最多加10个
        public int Quantity { get; set; }
    }

    // 更新购物车中商品数量的请求DTO
    public class UpdateCartItemQuantityRequestDto
    {
        // [Required(ErrorMessage = "购物车项ID不能为空")] // 或者用 ProductId
        // [Range(1, int.MaxValue, ErrorMessage = "无效的购物车项ID")]
        public int CartItemId { get; set; } // 使用 CartItem 的 ID 来定位要更新的项

        // [Required(ErrorMessage = "商品数量不能为空")]
        // [Range(0, 100, ErrorMessage = "商品数量必须在0到100之间")] // 0表示移除该项
        public int Quantity { get; set; } // 新的数量，如果为0，则表示从购物车移除该商品项
    }

    // 从购物车移除单个商品项的请求DTO (可以不需要，直接用UpdateCartItemQuantityRequestDto Quantity=0)
    // 但为了接口语义清晰，可以单独定义
    public class RemoveCartItemRequestDto
    {
        // [Required(ErrorMessage = "购物车项ID不能为空")]
        public int CartItemId { get; set; }
    }
}