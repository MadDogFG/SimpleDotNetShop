<!-- src/views/NotFoundView.vue -->

<template>
  <div class="not-found-container">
    <h1>404</h1>
    <p>抱歉，您访问的页面不存在。</p>
    <el-button type="primary" @click="goHome">返回首页</el-button>
  </div>
</template>

<script setup>
import { useRouter } from "vue-router"

const router = useRouter()

const goHome = () => {
  router.push("/") // 返回根路径，路由守卫会将其重定向到登录页或管理员页
}
</script>

<style scoped>
.not-found-container {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  min-height: 80vh; /* 确保在AdminLayout中也能居中 */
  text-align: center;
  color: #666;
}

.not-found-container h1 {
  font-size: 100px;
  margin-bottom: 20px;
  color: #409eff; /* Element Plus primary color */
}

.not-found-container p {
  font-size: 20px;
  margin-bottom: 30px;
}
</style>
