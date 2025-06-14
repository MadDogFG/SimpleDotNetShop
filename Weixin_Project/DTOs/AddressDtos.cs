using System.ComponentModel.DataAnnotations; // 可以保留用于参考，但主要依赖FluentValidation

namespace Weixin_Project.DTOs
{
    // 收货地址视图模型 (用于显示地址信息)
    public class AddressViewModel
    {
        public int Id { get; set; } // 地址ID
        public string ContactName { get; set; } // 联系人姓名
        public string PhoneNumber { get; set; } // 手机号码
        public string FullAddress => $"{Province}{City}{StreetAddress}"; // 完整地址 (只读属性，自动拼接)
        public string StreetAddress { get; set; } // 街道详细地址
        public string City { get; set; } // 城市
        public string Province { get; set; } // 省份
        public string? PostalCode { get; set; } // 邮政编码 (可能为空)
        public bool IsDefault { get; set; } // 是否为默认地址
    }

    // 创建收货地址的请求模型 DTO
    public class CreateAddressRequestDto
    {
        // [Required(ErrorMessage = "联系人姓名不能为空")] // FluentValidation 会处理
        // [StringLength(50, MinimumLength = 2, ErrorMessage = "联系人姓名长度必须在2到50个字符之间")]
        public string ContactName { get; set; }

        // [Required(ErrorMessage = "手机号码不能为空")]
        // [Phone(ErrorMessage = "请输入有效的手机号码")]
        // [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "请输入有效的中国大陆手机号码")]
        public string PhoneNumber { get; set; }

        // [Required(ErrorMessage = "省份不能为空")]
        // [StringLength(50, ErrorMessage = "省份名称过长")]
        public string Province { get; set; }

        // [Required(ErrorMessage = "城市不能为空")]
        // [StringLength(50, ErrorMessage = "城市名称过长")]
        public string City { get; set; }

        // 可以考虑添加区/县
        // public string District { get; set; }

        // [Required(ErrorMessage = "详细街道地址不能为空")]
        // [StringLength(200, MinimumLength = 5, ErrorMessage = "详细地址长度必须在5到200个字符之间")]
        public string StreetAddress { get; set; }

        // [StringLength(10, ErrorMessage = "邮政编码过长")]
        // [RegularExpression(@"^\d{6}$", ErrorMessage = "请输入有效的6位邮政编码")] // 中国邮编格式
        public string? PostalCode { get; set; } // 邮政编码，可选

        public bool IsDefault { get; set; } = false; // 是否设为默认地址
    }

    // 更新收货地址的请求模型 DTO (通常和创建类似，ID通过URL传递)
    public class UpdateAddressRequestDto : CreateAddressRequestDto
    {
        // 如果有特定于更新的字段，可以在这里添加
        // 例如，如果允许只更新部分字段，那么这个DTO的属性可以都设为可空，
        // 然后在服务层判断哪些字段被提供了值。但通常 PUT 表示全量更新。
    }
}