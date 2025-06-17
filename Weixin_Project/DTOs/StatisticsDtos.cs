// Weixin_Project\DTOs\StatisticsDtos.cs

namespace Weixin_Project.DTOs
{
    /// <summary>
    /// 核心统计数据视图模型
    /// </summary>
    public class StatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public int PendingShipmentOrders { get; set; } // 待发货订单数
    }

    /// <summary>
    /// 每日销售数据模型 (用于图表)
    /// </summary>
    public class DailySalesData
    {
        public string Date { get; set; } = string.Empty; // 日期字符串，如 "2023-10-26"
        public decimal Amount { get; set; }
    }
}