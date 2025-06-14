using FluentValidation;
using Weixin_Project.DTOs;

namespace Weixin_Project.Validators
{
    // CreateProductRequestDto 的验证器
    public class CreateProductRequestDtoValidator : AbstractValidator<CreateProductRequestDto>
    {
        public CreateProductRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("商品名称不能为空")
                .MaximumLength(100).WithMessage("商品名称长度不能超过 {MaxLength} 个字符");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("商品描述长度不能超过 {MaxLength} 个字符"); // 描述可以为空

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("商品价格必须大于0");

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("商品库存不能为负数");

            RuleFor(x => x.ImageUrl)
                .MaximumLength(255).WithMessage("图片URL过长"); // URL可以为空
        }
    }

    // UpdateProductRequestDto 的验证器
    public class UpdateProductRequestDtoValidator : AbstractValidator<UpdateProductRequestDto>
    {
        public UpdateProductRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("商品名称不能为空")
                .MaximumLength(100).WithMessage("商品名称长度不能超过 {MaxLength} 个字符");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("商品描述长度不能超过 {MaxLength} 个字符");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("商品价格必须大于0");

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("商品库存不能为负数");

            RuleFor(x => x.ImageUrl)
                .MaximumLength(255).WithMessage("图片URL过长");
        }
    }
}