<!-- src/views/Order/OrderDetailView.vue -->

<template>
  <div class="order-detail-container">
    <el-card class="box-card" v-loading="loading">
      <template #header>
        <div class="card-header">
          <h2>订单详情 - ID: {{ orderId }}</h2>
          <el-button @click="router.back()">返回订单列表</el-button>
        </div>
      </template>

      <el-alert
        title="提示"
        type="info"
        description="此页面展示单个订单的详细信息，包括收货地址、商品列表、总金额等，并提供更新订单状态的功能。"
        show-icon
        :closable="false"
        class="full-width-alert-content"
        style="margin-bottom: 20px"
      />

      <div v-if="order" class="order-details-content">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-descriptions class="margin-top" title="订单基本信息" :column="1" border>
              <el-descriptions-item label="订单ID">{{ order.id }}</el-descriptions-item>
              <el-descriptions-item label="用户"
                >{{ order.userName }} (ID: {{ order.userId }})</el-descriptions-item
              >
              <el-descriptions-item label="下单时间">{{
                new Date(order.orderDate).toLocaleString()
              }}</el-descriptions-item>
              <el-descriptions-item label="总金额"
                >¥ {{ order.totalAmount.toFixed(2) }}</el-descriptions-item
              >
              <el-descriptions-item label="订单状态">
                <el-tag :type="getOrderStatusTagType(order.status)">
                  {{ getOrderStatusName(order.status) }}
                  <!-- 这里使用 getOrderStatusName(order.status) -->
                </el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="备注">{{ order.notes || "无" }}</el-descriptions-item>
            </el-descriptions>
          </el-col>
          <el-col :span="12">
            <el-descriptions class="margin-top" title="收货地址信息" :column="1" border>
              <el-descriptions-item label="联系人">{{
                order.shippingAddress?.contactName || "N/A"
              }}</el-descriptions-item>
              <el-descriptions-item label="手机号">{{
                order.shippingAddress?.phoneNumber || "N/A"
              }}</el-descriptions-item>
              <el-descriptions-item label="省份">{{
                order.shippingAddress?.province || "N/A"
              }}</el-descriptions-item>
              <el-descriptions-item label="城市">{{
                order.shippingAddress?.city || "N/A"
              }}</el-descriptions-item>
              <el-descriptions-item label="详细地址">{{
                order.shippingAddress?.streetAddress || "N/A"
              }}</el-descriptions-item>
              <el-descriptions-item label="邮政编码">{{
                order.shippingAddress?.postalCode || "N/A"
              }}</el-descriptions-item>
            </el-descriptions>
          </el-col>
        </el-row>

        <h3 style="margin-top: 30px; margin-bottom: 15px">订单商品</h3>
        <el-table :data="order.orderItems" border stripe style="width: 100%">
          <el-table-column
            prop="productId"
            label="商品ID"
            width="100"
            align="center"
          ></el-table-column>
          <el-table-column label="商品图片" width="100" align="center">
            <template #default="{ row }">
              <el-image
                v-if="row.productImageUrl && row.productImageUrl !== '无'"
                style="width: 60px; height: 60px; border-radius: 4px"
                :src="row.productImageUrl"
                fit="cover"
                :preview-src-list="[row.productImageUrl]"
                preview-teleported
              >
                <template #error>
                  <div class="image-slot">
                    <el-icon><Picture /></el-icon>
                  </div>
                </template>
              </el-image>
              <span v-else>无图</span>
            </template>
          </el-table-column>
          <el-table-column prop="productName" label="商品名称" min-width="180"></el-table-column>
          <el-table-column prop="unitPrice" label="单价" width="100" align="right">
            <template #default="{ row }"> ¥ {{ row.unitPrice.toFixed(2) }} </template>
          </el-table-column>
          <el-table-column prop="quantity" label="数量" width="80" align="center"></el-table-column>
          <el-table-column prop="subtotal" label="小计" width="120" align="right">
            <template #default="{ row }"> ¥ {{ row.subtotal.toFixed(2) }} </template>
          </el-table-column>
        </el-table>

        <h3 style="margin-top: 30px; margin-bottom: 15px">更新订单状态</h3>
        <el-form :inline="true" :model="statusUpdateForm">
          <el-form-item label="新状态">
            <el-select
              v-model="statusUpdateForm.newStatus"
              placeholder="请选择新状态"
              style="width: 150px"
            >
              <el-option
                v-for="status in orderStatuses"
                :key="status.value"
                :label="status.name"
                :value="status.value"
              ></el-option>
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleUpdateStatus" :loading="updateStatusLoading"
              >更新状态</el-button
            >
          </el-form-item>
        </el-form>
      </div>
      <el-empty v-else description="订单不存在或加载失败"></el-empty>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, computed, reactive } from "vue"
import { useRoute, useRouter } from "vue-router"
import { ElMessage, ElMessageBox } from "element-plus"
import axios from "@/api"
import { Picture } from "@element-plus/icons-vue"

const route = useRoute()
const router = useRouter()

const orderId = computed(() => route.params.id)
const order = ref(null)
const loading = ref(false)
const orderStatuses = ref([])
const updateStatusLoading = ref(false)

// 前端枚举映射：字符串名称到整数值
// 这必须与后端 OrderStatus 枚举的定义顺序和值保持一致
const OrderStatusStringToIntMap = {
  Paid: 0,
  Shipped: 1,
  Completed: 2,
  Cancelled: 3,
}

// 状态更新表单数据
const statusUpdateForm = reactive({
  newStatus: "",
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

// 获取订单详情
const fetchOrderDetail = async () => {
  loading.value = true
  try {
    const response = await axios.get(`/AdminOrder/GetOrderById/${orderId.value}`)
    order.value = response.data
    // 默认选中当前订单状态，需要将后端返回的数字状态转换为字符串，以匹配 el-select 的 value
    const currentStatusString = Object.keys(OrderStatusStringToIntMap).find(
      (key) => OrderStatusStringToIntMap[key] === order.value.status
    )
    statusUpdateForm.newStatus = currentStatusString || "" // 确保设置为字符串
  } catch (error) {
    console.error("获取订单详情失败:", error)
    const errorMessage = error.response?.data?.message || "获取订单详情失败，请稍后再试。"
    ElMessage.error(errorMessage)
    order.value = null
  } finally {
    loading.value = false
  }
}

// 更新订单状态
const handleUpdateStatus = async () => {
  if (!statusUpdateForm.newStatus) {
    ElMessage.warning("请选择新的订单状态！")
    return
  }

  // 获取当前订单的字符串状态，用于比较
  const currentOrderStatusString = Object.keys(OrderStatusStringToIntMap).find(
    (key) => OrderStatusStringToIntMap[key] === order.value.status
  )

  // 避免重复提交相同状态
  if (currentOrderStatusString && statusUpdateForm.newStatus === currentOrderStatusString) {
    ElMessage.info("当前状态已是您选择的新状态，无需更新。")
    return
  }

  // 将前端选择的字符串状态转换为后端期望的整数值
  const newStatusInt = OrderStatusStringToIntMap[statusUpdateForm.newStatus]
  if (newStatusInt === undefined) {
    ElMessage.error("无效的订单状态选择。")
    return
  }

  ElMessageBox.confirm(
    `确定将订单状态更新为 "${getOrderStatusName(statusUpdateForm.newStatus)}" 吗？`,
    "确认更新",
    {
      confirmButtonText: "确定",
      cancelButtonText: "取消",
      type: "warning",
    }
  )
    .then(async () => {
      updateStatusLoading.value = true
      try {
        // 调用后端API更新订单状态，传递整数值
        await axios.put(`/AdminOrder/UpdateOrderStatus/${orderId.value}`, {
          newStatus: newStatusInt, // 传递转换后的整数值
        })
        ElMessage.success("订单状态更新成功！")
        fetchOrderDetail() // 重新获取订单详情以更新显示
      } catch (error) {
        console.error("更新订单状态失败:", error)
        // 解析后端返回的错误信息
        const responseData = error.response?.data
        let errorMessage = "更新订单状态失败，请稍后再试。"
        if (responseData && responseData.errors) {
          // 尝试从 errors 字段中提取详细信息
          const errorMessages = Object.values(responseData.errors).flat()
          if (errorMessages.length > 0) {
            errorMessage = errorMessages.join("; ")
          } else if (responseData.message) {
            errorMessage = responseData.message
          }
        } else if (error.response?.data?.message) {
          errorMessage = error.response.data.message
        }

        ElMessage.error(errorMessage)
      } finally {
        updateStatusLoading.value = false
      }
    })
    .catch(() => {
      ElMessage.info("已取消状态更新操作。")
    })
}

// 根据订单状态获取对应的标签类型（颜色）
// 这里的 status 可能是数字 (从后端获取的订单列表) 或字符串 (从 el-select)
const getOrderStatusTagType = (status) => {
  let stringStatus
  if (typeof status === "number") {
    stringStatus = Object.keys(OrderStatusStringToIntMap).find(
      (key) => OrderStatusStringToIntMap[key] === status
    )
  } else {
    stringStatus = status
  }

  switch (stringStatus) {
    case "Paid":
      return "warning"
    case "Shipped":
      return "primary"
    case "Completed":
      return "success"
    case "Cancelled":
      return "info"
    default:
      return "info"
  }
}

// 根据订单状态获取对应的显示名称
// 这里的 status 可能是数字 (从后端获取的订单列表) 或字符串 (从 el-select)
const getOrderStatusName = (status) => {
  let stringStatus
  if (typeof status === "number") {
    stringStatus = Object.keys(OrderStatusStringToIntMap).find(
      (key) => OrderStatusStringToIntMap[key] === status
    )
  } else {
    stringStatus = status
  }

  const statusObj = orderStatuses.value.find((s) => s.value === stringStatus)
  return statusObj ? statusObj.name : stringStatus || status
}

// 组件挂载时执行
onMounted(() => {
  fetchOrderStatuses()
  fetchOrderDetail()
})
</script>

<style scoped>
.order-detail-container {
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

.order-details-content {
  margin-top: 20px;
}

/* Element Plus Descriptions 标题样式调整 */
.margin-top {
  margin-bottom: 20px;
}

/* Element Plus 图片占位符样式 */
.image-slot {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;
  background: var(--el-fill-color-light);
  color: var(--el-text-color-secondary);
  font-size: 20px;
}
.image-slot span {
  font-size: 14px;
  margin-top: 5px;
}

/* 之前解决el-alert内容居中的样式 */
.full-width-alert-content :deep(.el-alert__content) {
  flex-grow: 1;
  text-align: center;
}
</style>
