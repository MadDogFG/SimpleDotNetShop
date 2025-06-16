<!-- src/views/Order/OrderListView.vue -->

<template>
  <div class="order-list-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h2>订单管理</h2>
        </div>
      </template>

      <el-alert
        title="提示"
        type="info"
        description="此页面展示所有订单列表，并支持按状态、用户ID、订单号进行筛选，以及查看订单详情和更新订单状态。"
        show-icon
        :closable="false"
        class="full-width-alert-content"
        style="margin-bottom: 20px"
      />

      <!-- 筛选表单 -->
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="订单状态">
          <el-select
            v-model="searchForm.status"
            placeholder="请选择状态"
            style="width: 150px"
            clearable
          >
            <el-option
              v-for="status in orderStatuses"
              :key="status.value"
              :label="status.name"
              :value="status.value"
            ></el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="用户ID">
          <el-input v-model="searchForm.userId" placeholder="请输入用户ID" clearable></el-input>
        </el-form-item>
        <el-form-item label="订单号">
          <el-input
            v-model="searchForm.orderNumber"
            placeholder="请输入订单号"
            clearable
          ></el-input>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 订单列表表格 -->
      <el-table :data="orders" v-loading="loading" style="width: 100%" border stripe>
        <el-table-column prop="id" label="订单ID" width="100" align="center"></el-table-column>
        <el-table-column prop="userName" label="用户" width="150"></el-table-column>
        <el-table-column prop="orderDate" label="下单时间" width="180" align="center">
          <template #default="{ row }">
            {{ new Date(row.orderDate).toLocaleString() }}
          </template>
        </el-table-column>
        <el-table-column prop="totalAmount" label="总金额" width="120" align="right">
          <template #default="{ row }"> ¥ {{ row.totalAmount.toFixed(2) }} </template>
        </el-table-column>
        <el-table-column label="订单状态" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="getOrderStatusTagType(row.status)">
              {{ getOrderStatusName(row.status) }}
              <!-- 这里使用 getOrderStatusName(row.status) -->
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="收货地址" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="row.shippingAddress"
              >{{ row.shippingAddress.province }}{{ row.shippingAddress.city
              }}{{ row.shippingAddress.streetAddress }}</span
            >
            <span v-else>无地址信息</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" align="center" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleViewDetail(row.id)">查看详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页组件 -->
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :page-sizes="[10, 20, 50]"
        :small="false"
        :disabled="loading"
        :background="true"
        layout="total, sizes, prev, pager, next, jumper"
        :total="totalCount"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
        style="margin-top: 20px; justify-content: flex-end"
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, reactive } from "vue"
import { useRouter } from "vue-router"
import { ElMessage } from "element-plus"
import axios from "@/api"

const router = useRouter()

const orders = ref([])
const loading = ref(false)
const totalCount = ref(0)
const pageSize = ref(10)
const currentPage = ref(1)
const orderStatuses = ref([]) // 订单状态列表，从后端获取

// 前端枚举映射：字符串名称到整数值
// 这必须与后端 OrderStatus 枚举的定义顺序和值保持一致
const OrderStatusStringToIntMap = {
  Paid: 0,
  Shipped: 1,
  Completed: 2,
  Cancelled: 3,
}

// 搜索表单数据
const searchForm = reactive({
  status: "", // 订单状态
  userId: "", // 用户ID
  orderNumber: "", // 订单号
})

// 获取订单状态列表 (从后端获取)
const fetchOrderStatuses = async () => {
  try {
    const response = await axios.get("/AdminOrder/GetOrderStatuses")
    orderStatuses.value = response.data
  } catch (error) {
    console.error("获取订单状态失败:", error)
    ElMessage.error("获取订单状态失败，请稍后再试。")
  }
}

// 获取订单列表数据
const fetchOrders = async () => {
  loading.value = true
  try {
    // 转换 searchForm.status 为后端期望的整数值
    const statusToSend = searchForm.status ? OrderStatusStringToIntMap[searchForm.status] : null

    const response = await axios.get("/AdminOrder/GetOrders", {
      params: {
        pageIndex: currentPage.value,
        pageSize: pageSize.value,
        status: statusToSend, // 传递转换后的整数值
        userId: searchForm.userId || null,
        orderNumber: searchForm.orderNumber || null,
      },
    })
    orders.value = response.data.items || []
    totalCount.value = response.data.totalCount || 0
  } catch (error) {
    console.error("获取订单列表失败:", error)
    ElMessage.error("获取订单列表失败，请稍后再试。")
  } finally {
    loading.value = false
  }
}

// 处理搜索
const handleSearch = () => {
  currentPage.value = 1
  fetchOrders()
}

// 重置搜索
const resetSearch = () => {
  searchForm.status = ""
  searchForm.userId = ""
  searchForm.orderNumber = ""
  currentPage.value = 1
  fetchOrders()
}

// 处理每页显示条数变化
const handleSizeChange = (val) => {
  pageSize.value = val
  currentPage.value = 1
  fetchOrders()
}

// 处理当前页码变化
const handleCurrentChange = (val) => {
  currentPage.value = val
  fetchOrders()
}

// 跳转到订单详情页面
const handleViewDetail = (id) => {
  router.push({ name: "OrderDetail", params: { id } })
}

// 根据订单状态获取对应的标签类型（颜色）
// 这里的 status 可能是数字 (从后端获取的订单列表) 或字符串 (从 el-select)
const getOrderStatusTagType = (status) => {
  let stringStatus
  if (typeof status === "number") {
    // 从数字找到对应的字符串名称
    stringStatus = Object.keys(OrderStatusStringToIntMap).find(
      (key) => OrderStatusStringToIntMap[key] === status
    )
  } else {
    stringStatus = status
  }

  switch (stringStatus) {
    case "Paid":
      return "warning" // 待发货
    case "Shipped":
      return "primary" // 已发货
    case "Completed":
      return "success" // 已完成
    case "Cancelled":
      return "info" // 已取消
    default:
      return "info"
  }
}

// 根据订单状态获取对应的显示名称
// 这里的 status 可能是数字 (从后端获取的订单列表) 或字符串 (从 el-select)
const getOrderStatusName = (status) => {
  let stringStatus
  if (typeof status === "number") {
    // 从数字找到对应的字符串名称
    stringStatus = Object.keys(OrderStatusStringToIntMap).find(
      (key) => OrderStatusStringToIntMap[key] === status
    )
  } else {
    stringStatus = status
  }

  const statusObj = orderStatuses.value.find((s) => s.value === stringStatus)
  return statusObj ? statusObj.name : stringStatus || status // 如果找不到中文名，退回英文名或原始值
}

// 组件挂载时获取订单状态和订单列表
onMounted(() => {
  fetchOrderStatuses()
  fetchOrders()
})
</script>

<style scoped>
.order-list-container {
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

.card-header h2 {
  margin: 0;
  font-size: 24px;
  color: #333;
}

.search-form {
  margin-bottom: 20px;
  padding: 15px;
  background-color: #f9f9f9;
  border-radius: 4px;
}

/* 之前解决el-alert内容居中的样式 */
.full-width-alert-content :deep(.el-alert__content) {
  flex-grow: 1;
  text-align: center;
}
</style>
