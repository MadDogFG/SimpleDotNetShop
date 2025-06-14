using Microsoft.AspNetCore.Identity; // ASP.NET Core Identity 相关
using System.ComponentModel.DataAnnotations; // 数据注解，用于验证和数据库映射
using System.ComponentModel.DataAnnotations.Schema; // Schema注解，用于更细致的数据库映射

namespace Weixin_Project.Entities
{
    public class Address
    {
        [Key] // 主键
        public int Id { get; set; }

        [Required] // 必填项
        public string UserId { get; set; } // 用户ID，关联到IdentityUser
        [ForeignKey("UserId")] // 外键配置
        public IdentityUser User { get; set; } // 导航属性，关联的用户对象

        [Required(ErrorMessage = "联系人姓名不能为空")] // 必填项，并指定错误消息
        [MaxLength(100)] // 最大长度100
        public string ContactName { get; set; } // 联系人姓名

        [Required(ErrorMessage = "手机号码不能为空")]
        [MaxLength(20)]
        [Phone(ErrorMessage = "请输入有效的手机号码")] // 验证是否为电话号码格式
        public string PhoneNumber { get; set; } // 手机号码

        [Required(ErrorMessage = "详细地址不能为空")]
        [MaxLength(200)]
        public string StreetAddress { get; set; } // 街道详细地址

        [MaxLength(100)]
        public string City { get; set; } // 城市

        [MaxLength(100)]
        public string Province { get; set; } // 省份

        [MaxLength(20)]
        public string PostalCode { get; set; } // 邮政编码

        public bool IsDefault { get; set; } = false; // 是否为默认地址，默认为false
        public bool IsDeleted { get; set; } = false; // 软删除标记，默认为false
    }
}