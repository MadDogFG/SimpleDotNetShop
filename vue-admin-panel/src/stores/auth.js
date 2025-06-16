// src/stores/auth.js

import { defineStore } from "pinia" // 导入Pinia的defineStore函数
import axios from "@/api" // 导入我们配置的axios实例，用于API请求

// 定义一个名为 'auth' 的store
export const useAuthStore = defineStore("auth", {
  // state: 定义store的状态数据
  state: () => ({
    token: localStorage.getItem("token") || null, // JWT token，从localStorage获取或初始化为null
    username: localStorage.getItem("username") || null, // 用户名
    roles: JSON.parse(localStorage.getItem("roles")) || [], // 用户角色数组，从localStorage获取或初始化为空数组
  }),

  // getters: 定义基于state的计算属性
  getters: {
    isAuthenticated: (state) => !!state.token, // 判断用户是否已认证 (是否有token)
    isAdmin: (state) => state.roles.includes("Admin"), // 判断用户是否是管理员 (角色中是否包含'Admin')
  },

  // actions: 定义执行异步操作和修改state的方法
  actions: {
    /**
     * 用户登录方法
     * @param {string} username - 用户名
     * @param {string} password - 密码
     * @returns {Promise<boolean>} - 登录成功返回true，否则返回false
     */
    async login(username, password) {
      try {
        // 向后端认证API发送登录请求
        const response = await axios.post("/Auth/Login", { username, password })
        const { token, username: loggedInUsername, roles } = response.data

        // 更新store状态
        this.token = token
        this.username = loggedInUsername
        this.roles = roles

        // 将认证信息保存到localStorage，以便在页面刷新后保持登录状态
        localStorage.setItem("token", token)
        localStorage.setItem("username", loggedInUsername)
        localStorage.setItem("roles", JSON.stringify(roles))

        return true // 登录成功
      } catch (error) {
        // 登录失败，清空本地存储和store状态
        this.logout()
        console.error("登录失败:", error)
        // 抛出错误以便在组件中捕获并显示给用户
        throw error
      }
    },

    /**
     * 用户登出方法
     */
    logout() {
      // 清空store状态
      this.token = null
      this.username = null
      this.roles = []

      // 清空localStorage中的认证信息
      localStorage.removeItem("token")
      localStorage.removeItem("username")
      localStorage.removeItem("roles")
    },

    /**
     * 从localStorage加载认证信息 (用于页面刷新后恢复状态)
     */
    loadAuthFromLocalStorage() {
      const token = localStorage.getItem("token")
      const username = localStorage.getItem("username")
      const roles = JSON.parse(localStorage.getItem("roles"))

      if (token && username && roles) {
        this.token = token
        this.username = username
        this.roles = roles
      } else {
        this.logout() // 如果信息不完整，则清除
      }
    },
  },
})
