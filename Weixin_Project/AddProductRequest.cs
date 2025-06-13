using System.ComponentModel.DataAnnotations;

namespace Weixin_Project
{
    public class AddProductRequest
    {
        [Required] public string Name { get; set; }
        public string Description { get; set; } = "无";
        [Range(0.01, double.MaxValue)] public decimal Price { get; set; }
        public int Stock { get; set; } // 库存
        public string ImageUrl { get; set; } = "无"; // 图片URL
    }
}
