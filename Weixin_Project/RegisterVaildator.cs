using FluentValidation;

namespace Weixin_Project
{
    public class RegisterVaildator : AbstractValidator<RegisterRequest>
    {
        public RegisterVaildator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("请输入名字");
            RuleFor(x => x.Password).NotEmpty().WithMessage("请输入密码");
            RuleFor(x => x.Password2).NotEmpty().WithMessage("请重复密码").Equal(x => x.Password).WithMessage("两遍密码应当相同");
        }
    }
}
