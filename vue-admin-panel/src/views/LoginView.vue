<!-- src/views/LoginView.vue -->

<template>
  <div class="login-container">
    <el-card class="login-card">
      <template #header>
        <div class="card-header">
          <span>管理员登录</span>
        </div>
      </template>
      <el-form :model="loginForm" :rules="loginRules" ref="loginFormRef" label-position="top">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="loginForm.username" placeholder="请输入用户名" clearable></el-input>
        </el-form-item>
        <el-form-item label="密码" prop="password">
          <el-input
            type="password"
            v-model="loginForm.password"
            placeholder="请输入密码"
            show-password
          ></el-input>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="handleLogin" style="width: 100%">
            登录
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from "vue" // 导入Vue 3的响应式API和生命周期钩子
import { useRouter, useRoute } from "vue-router" // 导入路由相关hook
import { useAuthStore } from "@/stores/auth" // 导入认证store
import { ElMessage } from "element-plus" // 导入Element Plus的消息提示组件

const router = useRouter() // 获取路由实例
const route = useRoute() // 获取当前路由信息
const authStore = useAuthStore() // 获取认证store实例

const loginFormRef = ref(null) // 表单的引用，用于触发表单验证
const loading = ref(false) // 登录按钮的加载状态

// 登录表单数据
const loginForm = reactive({
  username: "",
  password: "",
})

// 登录表单验证规则
const loginRules = reactive({
  username: [{ required: true, message: "请输入用户名", trigger: "blur" }],
  password: [{ required: true, message: "请输入密码", trigger: "blur" }],
})

// 处理登录逻辑
const handleLogin = async () => {
  // 触发表单验证
  await loginFormRef.value.validate(async (valid) => {
    if (valid) {
      // 如果表单验证通过
      loading.value = true // 设置加载状态为true
      try {
        // 调用authStore的login方法进行登录
        const success = await authStore.login(loginForm.username, loginForm.password)
        if (success) {
          // 登录成功后，检查是否是管理员
          if (authStore.isAdmin) {
            ElMessage.success("登录成功，欢迎管理员！")
            // 检查是否有重定向参数，有则跳转到原路径，否则跳转到管理员首页
            const redirectPath = route.query.redirect || "/admin"
            router.push(redirectPath)
          } else {
            ElMessage.warning("您不是管理员，无法访问后台。")
            authStore.logout() // 非管理员登录后直接登出
            router.push("/login") // 回到登录页
          }
        } else {
          // 理论上login方法会抛出错误，这里作为备用
          ElMessage.error("登录失败，请检查用户名或密码。")
        }
      } catch (error) {
        // 捕获login方法抛出的错误，显示给用户
        const errorMessage = error.response?.data?.message || "登录失败，请稍后再试。"
        ElMessage.error(errorMessage)
      } finally {
        loading.value = false // 无论成功失败，都解除加载状态
      }
    } else {
      ElMessage.error("请完整填写登录信息！")
      return false
    }
  })
}

// 组件挂载时检查是否有路由守卫传递的错误消息
onMounted(() => {
  if (route.query.message) {
    ElMessage.warning(route.query.message)
  }
})
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh; /* 最小高度为视口高度 */
  background-color: #f0f2f5; /* 背景色 */
}

.login-card {
  width: 400px; /* 卡片宽度 */
  max-width: 90%; /* 最大宽度，适应小屏幕 */
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1); /* 阴影效果 */
}

.card-header {
  text-align: center;
  font-size: 24px;
  font-weight: bold;
  color: #333;
}

/* 可以在这里添加更多样式来美化登录页面 */
</style>
