using System.ComponentModel.DataAnnotations;

namespace Weixin_Project.DTOs
{
    /// <summary>
    /// 用户视图模型
    /// </summary>
    public class UserViewModel
    {
        public string Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool LockoutEnabled { get; set; } // 是否启用锁定功能
        public DateTimeOffset? LockoutEnd { get; set; } // 锁定结束时间
        public bool IsLockedOut { get; set; } // 是否当前被锁定
        public int AccessFailedCount { get; set; } // 登录失败次数
        public List<string> Roles { get; set; } = new List<string>(); // 用户角色
    }

    /// <summary>
    /// 管理员重置用户密码请求DTO
    /// </summary>
    public class AdminResetPasswordRequestDto
    {
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(100, ErrorMessage = "密码长度必须在 {2} 到 {1} 个字符之间。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "新密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
