using FluentValidation;
using Weixin_Project.DTOs; // 确保引入了DTOs命名空间

namespace Weixin_Project.Validators
{
    // 创建收货地址请求的验证器
    public class CreateAddressRequestDtoValidator : AbstractValidator<CreateAddressRequestDto>
    {
        public CreateAddressRequestDtoValidator()
        {
            RuleFor(x => x.ContactName)
                .NotEmpty().WithMessage("联系人姓名不能为空。")
                .Length(2, 50).WithMessage("联系人姓名长度必须在 {MinLength} 到 {MaxLength} 个字符之间。");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("手机号码不能为空。")
                .Matches(@"^1[3-9]\d{9}$").WithMessage("请输入有效的中国大陆手机号码。"); // 简单校验中国大陆手机号格式

            RuleFor(x => x.Province)
                .NotEmpty().WithMessage("省份不能为空。")
                .MaximumLength(50).WithMessage("省份名称不能超过 {MaxLength} 个字符。");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("城市不能为空。")
                .MaximumLength(50).WithMessage("城市名称不能超过 {MaxLength} 个字符。");

            RuleFor(x => x.StreetAddress)
                .NotEmpty().WithMessage("详细街道地址不能为空。")
                .Length(5, 200).WithMessage("详细街道地址长度必须在 {MinLength} 到 {MaxLength} 个字符之间。");

            RuleFor(x => x.PostalCode)
                .Matches(@"^\d{6}$").WithMessage("请输入有效的6位邮政编码。")
                .When(x => !string.IsNullOrEmpty(x.PostalCode)); // 邮政编码是可选的，所以只有当它不为空时才验证格式

            // IsDefault 是布尔值，通常不需要特定验证，除非有特殊业务规则
        }
    }

    // 更新收货地址请求的验证器
    // 通常，更新操作的验证规则与创建时相似。
    // 如果ID是通过URL传递的，则DTO中不需要ID字段。
    // PUT通常表示全量更新，所以字段大多还是必填。如果是PATCH部分更新，则验证逻辑会更复杂。
    public class UpdateAddressRequestDtoValidator : AbstractValidator<UpdateAddressRequestDto>
    {
        public UpdateAddressRequestDtoValidator()
        {
            // 规则与CreateAddressRequestDtoValidator基本一致
            // 如果有需要，可以在这里覆盖或添加特定的更新规则
            RuleFor(x => x.ContactName)
                .NotEmpty().WithMessage("联系人姓名不能为空。")
                .Length(2, 50).WithMessage("联系人姓名长度必须在 {MinLength} 到 {MaxLength} 个字符之间。");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("手机号码不能为空。")
                .Matches(@"^1[3-9]\d{9}$").WithMessage("请输入有效的中国大陆手机号码。");

            RuleFor(x => x.Province)
                .NotEmpty().WithMessage("省份不能为空。")
                .MaximumLength(50).WithMessage("省份名称不能超过 {MaxLength} 个字符。");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("城市不能为空。")
                .MaximumLength(50).WithMessage("城市名称不能超过 {MaxLength} 个字符。");

            RuleFor(x => x.StreetAddress)
                .NotEmpty().WithMessage("详细街道地址不能为空。")
                .Length(5, 200).WithMessage("详细街道地址长度必须在 {MinLength} 到 {MaxLength} 个字符之间。");

            RuleFor(x => x.PostalCode)
                .Matches(@"^\d{6}$").WithMessage("请输入有效的6位邮政编码。")
                .When(x => !string.IsNullOrEmpty(x.PostalCode));
        }
    }
}