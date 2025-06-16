// PagedResponse.cs
namespace Weixin_Project.Utils // 确保命名空间正确
{
    public class PagedResponse<T>
    {
        public int PageIndex { get; set; } // 页码
        public int PageSize { get; set; } // 每页大小
        public int TotalCount { get; set; } // 总记录数
        public int TotalPages { get; set; } // 新增总页数
        public List<T> Items { get; set; } // 当前页的数据项
    }
}