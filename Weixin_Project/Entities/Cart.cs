using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Weixin_Project.Entities
{
    public class Cart
    {
        [Key]
        public int Id { get; set; } // 购物车ID

        [Required]
        public string UserId { get; set; } // 关联的用户ID
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } // 导航属性，关联的用户

        // 一个购物车包含多个购物车项
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow; // 最后修改时间，方便管理
    }
}