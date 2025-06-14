// PagedResponse.cs
namespace Weixin_Project // 确保命名空间正确
{
    public class PagedResponse<T>
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; } // 新增总页数
        public List<T> Items { get; set; }
    }
}