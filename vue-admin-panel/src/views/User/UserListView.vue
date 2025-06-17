<!-- src/views/User/UserListView.vue -->

<template>
  <div class="user-list-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h2>用户管理</h2>
        </div>
      </template>

      <el-alert
        title="提示"
        type="info"
        description="此页面用于管理注册用户，包括查看列表、搜索、禁用/启用用户和重置密码。管理员账户无法被禁用或重置密码。"
        show-icon
        :closable="false"
        class="full-width-alert-content"
        style="margin-bottom: 20px"
      />

      <!-- 筛选表单 -->
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="搜索用户">
          <el-input v-model="searchForm.searchTerm" placeholder="用户名或邮箱" clearable></el-input>
        </el-form-item>
        <el-form-item label="包含管理员">
          <el-switch v-model="searchForm.includeAdmin" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">查询</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 用户列表表格 -->
      <el-table
        :data="users"
        v-loading="loading"
        style="width: 100%"
        border
        stripe
        :row-class-name="tableRowClassName"
      >
        <el-table-column prop="id" label="ID" width="280" show-overflow-tooltip></el-table-column>
        <el-table-column prop="userName" label="用户名" width="180"></el-table-column>
        <el-table-column
          prop="email"
          label="邮箱"
          width="200"
          show-overflow-tooltip
        ></el-table-column>
        <el-table-column prop="phoneNumber" label="手机号" width="150"></el-table-column>
        <el-table-column prop="emailConfirmed" label="邮箱验证" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.emailConfirmed ? 'success' : 'info'">
              {{ row.emailConfirmed ? "已验证" : "未验证" }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="isLockedOut" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isLockedOut ? 'danger' : 'success'">
              {{ row.isLockedOut ? "已禁用" : "正常" }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="roles" label="角色" width="120" align="center">
          <template #default="{ row }">
            <el-tag v-for="role in row.roles" :key="role" size="small" style="margin-right: 5px">{{
              role
            }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" align="center" fixed="right">
          <template #default="{ row }">
            <el-button
              size="small"
              :type="row.isLockedOut ? 'success' : 'danger'"
              :disabled="row.roles.includes('Admin')"
              @click="
                row.isLockedOut
                  ? handleUnlockUser(row.id, row.userName)
                  : handleLockUser(row.id, row.userName)
              "
            >
              {{ row.isLockedOut ? "启用" : "禁用" }}
            </el-button>
            <el-button
              size="small"
              type="primary"
              :disabled="row.roles.includes('Admin')"
              @click="handleResetPassword(row.id, row.userName)"
            >
              重置密码
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

    <!-- 重置密码对话框 -->
    <el-dialog
      v-model="resetPasswordDialogVisible"
      title="重置用户密码"
      width="500px"
      :before-close="handleCloseResetPasswordDialog"
    >
      <el-form
        :model="resetPasswordForm"
        :rules="resetPasswordRules"
        ref="resetPasswordFormRef"
        label-width="100px"
        v-loading="resetPasswordLoading"
      >
        <el-form-item label="用户" prop="userName">
          <el-input v-model="resetPasswordForm.userName" disabled></el-input>
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input
            type="password"
            v-model="resetPasswordForm.newPassword"
            show-password
            placeholder="请输入新密码"
          ></el-input>
        </el-form-item>
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input
            type="password"
            v-model="resetPasswordForm.confirmPassword"
            show-password
            placeholder="请再次输入新密码"
          ></el-input>
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="handleCloseResetPasswordDialog">取消</el-button>
          <el-button type="primary" @click="submitResetPassword" :loading="resetPasswordLoading"
            >确定</el-button
          >
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted, reactive } from "vue"
import { ElMessage, ElMessageBox } from "element-plus"
import axios from "@/api"

// 响应式数据
const users = ref([]) // 用户列表数据
const loading = ref(false) // 加载状态
const totalCount = ref(0) // 用户总数
const pageSize = ref(10) // 每页显示条数
const currentPage = ref(1) // 当前页码

// 搜索表单数据
const searchForm = reactive({
  searchTerm: "", // 搜索关键词
  includeAdmin: false, // 是否包含管理员用户
})

// 重置密码对话框相关
const resetPasswordDialogVisible = ref(false)
const resetPasswordFormRef = ref(null) // 重置密码表单的引用
const resetPasswordLoading = ref(false) // 重置密码提交加载状态
const resetPasswordForm = reactive({
  userId: "",
  userName: "",
  newPassword: "",
  confirmPassword: "",
})

// 重置密码表单验证规则
const resetPasswordRules = reactive({
  newPassword: [
    { required: true, message: "请输入新密码", trigger: "blur" },
    { min: 6, message: "密码长度不能少于 6 个字符", trigger: "blur" },
    { max: 100, message: "密码长度不能超过 100 个字符", trigger: "blur" },
  ],
  confirmPassword: [
    { required: true, message: "请再次输入密码", trigger: "blur" },
    {
      validator: (rule, value, callback) => {
        if (value !== resetPasswordForm.newPassword) {
          callback(new Error("两次输入的密码不一致！"))
        } else {
          callback()
        }
      },
      trigger: "blur",
    },
  ],
})

// 获取用户列表数据
const fetchUsers = async () => {
  loading.value = true
  try {
    const response = await axios.get("/AdminUser/GetUsers", {
      params: {
        pageIndex: currentPage.value,
        pageSize: pageSize.value,
        searchTerm: searchForm.searchTerm || null,
        includeAdmin: searchForm.includeAdmin,
      },
    })
    users.value = response.data.items || []
    totalCount.value = response.data.totalCount || 0
  } catch (error) {
    console.error("获取用户列表失败:", error)
    ElMessage.error("获取用户列表失败，请稍后再试。")
  } finally {
    loading.value = false
  }
}

// 处理搜索
const handleSearch = () => {
  currentPage.value = 1
  fetchUsers()
}

// 重置搜索
const resetSearch = () => {
  searchForm.searchTerm = ""
  searchForm.includeAdmin = false
  currentPage.value = 1
  fetchUsers()
}

// 处理每页显示条数变化
const handleSizeChange = (val) => {
  pageSize.value = val
  currentPage.value = 1
  fetchUsers()
}

// 处理当前页码变化
const handleCurrentChange = (val) => {
  currentPage.value = val
  fetchUsers()
}

// 处理禁用用户
const handleLockUser = async (id, userName) => {
  ElMessageBox.confirm(`确定要禁用用户 "${userName}" 吗？禁用后该用户将无法登录。`, "确认禁用", {
    confirmButtonText: "确定",
    cancelButtonText: "取消",
    type: "warning",
  })
    .then(async () => {
      loading.value = true
      try {
        await axios.put(`/AdminUser/LockUser/${id}`)
        ElMessage.success(`用户 "${userName}" 已成功禁用！`)
        fetchUsers()
      } catch (error) {
        console.error("禁用用户失败:", error)
        const errorMessage = error.response?.data?.message || "禁用用户失败，请稍后再试。"
        ElMessage.error(errorMessage)
      } finally {
        loading.value = false
      }
    })
    .catch(() => {
      ElMessage.info("已取消禁用操作。")
    })
}

// 处理启用用户
const handleUnlockUser = async (id, userName) => {
  ElMessageBox.confirm(`确定要启用用户 "${userName}" 吗？启用后该用户将恢复登录。`, "确认启用", {
    confirmButtonText: "确定",
    cancelButtonText: "取消",
    type: "info",
  })
    .then(async () => {
      loading.value = true
      try {
        await axios.put(`/AdminUser/UnlockUser/${id}`)
        ElMessage.success(`用户 "${userName}" 已成功启用！`)
        fetchUsers()
      } catch (error) {
        console.error("启用用户失败:", error)
        const errorMessage = error.response?.data?.message || "启用用户失败，请稍后再试。"
        ElMessage.error(errorMessage)
      } finally {
        loading.value = false
      }
    })
    .catch(() => {
      ElMessage.info("已取消启用操作。")
    })
}

// 打开重置密码对话框
const handleResetPassword = (id, userName) => {
  resetPasswordForm.userId = id
  resetPasswordForm.userName = userName
  resetPasswordForm.newPassword = ""
  resetPasswordForm.confirmPassword = ""
  resetPasswordDialogVisible.value = true
  // 确保对话框打开后，表单验证状态重置
  if (resetPasswordFormRef.value) {
    resetPasswordFormRef.value.clearValidate()
  }
}

// 关闭重置密码对话框
const handleCloseResetPasswordDialog = () => {
  resetPasswordDialogVisible.value = false
  resetPasswordFormRef.value.resetFields() // 重置表单字段和验证状态
}

// 提交重置密码
const submitResetPassword = async () => {
  await resetPasswordFormRef.value.validate(async (valid) => {
    if (valid) {
      resetPasswordLoading.value = true
      try {
        await axios.post(`/AdminUser/ResetPassword/${resetPasswordForm.userId}`, {
          newPassword: resetPasswordForm.newPassword,
          confirmPassword: resetPasswordForm.confirmPassword,
        })
        ElMessage.success(`用户 "${resetPasswordForm.userName}" 的密码已成功重置！`)
        handleCloseResetPasswordDialog() // 关闭对话框
      } catch (error) {
        console.error("重置密码失败:", error)
        const responseData = error.response?.data
        let errorMessage = "重置密码失败，请稍后再试。"
        if (responseData && responseData.errors) {
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
        resetPasswordLoading.value = false
      }
    } else {
      ElMessage.error("请完整填写新密码信息！")
      return false
    }
  })
}

// 根据用户锁定状态添加行样式
const tableRowClassName = ({ row }) => {
  if (row.isLockedOut) {
    return "locked-out-row" // 如果用户被锁定，添加 locked-out-row 类
  }
  return ""
}

// 组件挂载时获取用户列表
onMounted(() => {
  fetchUsers()
})
</script>

<style scoped>
.user-list-container {
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

.search-form {
  margin-bottom: 20px;
  padding: 15px;
  background-color: #f9f9f9;
  border-radius: 4px;
}

/* 之前解决el-alert内容居中的样式 */
.full-width-alert-content :deep(.el-alert__content) {
  flex-grow: 1;
  text-align: center;
}

/* 被锁定行的样式 */
.el-table .locked-out-row {
  background-color: #fef0f0; /* 浅红色背景 */
  color: #f56c6c; /* 文字颜色变红 */
  font-style: italic; /* 斜体 */
}

/* 确保被锁定行的操作按钮颜色也变灰，或者保持原样 */
.el-table .locked-out-row .el-button {
  color: #f56c6c;
  border-color: #fbc4c4;
}
</style>
