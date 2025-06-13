using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles ="Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;

        public AdminController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // 添加商品
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] AddProductRequest addProductRequest)
        {
            dbContext.Products.Add(new Product { Name=addProductRequest.Name,Description=addProductRequest.Description,Price=addProductRequest.Price,Stock=addProductRequest.Stock,ImageUrl=addProductRequest.ImageUrl});
            await dbContext.SaveChangesAsync();
            return Ok($"创建[{addProductRequest.Name}]商品成功！");
        }

        // 获取所有商品（包含软删除）
        [HttpGet]
        public async Task<IActionResult> GetAllProducts(int pageIndex = 1, int pageSize = 9)
        {
            try
            {
                // 获取总记录数 - 使用LongCount处理大数量
                int totalCount = await dbContext.Products.CountAsync();

                // 计算跳过记录数
                int skip = (pageIndex - 1) * pageSize;
                if (skip < 0) skip = 0;

                // 计算实际跳过记录数（不超过总记录数）
                long actualSkip = Math.Min(skip, totalCount);

                // 计算实际要取的数量
                int actualTake = Math.Min(pageSize, (int)(totalCount - actualSkip));

                // 获取分页数据
                var items = actualTake <= 0
                    ? new List<Product>()
                    : await dbContext.Products
                        .OrderBy(p => p.Id) // 确保有序分页
                        .Skip((int)actualSkip)
                        .Take(actualTake)
                        .ToListAsync();

                // 返回分页响应
                return Ok(new PagedResponse<Product>
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    Items = items
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "分页查询失败", Error = ex.Message });
            }
        }

        // 获取单个商品详情
        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await dbContext.Products.FindAsync(id);
            return product == null ? NotFound() : Ok(product);
        }

        // 修改商品
        [HttpPut]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] AddProductRequest updatedProduct)
        {
            var product = await dbContext.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.Stock = updatedProduct.Stock;
            product.ImageUrl = updatedProduct.ImageUrl;

            await dbContext.SaveChangesAsync();
            return Ok($"修改{updatedProduct.Name}商品成功");
        }

        // 恢复商品
        [HttpPut]
        public async Task<IActionResult> RestoreProduct(int id)
        {
            var product = await dbContext.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsDeleted = false;

            await dbContext.SaveChangesAsync();
            return Ok($"恢复{product.Name}商品成功");
        }

        // 软删除商品
        [HttpDelete]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await dbContext.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsDeleted = true; // 软删除
            await dbContext.SaveChangesAsync();
            return Ok($"删除商品{product.Name}成功");
        }
    }
}
