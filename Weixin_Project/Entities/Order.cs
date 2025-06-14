using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Weixin_Project.Entities
{
    // 订单状态枚举
    public enum OrderStatus
    {
        Paid,           // 待发货
        Shipped,        // 已发货
        Completed,      // 已完成 (例如用户确认收货或评价后)
        Cancelled,      // 已取消 (用户取消或系统取消)
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // 用户ID
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } // 关联的用户

        public DateTime OrderDate { get; set; } = DateTime.UtcNow; // 下单日期，默认为当前UTC时间

        [Required]
        [Column(TypeName = "decimal(18,2)")] // 数据库中存储为decimal类型，总共18位，其中2位是小数
        public decimal TotalAmount { get; set; } // 订单总金额

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Paid; // 订单状态，默认为待付款

        [Required(ErrorMessage = "收货地址不能为空")]
        public int ShippingAddressId { get; set; } // 收货地址ID
        [ForeignKey("ShippingAddressId")]
        public Address ShippingAddress { get; set; } // 关联的收货地址对象

        [MaxLength(500)]
        public string? Notes { get; set; } // 订单备注，可为空

        // 导航属性，一个订单包含多个订单项
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}