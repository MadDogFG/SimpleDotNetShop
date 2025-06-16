using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // 用于获取当前登录用户
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // 用于获取UserId
using System.Threading.Tasks;
using Weixin_Project.DTOs;
using Weixin_Project.Entities;
using Weixin_Project.Utils;

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize] // 表示需要用户登录才能访问此控制器下的所有接口
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager; // 用于获取用户信息

        public OrderController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // 用户创建订单
        [HttpPost]
        public async Task<ActionResult<OrderViewModel>> CreateOrder([FromBody] CreateOrderRequestDto createOrderDto)
        {
            // FluentValidation 会自动验证 createOrderDto

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // 获取当前登录用户的ID
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息，请重新登录。" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "用户信息不存在。" });
            }

            // 验证收货地址是否存在且属于当前用户
            var shippingAddress = await _dbContext.Addresses
                                        .FirstOrDefaultAsync(a => a.Id == createOrderDto.ShippingAddressId && a.UserId == userId && !a.IsDeleted);
            if (shippingAddress == null)
            {
                return BadRequest(new { message = "无效的收货地址或地址不属于当前用户。" });
            }

            if (createOrderDto.Items == null || !createOrderDto.Items.Any())
            {
                return BadRequest(new { message = "订单中必须至少包含一个商品。" });
            }

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            // 使用事务来确保订单创建和库存扣减的原子性
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var itemDto in createOrderDto.Items)
                    {
                        var product = await _dbContext.Products.FindAsync(itemDto.ProductId);
                        if (product == null || product.IsDeleted) // 检查商品是否存在且未被软删除
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { message = $"商品ID {itemDto.ProductId} 无效或已下架。" });
                        }

                        if (product.Stock < itemDto.Quantity)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { message = $"商品 '{product.Name}' 库存不足，当前库存 {product.Stock}。" });
                        }

                        // 扣减库存
                        product.Stock -= itemDto.Quantity;
                        _dbContext.Products.Update(product);

                        var orderItem = new OrderItem
                        {
                            ProductId = product.Id,
                            Quantity = itemDto.Quantity,
                            UnitPrice = product.Price // 使用商品当前价格作为下单时单价
                        };
                        orderItems.Add(orderItem);
                        totalAmount += orderItem.Quantity * orderItem.UnitPrice;
                    }

                    // 创建订单主体
                    var newOrder = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.UtcNow,
                        TotalAmount = totalAmount,
                        Status = OrderStatus.Paid, // 新订单默认为待发货
                        ShippingAddressId = shippingAddress.Id,
                        Notes = createOrderDto.Notes,
                        OrderItems = orderItems // 关联订单项
                    };

                    _dbContext.Orders.Add(newOrder);
                    await _dbContext.SaveChangesAsync(); // 保存订单和订单项，以及库存更新

                    await transaction.CommitAsync(); // 提交事务

                    // 返回创建的订单详情
                    // 需要重新查询以包含关联数据
                    var createdOrder = await _dbContext.Orders
                        .Include(o => o.User)
                        .Include(o => o.ShippingAddress)
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                        .FirstOrDefaultAsync(o => o.Id == newOrder.Id);

                    if (createdOrder == null) // 理论上不应发生，但作为防御性编程
                    {
                        return StatusCode(500, new { message = "订单创建成功，但获取订单详情失败。" });
                    }

                    var orderViewModel = MapOrderToViewModel(createdOrder);
                    return CreatedAtAction(nameof(GetUserOrderById), new { id = createdOrder.Id }, orderViewModel);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // 记录日志 ex
                    Console.WriteLine($"Error creating order: {ex.Message}"); // 简单日志输出
                    return StatusCode(500, new { message = "创建订单时发生内部错误，请稍后重试。" });
                }
            }
        }

        // 用户获取自己的订单列表 (分页)
        [HttpGet]
        public async Task<ActionResult<PagedResponse<UserOrderListItemViewModel>>> GetMyOrders(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null) // 允许按订单状态筛选
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 5; // 用户订单列表通常每页数量少一些
            if (pageSize > 20) pageSize = 20;

            var query = _dbContext.Orders
                .Where(o => o.UserId == userId) // 只查询当前用户的订单
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // 用于获取第一个商品图片和名称
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            {
                query = query.Where(o => o.Status == orderStatus);
            }

            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var orderListViewModels = orders.Select(o => new UserOrderListItemViewModel
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                // StatusText 通过DTO的get属性自动生成
                FirstProductImageUrl = o.OrderItems.FirstOrDefault()?.Product?.ImageUrl,
                FirstProductName = o.OrderItems.FirstOrDefault()?.Product?.Name ?? "包含多个商品",
                TotalItemsCount = o.OrderItems.Sum(oi => oi.Quantity)
            }).ToList();

            var response = new PagedResponse<UserOrderListItemViewModel>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = orderListViewModels,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(response);
        }


        // 用户获取自己的单个订单详情
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderViewModel>> GetUserOrderById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var order = await _dbContext.Orders
                .Where(o => o.Id == id && o.UserId == userId) // 确保订单属于当前用户
                .Include(o => o.User) // 虽然是当前用户，但ViewModel可能需要用户名
                .Include(o => o.ShippingAddress)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { message = "订单不存在或您无权查看。" });
            }

            var orderViewModel = MapOrderToViewModel(order);
            return Ok(orderViewModel);
        }

        // 用户取消自己的订单 (仅在特定状态下允许, 例如 Pending 或 Paid)
        [HttpPut("{id}")]
        public async Task<IActionResult> CancelMyOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var order = await _dbContext.Orders
                                .Include(o => o.OrderItems) // 需要包含订单项以便恢复库存
                                    .ThenInclude(oi => oi.Product)
                                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new { message = "订单不存在或您无权操作。" });
            }

            // 定义哪些状态下用户可以取消订单
            var cancellableStatuses = new List<OrderStatus> { OrderStatus.Paid };
            if (!cancellableStatuses.Contains(order.Status))
            {
                return BadRequest(new { message = $"当前订单状态 ({order.Status.ToString()}) 无法取消。" });
            }

            // 使用事务确保原子性
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // 如果订单已付款但未发货，取消订单时需要恢复商品库存
                    if (order.Status == OrderStatus.Paid)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            if (item.Product != null) // 确保商品信息存在
                            {
                                item.Product.Stock += item.Quantity;
                                _dbContext.Products.Update(item.Product);
                            }
                        }
                    }

                    order.Status = OrderStatus.Cancelled;
                    _dbContext.Orders.Update(order);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "订单已成功取消。" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error cancelling order: {ex.Message}");
                    return StatusCode(500, new { message = "取消订单时发生内部错误。" });
                }
            }
        }

        [HttpPut("{id}/ConfirmReceipt")]
        public async Task<IActionResult> ConfirmReceipt(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var order = await _dbContext.Orders
                                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new { message = "订单不存在或您无权操作。" });
            }

            if (order.Status != OrderStatus.Shipped)
            {
                return BadRequest(new { message = $"订单状态为 {order.Status.ToString()}，无法确认收货。" });
            }

            order.Status = OrderStatus.Completed;
            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "确认收货成功！订单已完成。" });
            // 或者 return NoContent();
        }


        // 辅助方法：将Order实体映射到OrderViewModel (避免代码重复)
        private OrderViewModel MapOrderToViewModel(Order order)
        {
            if (order == null) return null;

            return new OrderViewModel
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "N/A",
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                // StatusText 通过DTO的get属性自动生成
                ShippingAddress = order.ShippingAddress == null ? null : new AddressViewModel
                {
                    Id = order.ShippingAddress.Id,
                    ContactName = order.ShippingAddress.ContactName,
                    PhoneNumber = order.ShippingAddress.PhoneNumber,
                    Province = order.ShippingAddress.Province,
                    City = order.ShippingAddress.City,
                    StreetAddress = order.ShippingAddress.StreetAddress,
                    PostalCode = order.ShippingAddress.PostalCode,
                    IsDefault = order.ShippingAddress.IsDefault
                    // FullAddress 通过DTO的get属性自动生成
                },
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "商品信息缺失",
                    ProductImageUrl = oi.Product?.ImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                    // Subtotal 通过DTO的get属性自动生成
                }).ToList(),
                Notes = order.Notes
            };
        }
    }
}