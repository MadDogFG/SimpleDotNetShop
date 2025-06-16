using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Weixin_Project.DTOs;
using Weixin_Project.Entities;
using Weixin_Project.Utils;

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // 确保只有Admin角色的用户可以访问
    public class AdminOrderController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminOrderController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 获取订单列表 (可按状态筛选、分页)
        [HttpGet]
        public async Task<ActionResult<PagedResponse<OrderViewModel>>> GetOrders(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null, // 允许按订单状态字符串筛选
            [FromQuery] string? userId = null, // 允许按用户ID筛选 (可选)
            [FromQuery] string? orderNumber = null) // 允许按订单号（ID）搜索
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50; // 限制每页最大数量

            var query = _dbContext.Orders
                .Include(o => o.User) // 包含用户信息，用于显示用户名
                .Include(o => o.ShippingAddress) // 包含收货地址信息
                .Include(o => o.OrderItems) // 包含订单项
                .ThenInclude(oi => oi.Product) // 在订单项中包含商品信息
                .OrderByDescending(o => o.OrderDate) // 默认按下单时间降序排序
                .AsQueryable(); // 转换为 IQueryable 以便后续添加条件

            // 订单状态筛选
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            {
                query = query.Where(o => o.Status == orderStatus);
            }

            // 用户ID筛选
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId);
            }

            // 订单号搜索（这里假设订单号就是订单的ID）
            if (!string.IsNullOrEmpty(orderNumber) && int.TryParse(orderNumber, out int orderId))
            {
                query = query.Where(o => o.Id == orderId);
            }

            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var orderViewModels = orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                UserId = o.UserId,
                UserName = o.User?.UserName ?? "N/A", // 如果User为null则显示N/A
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                // StatusText 在OrderViewModel的DTO中已经通过get属性实现
                ShippingAddress = o.ShippingAddress == null ? null : new AddressViewModel // 防止ShippingAddress为null
                {
                    Id = o.ShippingAddress.Id,
                    ContactName = o.ShippingAddress.ContactName,
                    PhoneNumber = o.ShippingAddress.PhoneNumber,
                    Province = o.ShippingAddress.Province,
                    City = o.ShippingAddress.City,
                    StreetAddress = o.ShippingAddress.StreetAddress,
                    PostalCode = o.ShippingAddress.PostalCode,
                    IsDefault = o.ShippingAddress.IsDefault,
                },
                OrderItems = o.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "商品已删除", // 如果关联的Product被硬删除了
                    ProductImageUrl = oi.Product?.ImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                    // Subtotal 在OrderItemViewModel的DTO中已经通过get属性实现
                }).ToList(),
                Notes = o.Notes
            }).ToList();

            var response = new PagedResponse<OrderViewModel>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = orderViewModels,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(response);
        }

        // 获取单个订单详情
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderViewModel>> GetOrderById(int id)
        {
            var order = await _dbContext.Orders
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound($"未能找到ID为 {id} 的订单");
            }

            var orderViewModel = new OrderViewModel
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "N/A",
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ShippingAddress = order.ShippingAddress == null ? null : new AddressViewModel
                {
                    Id = order.ShippingAddress.Id,
                    ContactName = order.ShippingAddress.ContactName,
                    PhoneNumber = order.ShippingAddress.PhoneNumber,
                    Province = order.ShippingAddress.Province,
                    City = order.ShippingAddress.City,
                    StreetAddress = order.ShippingAddress.StreetAddress,
                    PostalCode = order.ShippingAddress.PostalCode,
                    IsDefault = order.ShippingAddress.IsDefault,
                },
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "商品信息缺失",
                    ProductImageUrl = oi.Product?.ImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList(),
                Notes = order.Notes
            };

            return Ok(orderViewModel);
        }

        // 更新订单状态 (管理员操作)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequestDto updateStatusDto)
        {
            // FluentValidation会先验证 updateStatusDto
            var order = await _dbContext.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound($"未能找到ID为 {id} 的订单");
            }

            // 可以在这里添加更复杂的订单状态流转逻辑检查，例如：
            // if (order.Status == OrderStatus.Completed && newStatus == OrderStatus.Pending)
            // {
            //     return BadRequest("已完成的订单不能改回待处理状态");
            // }

            order.Status = updateStatusDto.NewStatus;
            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = $"订单 {id} 状态已更新为 {updateStatusDto.NewStatus}" });
            // 或者返回更新后的订单详情
            // return await GetOrderById(id);
        }

        // (可选) 获取所有订单状态的列表，用于前端筛选下拉框
        [HttpGet]
        public IActionResult GetOrderStatuses()
        {
            var statuses = Enum.GetValues(typeof(OrderStatus))
                .Cast<OrderStatus>()
                .Select(s => new { value = s.ToString(), name = GetOrderStatusName(s) }) // value用于请求，name用于显示
                .ToList();
            return Ok(statuses);
        }

        // 辅助方法：获取订单状态的中文名称 (可以根据需要扩展)
        private string GetOrderStatusName(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Paid => "待发货", // 已付款也视为待发货
                OrderStatus.Shipped => "已发货",
                OrderStatus.Completed => "已完成",
                OrderStatus.Cancelled => "已取消",
                _ => status.ToString() // 默认返回枚举的字符串形式
            };
        }
    }
}