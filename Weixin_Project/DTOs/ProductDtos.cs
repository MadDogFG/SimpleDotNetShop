using System.ComponentModel.DataAnnotations; // 虽然我们用FluentValidation，但保留这些注解有时也有用，或者可以移除

namespace Weixin_Project.DTOs
{
    // 用于创建商品的请求DTO
    public class CreateProductRequestDto
    {
        [Required(ErrorMessage = "商品名称不能为空")] // 这个注解会被FluentValidation覆盖，但可以保留作为参考
        public string Name { get; set; }

        public string Description { get; set; } = "无"; // 描述可以为空，默认为"无"

        [Range(0.01, double.MaxValue, ErrorMessage = "价格必须大于0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "库存不能为负数")]
        public int Stock { get; set; } // 库存

        public string ImageUrl { get; set; } = "无"; // 图片URL，可以为空，默认为"无"
    }

    // 用于更新商品的请求DTO (通常和创建类似，但可能有ID)
    // 如果更新操作允许部分更新 (PATCH)，DTO结构可能会更复杂
    public class UpdateProductRequestDto
    {
        [Required(ErrorMessage = "商品名称不能为空")]
        public string Name { get; set; }
        public string Description { get; set; } = "无";
        [Range(0.01, double.MaxValue, ErrorMessage = "价格必须大于0")]
        public decimal Price { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "库存不能为负数")]
        public int Stock { get; set; }
        public string ImageUrl { get; set; } = "无";
        // 注意：更新时，ID通常从URL路径中获取，而不是在请求体中。
        // 如果isDeleted等字段也允许通过此DTO更新，则需添加。
    }

    // 用于API响应的商品视图模型/DTO
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; }
        public bool IsDeleted { get; set; } // 管理员可能需要看到软删除状态
        public DateTime CreateTime { get; set; }
    }
}