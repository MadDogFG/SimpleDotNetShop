<!-- src/views/Product/ProductListView.vue -->

<template>
  <div class="product-list-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h2>商品管理</h2>
          <el-button type="primary" :icon="Plus" @click="goToAddProduct">新增商品</el-button>
        </div>
      </template>

      <!-- 提示信息，之前居中问题已解决 -->
      <el-alert
        title="提示"
        type="info"
        description="此页面用于展示商品列表，并提供新增、编辑、软删除和恢复商品的功能。已软删除的商品会显示为灰色，但仍可恢复。"
        show-icon
        :closable="false"
        class="full-width-alert-content"
        style="margin-bottom: 20px"
      />

      <!-- 商品列表表格 -->
      <el-table
        :data="products"
        v-loading="loading"
        style="width: 100%"
        border
        stripe
        :row-class-name="tableRowClassName"
      >
        <el-table-column prop="id" label="ID" width="80" align="center"></el-table-column>
        <el-table-column prop="name" label="商品名称" width="180"></el-table-column>
        <el-table-column prop="price" label="价格" width="100" align="right">
          <template #default="{ row }"> ¥ {{ row.price.toFixed(2) }} </template>
        </el-table-column>
        <el-table-column prop="stock" label="库存" width="100" align="center"></el-table-column>
        <el-table-column label="图片" width="100" align="center">
          <template #default="{ row }">
            <el-image
              v-if="row.imageUrl && row.imageUrl !== '无'"
              style="width: 60px; height: 60px; border-radius: 4px"
              :src="row.imageUrl"
              fit="cover"
              :preview-src-list="[row.imageUrl]"
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
        <el-table-column
          prop="description"
          label="描述"
          min-width="200"
          show-overflow-tooltip
        ></el-table-column>
        <el-table-column prop="createTime" label="创建时间" width="180" align="center">
          <template #default="{ row }">
            {{ new Date(row.createTime).toLocaleString() }}
          </template>
        </el-table-column>
        <el-table-column prop="isDeleted" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isDeleted ? 'danger' : 'success'">
              {{ row.isDeleted ? "已删除" : "正常" }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" align="center" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row.id)">编辑</el-button>
            <el-button
              size="small"
              :type="row.isDeleted ? 'success' : 'danger'"
              @click="
                row.isDeleted ? handleRestore(row.id, row.name) : handleDelete(row.id, row.name)
              "
            >
              {{ row.isDeleted ? "恢复" : "删除" }}
            </el-button>
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
import { ref, onMounted } from "vue" // 导入Vue 3的响应式API和生命周期钩子
import { useRouter } from "vue-router" // 导入路由hook
import { ElMessage, ElMessageBox } from "element-plus" // 导入Element Plus的消息提示和弹窗组件
import axios from "@/api" // 导入我们配置的axios实例
import { Plus, Picture } from "@element-plus/icons-vue" // 导入Element Plus图标

const router = useRouter() // 获取路由实例

// 响应式数据
const products = ref([]) // 商品列表数据
const loading = ref(false) // 加载状态
const totalCount = ref(0) // 商品总数
const pageSize = ref(10) // 每页显示条数
const currentPage = ref(1) // 当前页码

// 获取商品列表数据
const fetchProducts = async () => {
  loading.value = true // 设置加载状态为true
  try {
    // 向后端API发送请求，获取商品列表，并传入分页参数
    const response = await axios.get("/AdminProduct/GetAllProducts", {
      params: {
        pageIndex: currentPage.value,
        pageSize: pageSize.value,
      },
    })
    // 更新商品列表和总数
    products.value = response.data.items || []
    totalCount.value = response.data.totalCount || 0
  } catch (error) {
    console.error("获取商品列表失败:", error)
    ElMessage.error("获取商品列表失败，请稍后再试。")
  } finally {
    loading.value = false // 解除加载状态
  }
}

// 处理每页显示条数变化
const handleSizeChange = (val) => {
  pageSize.value = val // 更新每页显示条数
  currentPage.value = 1 // 切换每页条数后回到第一页
  fetchProducts() // 重新获取数据
}

// 处理当前页码变化
const handleCurrentChange = (val) => {
  currentPage.value = val // 更新当前页码
  fetchProducts() // 重新获取数据
}

// 跳转到新增商品页面
const goToAddProduct = () => {
  router.push({ name: "ProductAdd" })
}

// 跳转到编辑商品页面
const handleEdit = (id) => {
  router.push({ name: "ProductEdit", params: { id } })
}

// 处理软删除商品
const handleDelete = async (id, name) => {
  // 弹出确认框
  ElMessageBox.confirm(`确定要软删除商品 "${name}" 吗？删除后可恢复。`, "确认删除", {
    confirmButtonText: "确定",
    cancelButtonText: "取消",
    type: "warning",
  })
    .then(async () => {
      // 用户点击确定
      loading.value = true
      try {
        // 调用后端API进行软删除
        await axios.delete(`/AdminProduct/DeleteProduct/${id}`)
        ElMessage.success(`商品 "${name}" 已成功软删除！`)
        fetchProducts() // 重新获取列表数据以更新状态
      } catch (error) {
        console.error("软删除商品失败:", error)
        const errorMessage = error.response?.data?.message || "软删除商品失败，请稍后再试。"
        ElMessage.error(errorMessage)
      } finally {
        loading.value = false
      }
    })
    .catch(() => {
      // 用户点击取消
      ElMessage.info("已取消删除操作。")
    })
}

// 处理恢复商品
const handleRestore = async (id, name) => {
  // 弹出确认框
  ElMessageBox.confirm(`确定要恢复商品 "${name}" 吗？`, "确认恢复", {
    confirmButtonText: "确定",
    cancelButtonText: "取消",
    type: "info",
  })
    .then(async () => {
      // 用户点击确定
      loading.value = true
      try {
        // 调用后端API进行恢复
        await axios.put(`/AdminProduct/RestoreProduct/${id}`)
        ElMessage.success(`商品 "${name}" 已成功恢复！`)
        fetchProducts() // 重新获取列表数据以更新状态
      } catch (error) {
        console.error("恢复商品失败:", error)
        const errorMessage = error.response?.data?.message || "恢复商品失败，请稍后再试。"
        ElMessage.error(errorMessage)
      } finally {
        loading.value = false
      }
    })
    .catch(() => {
      // 用户点击取消
      ElMessage.info("已取消恢复操作。")
    })
}

// 根据商品是否删除添加行样式
const tableRowClassName = ({ row }) => {
  if (row.isDeleted) {
    return "deleted-row" // 如果商品已删除，添加 deleted-row 类
  }
  return ""
}

// 组件挂载时获取商品列表
onMounted(() => {
  fetchProducts()
})
</script>

<style scoped>
.product-list-container {
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

/* 之前解决el-alert内容居中的样式 */
.full-width-alert-content :deep(.el-alert__content) {
  flex-grow: 1;
  text-align: center;
}

/* Element Plus 图片占位符样式 */
.image-slot {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;
  background: var(--el-fill-color-light);
  color: var(--el-text-color-secondary);
  font-size: 30px;
}

/* 已删除行的样式 */
.el-table .deleted-row {
  background-color: #f5f7fa; /* 浅灰色背景 */
  color: #909399; /* 文字颜色变灰 */
  font-style: italic; /* 斜体 */
}

/* 确保已删除行的操作按钮颜色也变灰，或者保持原样 */
.el-table .deleted-row .el-button {
  color: #909399;
  border-color: #dcdfe6;
}
</style>
