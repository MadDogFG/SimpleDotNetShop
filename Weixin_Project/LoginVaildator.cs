using FluentValidation;

namespace Weixin_Project
{
    public class LoginVaildator : AbstractValidator<LoginRequest>
    {
        public LoginVaildator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("请输入名字");
            RuleFor(x => x.Password).NotEmpty().WithMessage("请输入密码");
        }
    }
}
