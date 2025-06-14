using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // 需要引入Linq
using System.Threading.Tasks;
using Weixin_Project.DTOs; // 引入DTOs命名空间
using Weixin_Project.Entities; // 如果还需要Product实体 (例如在映射时)

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // 确保只有Admin角色的用户可以访问
    public class AdminProductController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext; // 使用下划线前缀作为私有字段的约定

        public AdminProductController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 添加商品
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto createProductDto)
        {
            // FluentValidation 会在进入这个Action之前自动验证 createProductDto
            // 如果验证失败，会自动返回400 Bad Request，并附带错误信息
            // 所以这里通常不需要再次检查 ModelState.IsValid，除非你有特殊的处理逻辑

            var product = new Product
            {
                Name = createProductDto.Name,
                Description = string.IsNullOrEmpty(createProductDto.Description) ? "无" : createProductDto.Description,
                Price = createProductDto.Price,
                Stock = createProductDto.Stock,
                ImageUrl = string.IsNullOrEmpty(createProductDto.ImageUrl) ? "无" : createProductDto.ImageUrl,
                CreateTime = DateTime.UtcNow,
                IsDeleted = false // 新建商品默认不是软删除状态
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            // 返回创建的商品信息 (使用 ProductViewModel)
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
            // 通常会返回一个 201 Created 状态码，并包含新资源的URI和内容
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, productViewModel);
            // 或者简单返回Ok:
            // return Ok($"商品 [{product.Name}] 创建成功!");
        }

        // 获取所有商品 (包含软删除的，因为是管理员接口) - 使用分页
        [HttpGet]
        public async Task<ActionResult<PagedResponse<ProductViewModel>>> GetAllProducts(int pageIndex = 1, int pageSize = 10)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // 限制每页最大数量

            // 管理员可以查看所有商品，包括软删除的，所以使用 IgnoreQueryFilters()
            var query = _dbContext.Products.IgnoreQueryFilters().OrderByDescending(p => p.CreateTime);

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
                    Stock = p.Stock,
                    ImageUrl = p.ImageUrl,
                    IsDeleted = p.IsDeleted, // 管理员需要看到这个状态
                    CreateTime = p.CreateTime
                })
                .ToListAsync();

            var response = new PagedResponse<ProductViewModel>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize) // 添加总页数
            };
            return Ok(response);
        }

        // 获取单个商品详情 (管理员接口，可以获取软删除的商品)
        [HttpGet("{id}")] // 将id作为路由参数
        public async Task<ActionResult<ProductViewModel>> GetProductById(int id)
        {
            var product = await _dbContext.Products
                                        .IgnoreQueryFilters() // 管理员可以查看软删除的
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound($"未能找到ID为 {id} 的商品");
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

        // 修改商品
        [HttpPut("{id}")] // id 从URL路径获取
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequestDto updatedProductDto)
        {
            // FluentValidation 自动验证 updatedProductDto

            // 管理员操作，可以获取包括软删除的商品进行修改
            var product = await _dbContext.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound($"未能找到ID为 {id} 的商品进行修改");
            }

            product.Name = updatedProductDto.Name;
            product.Description = string.IsNullOrEmpty(updatedProductDto.Description) ? "无" : updatedProductDto.Description;
            product.Price = updatedProductDto.Price;
            product.Stock = updatedProductDto.Stock;
            product.ImageUrl = string.IsNullOrEmpty(updatedProductDto.ImageUrl) ? "无" : updatedProductDto.ImageUrl;
            // 其他需要更新的字段，比如 IsDeleted（如果允许通过这个接口修改）

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) // 处理并发冲突，虽然在这个简单场景下概率不高
            {
                if (!_dbContext.Products.IgnoreQueryFilters().Any(e => e.Id == id))
                {
                    return NotFound($"商品ID {id} 在尝试更新时已被删除。");
                }
                else
                {
                    throw; // 重新抛出异常，让全局异常处理器处理
                }
            }
            // return Ok($"商品 [{product.Name}] 修改成功!");
            return NoContent(); // HTTP 204 No Content 是更新成功的常用响应，表示操作成功但响应体中无内容
        }

        // 软删除商品
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            // 管理员操作，即使商品已被软删除，理论上这个操作也是幂等的
            var product = await _dbContext.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound($"未能找到ID为 {id} 的商品进行删除");
            }

            if (product.IsDeleted)
            {
                return Ok($"商品 [{product.Name}] 已经是删除状态"); // 或者返回 204 NoContent
            }

            product.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            return Ok($"商品 [{product.Name}] 已被软删除");
            // 或者 return NoContent();
        }

        // 恢复软删除的商品
        [HttpPut("Restore/{id}")] // 使用不同的路径区分操作
        public async Task<IActionResult> RestoreProduct(int id)
        {
            var product = await _dbContext.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound($"未能找到ID为 {id} 的商品进行恢复");
            }

            if (!product.IsDeleted)
            {
                return Ok($"商品 [{product.Name}] 本来就不是删除状态");
            }

            product.IsDeleted = false;
            await _dbContext.SaveChangesAsync();

            return Ok($"商品 [{product.Name}] 已成功恢复");
        }
    }
}