using FluentValidation;
using Weixin_Project.DTOs; // 确保引入了DTOs命名空间
using Weixin_Project.Entities; // 如果需要访问OrderStatus等枚举

namespace Weixin_Project.Validators
{
    // 创建订单时单个商品项的验证器
    public class CreateOrderRequestItemDtoValidator : AbstractValidator<CreateOrderRequestItemDto>
    {
        public CreateOrderRequestItemDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("商品ID不能为空。")
                .GreaterThan(0).WithMessage("无效的商品ID。");

            RuleFor(x => x.Quantity)
                .NotEmpty().WithMessage("商品数量不能为空。")
                .InclusiveBetween(1, 100).WithMessage("单个商品购买数量必须在 {From} 到 {To} 之间。");
            // 这个100是假设的上限，您可以根据业务调整
        }
    }

    // 创建订单请求的验证器
    public class CreateOrderRequestDtoValidator : AbstractValidator<CreateOrderRequestDto>
    {
        public CreateOrderRequestDtoValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("订单商品列表不能为空。")
                .Must(items => items != null && items.Count > 0).WithMessage("订单至少需要包含一个商品。");

            // 对商品列表中的每一项进行验证 (使用上面定义的验证器)
            RuleForEach(x => x.Items)
                .SetValidator(new CreateOrderRequestItemDtoValidator());

            RuleFor(x => x.ShippingAddressId)
                .NotEmpty().WithMessage("收货地址ID不能为空。")
                .GreaterThan(0).WithMessage("无效的收货地址ID。");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("订单备注不能超过 {MaxLength} 个字符。")
                .When(x => !string.IsNullOrEmpty(x.Notes)); // 仅当备注不为空时才校验长度
        }
    }

    // 更新订单状态请求的验证器
    public class UpdateOrderStatusRequestDtoValidator : AbstractValidator<UpdateOrderStatusRequestDto>
    {
        public UpdateOrderStatusRequestDtoValidator()
        {
            RuleFor(x => x.NewStatus)
                .IsInEnum().WithMessage("无效的订单状态值。"); // 确保传入的状态是OrderStatus枚举中定义的值
        }
    }
}