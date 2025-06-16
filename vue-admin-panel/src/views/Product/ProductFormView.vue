<!-- src/views/Product/ProductFormView.vue -->

<template>
  <div class="product-form-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h2>{{ isEditMode ? "编辑商品" : "新增商品" }}</h2>
        </div>
      </template>

      <el-alert
        title="提示"
        type="info"
        :description="isEditMode ? '请填写或修改商品信息。' : '请填写新商品的信息。'"
        show-icon
        :closable="false"
        class="full-width-alert-content"
        style="margin-bottom: 20px"
      />

      <el-form
        :model="productForm"
        :rules="formRules"
        ref="productFormRef"
        label-width="120px"
        v-loading="loading"
      >
        <el-form-item label="商品名称" prop="name">
          <el-input v-model="productForm.name" placeholder="请输入商品名称"></el-input>
        </el-form-item>
        <el-form-item label="价格" prop="price">
          <el-input-number
            v-model="productForm.price"
            :min="0.01"
            :precision="2"
            :step="0.01"
            controls-position="right"
            style="width: 100%"
          ></el-input-number>
        </el-form-item>
        <el-form-item label="库存" prop="stock">
          <el-input-number
            v-model="productForm.stock"
            :min="0"
            :step="1"
            controls-position="right"
            style="width: 100%"
          ></el-input-number>
        </el-form-item>
        <el-form-item label="图片URL" prop="imageUrl">
          <el-input v-model="productForm.imageUrl" placeholder="请输入商品图片URL"></el-input>
          <div v-if="productForm.imageUrl" style="margin-top: 10px">
            <el-image
              style="width: 100px; height: 100px; border-radius: 4px"
              :src="productForm.imageUrl"
              fit="cover"
            >
              <template #error>
                <div class="image-slot">
                  <el-icon><Picture /></el-icon>
                  <span>加载失败</span>
                </div>
              </template>
            </el-image>
          </div>
        </el-form-item>
        <el-form-item label="商品描述" prop="description">
          <el-input
            v-model="productForm.description"
            type="textarea"
            :rows="4"
            placeholder="请输入商品描述"
          ></el-input>
        </el-form-item>

        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitLoading">
            {{ isEditMode ? "保存修改" : "立即创建" }}
          </el-button>
          <el-button @click="router.back()">取消</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from "vue" // 导入Vue 3的响应式API和生命周期钩子
import { useRoute, useRouter } from "vue-router" // 导入路由相关hook
import { ElMessage } from "element-plus" // 导入Element Plus的消息提示组件
import axios from "@/api" // 导入我们配置的axios实例
import { Picture } from "@element-plus/icons-vue" // 导入Element Plus图标

const route = useRoute() // 获取当前路由信息
const router = useRouter() // 获取路由实例

// 判断当前是编辑模式还是新增模式
const isEditMode = computed(() => route.params.id !== undefined)
const productId = computed(() => route.params.id) // 获取商品ID（编辑模式下）

const productFormRef = ref(null) // 表单的引用，用于触发表单验证
const loading = ref(false) // 页面加载状态（用于获取商品详情）
const submitLoading = ref(false) // 提交按钮加载状态

// 商品表单数据
const productForm = reactive({
  name: "",
  price: 0.01, // 默认值，与后端验证规则一致
  stock: 0, // 默认值
  imageUrl: "",
  description: "",
})

// 表单验证规则
const formRules = reactive({
  name: [
    { required: true, message: "请输入商品名称", trigger: "blur" },
    { min: 2, max: 100, message: "长度在 2 到 100 个字符", trigger: "blur" },
  ],
  price: [
    { required: true, message: "请输入商品价格", trigger: "blur" },
    { type: "number", min: 0.01, message: "价格必须大于 0", trigger: "blur" },
  ],
  stock: [
    { required: true, message: "请输入库存", trigger: "blur" },
    { type: "number", min: 0, message: "库存不能为负数", trigger: "blur" },
  ],
  imageUrl: [{ max: 255, message: "图片URL长度不能超过 255 个字符", trigger: "blur" }],
  description: [{ max: 500, message: "描述长度不能超过 500 个字符", trigger: "blur" }],
})

// 获取商品详情（编辑模式下）
const fetchProductDetail = async () => {
  if (!isEditMode.value) return // 如果不是编辑模式，则不执行

  loading.value = true // 设置加载状态
  try {
    // 调用后端API获取商品详情
    const response = await axios.get(`/AdminProduct/GetProductById/${productId.value}`)
    // 将获取到的数据填充到表单中
    const data = response.data
    productForm.name = data.name
    productForm.price = data.price
    productForm.stock = data.stock
    productForm.imageUrl = data.imageUrl === "无" ? "" : data.imageUrl // '无' 转换为空字符串
    productForm.description = data.description === "无" ? "" : data.description // '无' 转换为空字符串
  } catch (error) {
    console.error("获取商品详情失败:", error)
    const errorMessage = error.response?.data?.message || "获取商品详情失败，请稍后再试。"
    ElMessage.error(errorMessage)
    router.back() // 获取失败则返回列表页
  } finally {
    loading.value = false // 解除加载状态
  }
}

// 提交表单
const handleSubmit = async () => {
  // 触发表单验证
  await productFormRef.value.validate(async (valid) => {
    if (valid) {
      // 如果表单验证通过
      submitLoading.value = true // 设置提交按钮加载状态

      // 准备提交的数据，处理 '无' 的情况
      const dataToSubmit = {
        name: productForm.name,
        price: productForm.price,
        stock: productForm.stock,
        imageUrl: productForm.imageUrl || "无", // 如果为空，则传 '无'
        description: productForm.description || "无", // 如果为空，则传 '无'
      }

      try {
        if (isEditMode.value) {
          // 编辑模式：调用PUT接口更新商品
          await axios.put(`/AdminProduct/UpdateProduct/${productId.value}`, dataToSubmit)
          ElMessage.success("商品信息更新成功！")
        } else {
          // 新增模式：调用POST接口创建商品
          await axios.post("/AdminProduct/CreateProduct", dataToSubmit)
          ElMessage.success("商品创建成功！")
        }
        router.push({ name: "ProductList" }) // 提交成功后跳转回商品列表页
      } catch (error) {
        console.error("提交商品信息失败:", error)
        const errorMessage = error.response?.data?.message || "提交商品信息失败，请稍后再试。"
        ElMessage.error(errorMessage)
      } finally {
        submitLoading.value = false // 解除提交按钮加载状态
      }
    } else {
      ElMessage.error("请完整填写表单信息！")
      return false
    }
  })
}

// 组件挂载时执行
onMounted(() => {
  if (isEditMode.value) {
    fetchProductDetail() // 如果是编辑模式，获取商品详情
  }
})
</script>

<style scoped>
.product-form-container {
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
  flex-direction: column; /* 让文字和图标垂直排列 */
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;
  background: var(--el-fill-color-light);
  color: var(--el-text-color-secondary);
  font-size: 20px; /* 调整图标大小 */
}
.image-slot span {
  font-size: 14px; /* 调整文字大小 */
  margin-top: 5px;
}
</style>
