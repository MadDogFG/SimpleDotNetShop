using FluentValidation;
using Weixin_Project.DTOs;

namespace Weixin_Project.Validators
{
    public class RegisterDtoVaildator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterDtoVaildator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("请输入名字");
            RuleFor(x => x.Password).NotEmpty().WithMessage("请输入密码");
            RuleFor(x => x.Password2).NotEmpty().WithMessage("请重复密码").Equal(x => x.Password).WithMessage("两遍密码应当相同");
        }
    }
}
