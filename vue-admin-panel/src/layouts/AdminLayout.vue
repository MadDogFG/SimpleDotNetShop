<!-- src/layouts/AdminLayout.vue -->

<template>
  <el-container class="admin-layout-container">
    <!-- 侧边栏 -->
    <el-aside width="200px" class="admin-aside">
      <div class="logo">管理后台</div>
      <el-menu
        :default-active="activeMenu"
        class="el-menu-vertical-demo"
        router
        background-color="#545c64"
        text-color="#fff"
        active-text-color="#ffd04b"
      >
        <el-menu-item index="/admin/products">
          <el-icon><Goods /></el-icon>
          <span>商品管理</span>
        </el-menu-item>
        <el-menu-item index="/admin/orders">
          <el-icon><Tickets /></el-icon>
          <span>订单管理</span>
        </el-menu-item>
        <el-menu-item index="/admin/users">
          <el-icon><User /></el-icon>
          <span>用户管理</span>
        </el-menu-item>
        <el-menu-item index="/admin/statistics">
          <el-icon><TrendCharts /></el-icon>
          <span>统计信息</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <!-- 顶部导航栏 -->
      <el-header class="admin-header">
        <div class="header-left">
          <!-- 可以放面包屑导航或其他内容 -->
          <span>欢迎，{{ authStore.username }}</span>
        </div>
        <div class="header-right">
          <el-button type="danger" @click="handleLogout" plain>
            <el-icon><SwitchButton /></el-icon>
            退出登录
          </el-button>
        </div>
      </el-header>

      <!-- 主内容区域 -->
      <el-main class="admin-main">
        <router-view />
        <!-- 路由视图，显示子路由组件 -->
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { computed } from "vue" // 导入computed用于计算属性
import { useRoute, useRouter } from "vue-router" // 导入路由相关hook
import { useAuthStore } from "@/stores/auth" // 导入认证store
import { ElMessage } from "element-plus" // 导入消息提示组件
// 导入Element Plus图标
import { Goods, Tickets, User, TrendCharts, SwitchButton } from "@element-plus/icons-vue"

const route = useRoute() // 获取当前路由信息
const router = useRouter() // 获取路由实例
const authStore = useAuthStore() // 获取认证store实例

// 计算当前激活的菜单项，使其与当前路由匹配
const activeMenu = computed(() => {
  // 确保当前路由的path以'/admin/'开头，并截取到第一级子路径
  const path = route.path
  if (path.startsWith("/admin/")) {
    const parts = path.split("/")
    // 例如：/admin/products/add 会匹配到 /admin/products
    if (parts.length > 3) {
      return `/admin/${parts[2]}`
    }
  }
  return path
})

// 处理退出登录
const handleLogout = () => {
  authStore.logout() // 调用store的logout方法
  ElMessage.success("已成功退出登录！")
  router.push("/login") // 跳转到登录页
}
</script>

<style scoped>
.admin-layout-container {
  min-height: 100vh; /* 确保布局占满整个视口高度 */
}

.admin-aside {
  background-color: #545c64; /* 侧边栏背景色 */
  color: #fff;
  overflow-x: hidden; /* 防止内容溢出时出现水平滚动条 */
}

.logo {
  height: 60px;
  line-height: 60px;
  text-align: center;
  font-size: 20px;
  font-weight: bold;
  border-bottom: 1px solid #4a5057; /* logo下方边框 */
}

.el-menu-vertical-demo {
  border-right: none; /* 移除Element Plus菜单自带的右边框 */
}

.admin-header {
  background-color: #fff;
  color: #333;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #eee; /* 顶部导航栏下方边框 */
  padding: 0 20px;
}

.admin-main {
  background-color: #f0f2f5; /* 主内容区域背景色 */
  padding: 20px;
}
</style>
