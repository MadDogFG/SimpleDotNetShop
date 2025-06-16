using FluentValidation;
using Weixin_Project.DTOs;

namespace Weixin_Project.Validators
{
    // AddToCartRequestDto 的验证器
    public class AddToCartRequestDtoValidator : AbstractValidator<AddToCartRequestDto>
    {
        public AddToCartRequestDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("商品ID不能为空。")
                .GreaterThan(0).WithMessage("无效的商品ID。");

            RuleFor(x => x.Quantity)
                .NotEmpty().WithMessage("商品数量不能为空。")
                .InclusiveBetween(1, 10).WithMessage("添加数量必须在 {From} 到 {To} 之间。"); // 假设单次添加上限为10
        }
    }

    // UpdateCartItemQuantityRequestDto 的验证器
    public class UpdateCartItemQuantityRequestDtoValidator : AbstractValidator<UpdateCartItemQuantityRequestDto>
    {
        public UpdateCartItemQuantityRequestDtoValidator()
        {
            RuleFor(x => x.CartItemId)
                .NotEmpty().WithMessage("购物车项ID不能为空。")
                .GreaterThan(0).WithMessage("无效的购物车项ID。");

            RuleFor(x => x.Quantity)
                .NotNull().WithMessage("商品数量不能为空。") // NotNull 允许0
                .GreaterThanOrEqualTo(0).WithMessage("商品数量不能为负数。") // 数量可以为0（表示移除）
                .LessThanOrEqualTo(100).WithMessage("商品数量不能超过100。"); // 假设购物车单个商品上限为100
        }
    }

    // RemoveCartItemRequestDto 的验证器 (如果使用这个DTO)
    public class RemoveCartItemRequestDtoValidator : AbstractValidator<RemoveCartItemRequestDto>
    {
        public RemoveCartItemRequestDtoValidator()
        {
            RuleFor(x => x.CartItemId)
                .NotEmpty().WithMessage("购物车项ID不能为空。")
                .GreaterThan(0).WithMessage("无效的购物车项ID。");
        }
    }

    // 新增：批量删除购物车项请求的验证器
    public class RemoveCartItemsRequestDtoValidator : AbstractValidator<RemoveCartItemsRequestDto>
    {
        public RemoveCartItemsRequestDtoValidator()
        {
            RuleFor(x => x.CartItemIds)
                .NotNull().WithMessage("购物车项ID列表不能为null。")
                .NotEmpty().WithMessage("购物车项ID列表不能为空。");

            RuleForEach(x => x.CartItemIds)
                .GreaterThan(0).WithMessage("购物车项ID必须是有效的正整数。");
        }
    }

}