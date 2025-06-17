using FluentValidation;
using Weixin_Project.DTOs;

namespace Weixin_Project.Validators
{
    public class AdminResetPasswordRequestDtoValidator : AbstractValidator<AdminResetPasswordRequestDto>
    {
        public AdminResetPasswordRequestDtoValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("新密码不能为空。")
                .MinimumLength(6).WithMessage("新密码长度不能少于 6 个字符。")
                .MaximumLength(100).WithMessage("新密码长度不能超过 100 个字符。");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("确认密码不能为空。")
                .Equal(x => x.NewPassword).WithMessage("新密码和确认密码不匹配。");
        }
    }
}
