// Weixin_Project\Controllers\AdminStatisticsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Weixin_Project.DTOs; // 假设您会定义 StatisticsViewModel
using Weixin_Project.Utils; // For ApplicationDbContext
using System;
using System.Linq;
using System.Threading.Tasks;
using Weixin_Project.Entities;

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // 只有管理员才能访问这些接口
    public class AdminStatisticsController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public AdminStatisticsController(UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        /// <summary>
        /// 获取核心统计数据（用户数、商品数、订单数、总销售额、待发货订单数）
        /// </summary>
        /// <returns>核心统计数据</returns>
        [HttpGet]
        public async Task<ActionResult<StatisticsViewModel>> GetCoreStatistics()
        {
            // 获取总用户数 (排除管理员，如果需要)
            // 这里为了简化，直接统计所有IdentityUser
            var totalUsers = await _userManager.Users.CountAsync();

            // 获取总商品数 (只统计未删除的商品)
            var totalProducts = await _dbContext.Products.CountAsync(p => !p.IsDeleted);

            // 获取总订单数
            var totalOrders = await _dbContext.Orders.CountAsync();

            // 获取总销售额
            var totalSalesAmount = await _dbContext.Orders.SumAsync(o => o.TotalAmount);

            // 获取待发货订单数 (OrderStatus.Paid 对应待发货)
            var pendingShipmentOrders = await _dbContext.Orders.CountAsync(o => o.Status == OrderStatus.Paid);

            var viewModel = new StatisticsViewModel
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalSalesAmount = totalSalesAmount,
                PendingShipmentOrders = pendingShipmentOrders
            };

            return Ok(viewModel);
        }

        /// <summary>
        /// 获取最近7天的销售额趋势数据
        /// </summary>
        /// <returns>包含日期和销售额的列表</returns>
        [HttpGet]
        public async Task<ActionResult<List<DailySalesData>>> GetLast7DaysSales()
        {
            var endDate = DateTime.UtcNow.Date; // 今天
            var startDate = endDate.AddDays(-6); // 7天前（包含今天共7天）

            var salesData = new List<DailySalesData>();

            // 循环获取最近7天的数据
            for (int i = 0; i < 7; i++)
            {
                var date = startDate.AddDays(i);
                var nextDate = date.AddDays(1); // 用于范围查询的下一天

                // 统计某一天的销售额
                var dailyAmount = await _dbContext.Orders
                    .Where(o => o.OrderDate >= date && o.OrderDate < nextDate)
                    .SumAsync(o => (decimal?)o.TotalAmount ?? 0); // 使用 (decimal?)o.TotalAmount ?? 0 处理可能为空的情况

                salesData.Add(new DailySalesData
                {
                    Date = date.ToString("yyyy-MM-dd"), // 格式化日期
                    Amount = dailyAmount
                });
            }

            return Ok(salesData);
        }
    }
}