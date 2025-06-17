<!-- src/views/StatisticsView.vue -->

<template>
  <div class="statistics-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h2>统计信息</h2>
        </div>
      </template>

      <el-alert
        title="提示"
        type="info"
        description="此页面展示了关键业务统计数据，包括用户数、商品数、订单数、总销售额以及最近7天的销售趋势。"
        show-icon
        :closable="false"
        class="full-width-alert-content"
        style="margin-bottom: 20px"
      />

      <!-- 核心统计卡片 -->
      <el-row :gutter="20" class="core-stats-row" v-loading="coreStatsLoading">
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-item">
              <el-icon :size="30" color="#409EFF"><UserFilled /></el-icon>
              <div class="stat-content">
                <div class="stat-value">{{ coreStatistics.totalUsers }}</div>
                <div class="stat-label">总用户数</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-item">
              <el-icon :size="30" color="#67C23A"><GoodsFilled /></el-icon>
              <div class="stat-content">
                <div class="stat-value">{{ coreStatistics.totalProducts }}</div>
                <div class="stat-label">总商品数</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-item">
              <el-icon :size="30" color="#E6A23C"><Tickets /></el-icon>
              <div class="stat-content">
                <div class="stat-value">{{ coreStatistics.totalOrders }}</div>
                <div class="stat-label">总订单数</div>
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-item">
              <el-icon :size="30" color="#F56C6C"><Money /></el-icon>
              <div class="stat-content">
                <div class="stat-value">¥ {{ coreStatistics.totalSalesAmount.toFixed(2) }}</div>
                <div class="stat-label">总销售额</div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <el-row
        :gutter="20"
        class="core-stats-row"
        style="margin-top: 20px"
        v-loading="coreStatsLoading"
      >
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-item">
              <el-icon :size="30" color="#909399"><Loading /></el-icon>
              <div class="stat-content">
                <div class="stat-value">{{ coreStatistics.pendingShipmentOrders }}</div>
                <div class="stat-label">待发货订单</div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <!-- 销售额趋势图 -->
      <el-card style="margin-top: 20px">
        <template #header>
          <div class="card-header">
            <h3>最近7天销售额趋势</h3>
          </div>
        </template>
        <div ref="salesChart" style="width: 100%; height: 400px" v-loading="chartLoading"></div>
      </el-card>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, reactive, onBeforeUnmount } from "vue"
import { ElMessage } from "element-plus"
import axios from "@/api"
import * as echarts from "echarts" // 导入 ECharts
import { UserFilled, GoodsFilled, Tickets, Money, Loading } from "@element-plus/icons-vue" // 导入Element Plus图标

// 核心统计数据
const coreStatistics = reactive({
  totalUsers: 0,
  totalProducts: 0,
  totalOrders: 0,
  totalSalesAmount: 0,
  pendingShipmentOrders: 0,
})
const coreStatsLoading = ref(false)

// 图表数据和加载状态
const salesChart = ref(null) // ECharts 容器的引用
let myChart = null // ECharts 实例
const chartLoading = ref(false)

// 获取核心统计数据
const fetchCoreStatistics = async () => {
  coreStatsLoading.value = true
  try {
    const response = await axios.get("/AdminStatistics/GetCoreStatistics")
    Object.assign(coreStatistics, response.data) // 将数据赋值给 reactive 对象
  } catch (error) {
    console.error("获取核心统计数据失败:", error)
    ElMessage.error("获取核心统计数据失败，请稍后再试。")
  } finally {
    coreStatsLoading.value = false
  }
}

// 获取最近7天销售额数据并渲染图表
const fetchSalesDataAndRenderChart = async () => {
  chartLoading.value = true
  try {
    const response = await axios.get("/AdminStatistics/GetLast7DaysSales")
    const data = response.data || []

    const dates = data.map((item) => item.date)
    const amounts = data.map((item) => item.amount)

    // 初始化并渲染 ECharts 图表
    if (myChart) {
      myChart.dispose() // 如果已存在实例，先销毁
    }
    myChart = echarts.init(salesChart.value) // 在DOM元素上初始化ECharts实例

    const option = {
      title: {
        text: "销售额趋势",
        left: "center",
      },
      tooltip: {
        trigger: "axis",
        formatter: "日期: {b}<br/>销售额: ¥{c}", // 格式化提示框内容
      },
      xAxis: {
        type: "category",
        data: dates,
        axisLabel: {
          rotate: 45, // 旋转标签，防止重叠
          interval: 0, // 显示所有标签
        },
      },
      yAxis: {
        type: "value",
        name: "销售额 (¥)",
      },
      series: [
        {
          name: "销售额",
          type: "line",
          data: amounts,
          smooth: true, // 平滑曲线
          itemStyle: {
            color: "#409EFF", // Element Plus primary color
          },
          lineStyle: {
            width: 3, // 线条宽度
          },
          areaStyle: {
            // 区域填充样式
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: "rgba(64, 158, 255, 0.3)" }, // 渐变色
              { offset: 1, color: "rgba(64, 158, 255, 0)" },
            ]),
          },
        },
      ],
      grid: {
        left: "3%",
        right: "4%",
        bottom: "10%", // 留出空间给旋转的x轴标签
        containLabel: true,
      },
    }
    myChart.setOption(option)

    // 监听窗口大小变化，自动调整图表大小
    window.addEventListener("resize", myChart.resize)
  } catch (error) {
    console.error("获取销售数据失败:", error)
    ElMessage.error("获取销售数据失败，请稍后再试。")
  } finally {
    chartLoading.value = false
  }
}

// 组件挂载时执行
onMounted(() => {
  fetchCoreStatistics()
  fetchSalesDataAndRenderChart()
})

// 组件卸载前销毁 ECharts 实例，防止内存泄漏
onBeforeUnmount(() => {
  if (myChart) {
    myChart.dispose()
    myChart = null
    window.removeEventListener("resize", myChart.resize)
  }
})
</script>

<style scoped>
.statistics-container {
  padding: 20px;
}

.box-card {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header h2,
.card-header h3 {
  margin: 0;
  font-size: 24px;
  color: #333;
}

/* 之前解决el-alert内容居中的样式 */
.full-width-alert-content :deep(.el-alert__content) {
  flex-grow: 1;
  text-align: center;
}

/* 核心统计卡片样式 */
.core-stats-row {
  margin-bottom: 20px;
}

.stat-card {
  text-align: center;
  height: 120px; /* 固定高度，保持一致 */
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-item {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
}

.stat-item .el-icon {
  margin-right: 15px;
  font-size: 40px; /* 调整图标大小 */
}

.stat-content {
  display: flex;
  flex-direction: column;
  align-items: flex-start; /* 让文本左对齐 */
}

.stat-value {
  font-size: clamp(18px, 2.5vw, 28px);
  font-size: 28px;
  font-weight: bold;
  color: #303133;
}

.stat-label {
  font-size: 14px;
  color: #909399;
  margin-top: 5px;
}
</style>
