using FluentValidation;
using Weixin_Project.DTOs;

namespace Weixin_Project.Validators
{
    public class LoginDtoVaildator : AbstractValidator<LoginRequestDto>
    {
        public LoginDtoVaildator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("请输入名字");
            RuleFor(x => x.Password).NotEmpty().WithMessage("请输入密码");
        }
    }
}
