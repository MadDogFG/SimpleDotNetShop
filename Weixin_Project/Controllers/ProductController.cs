using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Weixin_Project.DTOs; // 引入DTOs命名空间
using Weixin_Project.Entities;
using Weixin_Project.Utils; // 引入Entities命名空间

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]")] // 基础路由 api/Product
    [ApiController]
    public class ProductController : ControllerBase // 注意这里是 ControllerBase，不是 AdminController
    {
        private readonly ApplicationDbContext _dbContext;

        public ProductController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 获取商品列表 (分页，只返回未软删除的商品)
        // GET: api/Product/List
        // 或者 GET: api/Product (如果希望这是默认的GET行为)
        [HttpGet("List")] // 定义一个明确的路由，如 api/Product/List
        public async Task<ActionResult<PagedResponse<ProductViewModel>>> GetProducts(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null, // 可选的搜索关键词
            [FromQuery] string? sortBy = null, // 可选的排序字段 (e.g., "price", "createTime")
            [FromQuery] bool sortAsc = true) // 可选的排序方向 (true为升序, false为降序)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 9; // 与前端默认一致
            if (pageSize > 50) pageSize = 50;

            // 查询时，全局查询过滤器 !p.IsDeleted 会自动生效
            var query = _dbContext.Products.AsQueryable();

            // 处理搜索条件
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || (p.Description != null && p.Description.Contains(searchTerm)));
            }

            // 处理排序 (简单示例，实际应用可能需要更复杂的排序逻辑)
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLowerInvariant())
                {
                    case "price":
                        query = sortAsc ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price);
                        break;
                    case "createtime": // 按创建时间排序
                        query = sortAsc ? query.OrderBy(p => p.CreateTime) : query.OrderByDescending(p => p.CreateTime);
                        break;
                    default: // 默认按ID排序或不排序
                        query = query.OrderByDescending(p => p.Id); // 默认新品在前
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(p => p.Id); // 默认排序
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductViewModel // 映射到ViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock, // 用户端也需要知道库存以便判断是否可购买
                    ImageUrl = p.ImageUrl,
                    IsDeleted = p.IsDeleted, // 虽然用户端主要看非删除的，但ViewModel可以保留此字段
                    CreateTime = p.CreateTime
                })
                .ToListAsync();

            var response = new PagedResponse<ProductViewModel>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
            return Ok(response);
        }

        // 获取单个商品详情 (只返回未软删除的商品)
        // GET: api/Product/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductViewModel>> GetProductById(int id)
        {
            // 全局查询过滤器 !p.IsDeleted 会自动生效
            var product = await _dbContext.Products
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                // 对于用户端，如果商品不存在或已被软删除，都应该返回404
                return NotFound(new { message = "商品未找到或已下架。" });
            }

            var productViewModel = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                IsDeleted = product.IsDeleted,
                CreateTime = product.CreateTime
            };
            return Ok(productViewModel);
        }
    }
}