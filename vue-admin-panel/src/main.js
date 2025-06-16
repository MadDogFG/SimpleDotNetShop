// src/main.js

import { createApp } from "vue" // 从vue库导入createApp函数，用于创建Vue应用实例
import App from "./App.vue" // 导入根组件App.vue
import router from "./router" // 导入Vue Router实例
import { createPinia } from "pinia" // 导入Pinia的createPinia函数，用于创建Pinia实例

// 导入Element Plus UI库及其样式
import ElementPlus from "element-plus"
import "element-plus/dist/index.css"

// 导入Element Plus的图标库
import * as ElementPlusIconsVue from "@element-plus/icons-vue"

// 创建Vue应用实例
const app = createApp(App)

// 创建Pinia实例
const pinia = createPinia()

// 将Pinia添加到Vue应用中，使其可以在所有组件中使用
app.use(pinia)

// 将Vue Router添加到Vue应用中，启用路由功能
app.use(router)

// 将Element Plus添加到Vue应用中，注册所有Element Plus组件
app.use(ElementPlus)

// 注册Element Plus的所有图标组件，让它们可以在模板中直接使用
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

// 将Vue应用挂载到HTML页面的#app元素上
app.mount("#app")
