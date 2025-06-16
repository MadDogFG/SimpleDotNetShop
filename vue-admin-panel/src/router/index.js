// src/router/index.js

import { createRouter, createWebHistory } from "vue-router" // 导入创建路由和历史模式的函数
import { useAuthStore } from "@/stores/auth" // 导入认证状态管理store

// 定义路由数组
const routes = [
  {
    path: "/login", // 登录页面的路径
    name: "Login", // 路由名称
    component: () => import("../views/LoginView.vue"), // 懒加载登录组件
  },
  {
    path: "/admin", // 管理后台的基础路径
    name: "Admin", // 路由名称
    component: () => import("../layouts/AdminLayout.vue"), // 管理后台的布局组件
    meta: { requiresAuth: true, requiresAdmin: true }, // 元数据：需要认证且需要管理员角色
    children: [
      // 管理后台的子路由
      {
        path: "products", // 商品管理路径：/admin/products
        name: "ProductList",
        component: () => import("../views/Product/ProductListView.vue"), // 商品列表组件
        meta: { title: "商品管理" }, // 页面标题
      },
      {
        path: "products/add", // 添加商品路径：/admin/products/add
        name: "ProductAdd",
        component: () => import("../views/Product/ProductFormView.vue"), // 商品表单组件（用于添加）
        meta: { title: "添加商品" },
      },
      {
        path: "products/edit/:id", // 编辑商品路径：/admin/products/edit/123
        name: "ProductEdit",
        component: () => import("../views/Product/ProductFormView.vue"), // 商品表单组件（用于编辑）
        props: true, // 允许将路由参数作为组件的props传递
        meta: { title: "编辑商品" },
      },
      {
        path: "orders", // 订单管理路径：/admin/orders
        name: "OrderList",
        component: () => import("../views/Order/OrderListView.vue"), // 订单列表组件
        meta: { title: "订单管理" },
      },
      {
        path: "orders/:id", // 订单详情路径：/admin/orders/123
        name: "OrderDetail",
        component: () => import("../views/Order/OrderDetailView.vue"), // 订单详情组件
        props: true,
        meta: { title: "订单详情" },
      },
      {
        path: "users", // 用户管理路径：/admin/users
        name: "UserList",
        component: () => import("../views/User/UserListView.vue"), // 用户列表组件 (占位符)
        meta: { title: "用户管理" },
      },
      {
        path: "statistics", // 统计信息路径：/admin/statistics
        name: "Statistics",
        component: () => import("../views/StatisticsView.vue"), // 统计信息组件 (占位符)
        meta: { title: "统计信息" },
      },
      {
        path: "", // 默认子路由，重定向到商品列表
        redirect: { name: "ProductList" },
      },
    ],
  },
  {
    path: "/", // 根路径，重定向到登录页
    redirect: "/login",
  },
  {
    path: "/:pathMatch(.*)*", // 404 Not Found 路由
    name: "NotFound",
    component: () => import("../views/NotFoundView.vue"), // 404页面
  },
]

// 创建路由实例
const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes, // 传入定义的路由数组
})

// 全局前置守卫：在每次路由跳转前执行
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore() // 获取认证状态store实例

  // 检查是否需要认证
  if (to.meta.requiresAuth) {
    // 如果store中没有token，尝试从localStorage加载（防止刷新丢失状态）
    if (!authStore.token) {
      authStore.loadAuthFromLocalStorage()
    }

    // 如果仍然没有token，重定向到登录页
    if (!authStore.token) {
      next({ name: "Login", query: { redirect: to.fullPath } }) // 记录原路径，登录后可以跳回
      return
    }

    // 检查是否需要管理员角色
    if (to.meta.requiresAdmin) {
      // 如果不是管理员，重定向到登录页或显示无权限信息
      if (!authStore.isAdmin) {
        // 可以根据实际需求重定向到其他页面，这里简单地回到登录页并提示
        next({ name: "Login", query: { message: "您没有权限访问此页面，请使用管理员账户登录。" } })
        return
      }
    }
  }

  // 如果目标路由有标题，设置页面标题
  if (to.meta.title) {
    document.title = `${to.meta.title} - 管理后台`
  } else {
    document.title = "管理后台"
  }

  next() // 继续路由跳转
})

export default router // 导出路由实例
