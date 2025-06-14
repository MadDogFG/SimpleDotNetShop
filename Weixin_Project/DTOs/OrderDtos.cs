using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // 可以保留用于参考，但主要依赖FluentValidation
using Weixin_Project.Entities; // 为了使用OrderStatus枚举

namespace Weixin_Project.DTOs
{
    // 订单项视图模型 (用于显示订单中的单个商品信息)
    public class OrderItemViewModel
    {
        public int ProductId { get; set; } // 商品ID
        public string ProductName { get; set; } // 商品名称
        public string? ProductImageUrl { get; set; } // 商品图片URL (可能为空)
        public int Quantity { get; set; } // 购买数量
        public decimal UnitPrice { get; set; } // 下单时单价
        public decimal Subtotal => Quantity * UnitPrice; // 小计金额 (只读属性，自动计算)
    }

    // 订单详情视图模型 (用于管理员查看或用户查看订单详情)
    public class OrderViewModel
    {
        public int Id { get; set; } // 订单ID
        public string UserId { get; set; } // 用户ID
        public string UserName { get; set; } // 用户名 (方便管理员查看)
        public DateTime OrderDate { get; set; } // 下单日期
        public decimal TotalAmount { get; set; } // 订单总金额
        public OrderStatus Status { get; set; } // 订单状态 (枚举)
        public string StatusText // 订单状态的文本描述 (只读属性)
        {
            get
            {
                // 这个逻辑也可以放在服务层或辅助类中，这里为了DTO自包含
                return Status switch
                {
                    OrderStatus.Paid => "待发货",
                    OrderStatus.Shipped => "已发货",
                    OrderStatus.Completed => "已完成",
                    OrderStatus.Cancelled => "已取消",
                    _ => Status.ToString()
                };
            }
        }
        public AddressViewModel? ShippingAddress { get; set; } // 收货地址信息 (可能为空，如果订单不需要地址)
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>(); // 订单项列表
        public string? Notes { get; set; } // 订单备注 (可能为空)
    }

    // 用户订单列表项视图模型 (简化版，用于用户查看自己的订单列表)
    public class UserOrderListItemViewModel
    {
        public int Id { get; set; } // 订单ID
        public DateTime OrderDate { get; set; } // 下单日期
        public decimal TotalAmount { get; set; } // 订单总金额
        public OrderStatus Status { get; set; } // 订单状态
        public string StatusText => GetStatusText(Status); // 订单状态文本
        public string? FirstProductImageUrl { get; set; } // 订单中第一个商品的图片URL (用于列表预览)
        public string? FirstProductName { get; set; } // 订单中第一个商品的名称
        public int TotalItemsCount { get; set; } // 订单中商品总数 (所有OrderItems的Quantity之和)

        private string GetStatusText(OrderStatus status) // 辅助方法获取状态文本
        {
            return status switch
            {
                OrderStatus.Paid => "待发货",
                OrderStatus.Shipped => "已发货",
                OrderStatus.Completed => "已完成",
                OrderStatus.Cancelled => "已取消",
                _ => status.ToString()
            };
        }
    }

    // 创建订单时，请求中的单个商品项 DTO
    public class CreateOrderRequestItemDto
    {
        // [Required(ErrorMessage = "商品ID不能为空")] // FluentValidation 会处理
        // [Range(1, int.MaxValue, ErrorMessage = "无效的商品ID")]
        public int ProductId { get; set; } // 商品ID

        // [Required(ErrorMessage = "商品数量不能为空")]
        // [Range(1, 100, ErrorMessage = "单次购买数量必须在1到100之间")]
        public int Quantity { get; set; } // 购买数量
    }

    // 创建订单的请求模型 DTO (用户提交订单时使用)
    public class CreateOrderRequestDto
    {
        // [Required(ErrorMessage = "订单商品列表不能为空")]
        // [MinLength(1, ErrorMessage = "订单至少需要包含一个商品")]
        public List<CreateOrderRequestItemDto> Items { get; set; } = new List<CreateOrderRequestItemDto>(); // 商品项列表

        // [Required(ErrorMessage = "收货地址ID不能为空")]
        // [Range(1, int.MaxValue, ErrorMessage = "无效的收货地址ID")]
        public int ShippingAddressId { get; set; } // 用户选择的收货地址ID

        // [MaxLength(500, ErrorMessage = "备注不能超过500个字符")]
        public string? Notes { get; set; } // 订单备注，可选
    }

    // 管理员更新订单状态的请求模型 DTO
    public class UpdateOrderStatusRequestDto
    {
        // [Required(ErrorMessage = "订单状态不能为空")] // FluentValidation 会处理
        public OrderStatus NewStatus { get; set; } // 新的订单状态
    }
}