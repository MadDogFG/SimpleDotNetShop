import axios from "axios" // 导入axios库
import { useAuthStore } from "@/stores/auth" // 导入认证状态store

// 创建一个axios实例
const instance = axios.create({
  baseURL: "https://localhost:7024/api", // 后端API基地址
  timeout: 10000, // 请求超时时间
  headers: {
    "Content-Type": "application/json", // 默认请求头
  },
})

// 请求拦截器：在发送请求之前执行
instance.interceptors.request.use(
  (config) => {
    const authStore = useAuthStore() // 获取认证store实例

    // 如果存在token，则在请求头中添加Authorization字段
    if (authStore.token) {
      config.headers.Authorization = `Bearer ${authStore.token}` // JWT认证格式
    }
    return config // 返回配置，继续发送请求
  },
  (error) => {
    // 处理请求错误
    return Promise.reject(error)
  }
)

// 响应拦截器：在接收到响应之后执行
instance.interceptors.response.use(
  (response) => {
    // 对响应数据做点什么
    return response
  },
  (error) => {
    const authStore = useAuthStore() // 获取认证store实例

    // 处理HTTP状态码
    if (error.response) {
      switch (error.response.status) {
        case 401: // 未授权
          // 如果是401错误，且不是登录请求，则可能是token过期或无效，执行登出操作
          if (error.config.url !== "/Auth/Login") {
            authStore.logout() // 清除认证信息
            // 提示用户或重定向到登录页
            console.error("认证失败，请重新登录。")
            // router.push('/login') // 如果需要自动跳转
          }
          break
        case 403: // 禁止访问 (无权限)
          console.error("您没有权限执行此操作。")
          break
        case 404: // 资源未找到
          console.error("请求的资源不存在。")
          break
        case 500: // 服务器内部错误
          console.error("服务器内部错误，请稍后重试。")
          break
        default:
          console.error(`HTTP错误：${error.response.status} - ${error.response.statusText}`)
      }
    } else if (error.request) {
      // 请求已发出但没有收到响应
      console.error("网络错误或服务器无响应。")
    } else {
      // 在设置请求时触发了错误
      console.error("请求设置错误:", error.message)
    }
    return Promise.reject(error) // 拒绝Promise，将错误传递给调用者
  }
)

export default instance // 导出配置好的axios实例
