using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Weixin_Project.DTOs; // 引入DTOs命名空间
using Weixin_Project.Entities;
using Weixin_Project.Utils; // 引入Entities命名空间

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize] // 需要用户登录才能操作购物车
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager; // 如果需要根据用户信息做特定操作

        public CartController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // 获取或创建当前用户的购物车 (辅助方法)
        private async Task<Cart> GetOrCreateUserCartAsync(string userId)
        {
            var cart = await _dbContext.Carts
                                    .Include(c => c.Items) // 预加载购物车项
                                        .ThenInclude(ci => ci.Product) // 在购物车项中预加载商品信息
                                    .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _dbContext.Carts.Add(cart);
                // 注意：这里不立即 SaveChanges，因为后续操作可能会修改cart并一起保存
            }
            return cart;
        }

        // 将Order实体映射到OrderViewModel (辅助方法)
        private CartViewModel MapCartToViewModel(Cart cart)
        {
            if (cart == null) return new CartViewModel(); // 返回一个空的购物车视图

            return new CartViewModel
            {
                CartId = cart.Id,
                LastModifiedDate = cart.LastModifiedDate,
                Items = cart.Items?.Select(ci => new CartItemViewModel
                {
                    CartItemId = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "商品信息缺失",
                    Price = ci.Product?.Price ?? 0, // 如果商品不存在，价格为0
                    Quantity = ci.Quantity,
                    ImageUrl = ci.Product?.ImageUrl,
                    Stock = ci.Product?.Stock ?? 0 // 如果商品不存在，库存为0
                }).ToList() ?? new List<CartItemViewModel>()
                // TotalAmount 和 TotalItemsCount 通过DTO的get属性自动计算
            };
        }


        // 获取当前用户的购物车内容
        [HttpGet]
        public async Task<ActionResult<CartViewModel>> GetMyCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var cart = await GetOrCreateUserCartAsync(userId);
            // 即使是新创建的空购物车，也应该返回，而不是null

            // 检查购物车中是否有已下架或库存不足的商品 (可选，但用户体验好)
            bool cartNeedsUpdate = false;
            if (cart.Items != null && cart.Items.Any())
            {
                var itemsToRemove = new List<CartItem>();
                foreach (var item in cart.Items)
                {
                    if (item.Product == null || item.Product.IsDeleted) // 商品不存在或已软删除
                    {
                        itemsToRemove.Add(item);
                        cartNeedsUpdate = true;
                        continue;
                    }
                    if (item.Product.Stock < item.Quantity) // 库存不足
                    {
                        // 自动调整数量到最大库存，或直接移除，或提示用户
                        // 这里简单处理：如果库存为0，则移除；否则调整为最大库存
                        if (item.Product.Stock == 0)
                        {
                            itemsToRemove.Add(item);
                        }
                        else
                        {
                            item.Quantity = item.Product.Stock;
                        }
                        cartNeedsUpdate = true;
                    }
                }
                if (itemsToRemove.Any())
                {
                    _dbContext.CartItems.RemoveRange(itemsToRemove);
                    // 从 cart.Items 集合中也移除，以便 MapCartToViewModel 正确映射
                    foreach (var removed in itemsToRemove)
                    {
                        cart.Items.Remove(removed);
                    }
                }
            }

            if (cartNeedsUpdate)
            {
                cart.LastModifiedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(); // 保存因商品下架或库存不足导致的购物车更新
            }


            return Ok(MapCartToViewModel(cart));
        }

        // 添加商品到购物车
        [HttpPost]
        public async Task<ActionResult<CartViewModel>> AddItemToCart([FromBody] AddToCartRequestDto addToCartDto)
        {
            // FluentValidation 会自动验证 addToCartDto

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var product = await _dbContext.Products.FindAsync(addToCartDto.ProductId);
            if (product == null || product.IsDeleted)
            {
                return NotFound(new { message = $"商品ID {addToCartDto.ProductId} 不存在或已下架。" });
            }

            if (product.Stock < addToCartDto.Quantity)
            {
                return BadRequest(new { message = $"商品 '{product.Name}' 库存不足，当前库存 {product.Stock}。" });
            }

            var cart = await GetOrCreateUserCartAsync(userId);
            var existingCartItem = cart.Items?.FirstOrDefault(ci => ci.ProductId == addToCartDto.ProductId);

            if (existingCartItem != null)
            {
                // 商品已存在购物车中，更新数量
                if (product.Stock < existingCartItem.Quantity + addToCartDto.Quantity)
                {
                    return BadRequest(new { message = $"商品 '{product.Name}' 库存不足以增加到 {existingCartItem.Quantity + addToCartDto.Quantity} 件。" });
                }
                existingCartItem.Quantity += addToCartDto.Quantity;
                _dbContext.CartItems.Update(existingCartItem);
            }
            else
            {
                // 商品不存在购物车中，新增一项
                var newCartItem = new CartItem
                {
                    CartId = cart.Id, // 如果是新cart，ID此时可能为0，EF Core会在SaveChanges时处理
                    ProductId = addToCartDto.ProductId,
                    Quantity = addToCartDto.Quantity,
                    DateAdded = DateTime.UtcNow
                };
                // 如果cart是新创建的，需要先保存cart以获取cart.Id
                if (cart.Id == 0) // 仅当cart是新创建且未保存时
                {
                    // _dbContext.Carts.Add(cart); // 已在GetOrCreateUserCartAsync中Add
                    // await _dbContext.SaveChangesAsync(); // 保存以获取cart.Id, 但这可能导致多次SaveChanges
                    // EF Core 关系修复：如果CartItem的Cart导航属性已设置，EF Core能处理
                    newCartItem.Cart = cart; // 确保导航属性设置
                }

                _dbContext.CartItems.Add(newCartItem);
                if (cart.Items == null) cart.Items = new List<CartItem>(); // 防御null
                cart.Items.Add(newCartItem); // 更新内存中的cart对象，以便MapCartToViewModel正确
            }

            cart.LastModifiedDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(); // 一次性保存所有更改（新cart, 新cartItem或更新的cartItem, cart的LastModifiedDate）

            // 为了返回完整的CartViewModel，需要重新加载product信息
            // GetOrCreateUserCartAsync 内部已经做了Include，所以再次调用它
            var updatedCart = await GetOrCreateUserCartAsync(userId);
            return Ok(MapCartToViewModel(updatedCart));
        }

        // 更新购物车中商品的数量
        [HttpPut]
        public async Task<ActionResult<CartViewModel>> UpdateItemQuantity([FromBody] UpdateCartItemQuantityRequestDto updateDto)
        {
            // FluentValidation 会自动验证 updateDto

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var cart = await GetOrCreateUserCartAsync(userId);
            var cartItemToUpdate = cart.Items?.FirstOrDefault(ci => ci.Id == updateDto.CartItemId);

            if (cartItemToUpdate == null)
            {
                return NotFound(new { message = $"购物车中未找到ID为 {updateDto.CartItemId} 的商品项。" });
            }

            // 获取关联商品以检查库存
            var product = cartItemToUpdate.Product; // 依赖于GetOrCreateUserCartAsync中对Product的Include
            if (product == null || product.IsDeleted) // 防御性检查，理论上已在GetMyCart中处理
            {
                _dbContext.CartItems.Remove(cartItemToUpdate); // 如果商品已失效，直接从购物车移除
                cart.Items.Remove(cartItemToUpdate);
                cart.LastModifiedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return Ok(MapCartToViewModel(cart)); // 返回更新后的购物车
            }


            if (updateDto.Quantity == 0)
            {
                // 如果数量更新为0，则移除该商品项
                _dbContext.CartItems.Remove(cartItemToUpdate);
                cart.Items.Remove(cartItemToUpdate); // 从内存中移除
            }
            else
            {
                if (product.Stock < updateDto.Quantity)
                {
                    return BadRequest(new { message = $"商品 '{product.Name}' 库存不足，无法将数量更新为 {updateDto.Quantity}。" });
                }
                cartItemToUpdate.Quantity = updateDto.Quantity;
                _dbContext.CartItems.Update(cartItemToUpdate);
            }

            cart.LastModifiedDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var updatedCart = await GetOrCreateUserCartAsync(userId); // 重新获取以确保数据最新
            return Ok(MapCartToViewModel(updatedCart));
        }

        // 从购物车中移除单个商品项
        [HttpDelete("{cartItemId}")] // 将cartItemId作为路由参数
        public async Task<ActionResult<CartViewModel>> RemoveItemFromCart(int cartItemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var cart = await GetOrCreateUserCartAsync(userId);
            var cartItemToRemove = cart.Items?.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItemToRemove == null)
            {
                // 即使找不到也要返回OK和当前购物车，避免前端出错
                return Ok(MapCartToViewModel(cart));
                // return NotFound(new { message = $"购物车中未找到ID为 {cartItemId} 的商品项。" });
            }

            _dbContext.CartItems.Remove(cartItemToRemove);
            cart.Items.Remove(cartItemToRemove); // 从内存中移除
            cart.LastModifiedDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var updatedCart = await GetOrCreateUserCartAsync(userId);
            return Ok(MapCartToViewModel(updatedCart));
        }

        // 清空当前用户的购物车 (可选)
        [HttpDelete("Clear")]
        public async Task<IActionResult> ClearMyCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var cart = await _dbContext.Carts
                                    .Include(c => c.Items)
                                    .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null && cart.Items != null && cart.Items.Any())
            {
                _dbContext.CartItems.RemoveRange(cart.Items); // 移除所有购物车项
                cart.Items.Clear(); // 从内存中清空
                cart.LastModifiedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
            // 即使购物车原本就是空的，也返回成功
            return Ok(new { message = "购物车已清空。" });
            // 或者返回空的CartViewModel:
            // return Ok(MapCartToViewModel(cart ?? new Cart { UserId = userId }));
        }

        /// <summary>
        /// 批量删除购物车中的商品项
        /// </summary>
        /// <param name="removeDto">包含要删除的购物车项ID列表的DTO</param>
        /// <returns>更新后的购物车视图模型</returns>
        [HttpPost] // 使用 POST 来接收请求体中的ID列表
        public async Task<ActionResult<CartViewModel>> RemoveMultipleItemsFromCart([FromBody] RemoveCartItemsRequestDto removeDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            // FluentValidation 会自动验证 removeDto，如果验证失败，会返回 BadRequest

            var cart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
            {
                // 购物车已为空或不存在，无需执行删除操作
                return Ok(MapCartToViewModel(cart)); // 返回一个空的或当前状态的购物车
            }

            // 筛选出属于当前用户购物车且在请求列表中存在的商品项
            var itemsToRemove = cart.Items
                .Where(ci => removeDto.CartItemIds.Contains(ci.Id) && ci.CartId == cart.Id)
                .ToList();

            if (!itemsToRemove.Any())
            {
                // 没有找到匹配的商品项，可能已经删除或ID不正确
                return Ok(MapCartToViewModel(cart)); // 返回当前购物车状态
            }

            _dbContext.CartItems.RemoveRange(itemsToRemove);
            cart.LastModifiedDate = DateTime.UtcNow; // 更新购物车最后修改时间

            await _dbContext.SaveChangesAsync();

            // 重新获取更新后的购物车数据并返回
            var updatedCart = await GetOrCreateUserCartAsync(userId);
            return Ok(MapCartToViewModel(updatedCart));
        }
    }
}