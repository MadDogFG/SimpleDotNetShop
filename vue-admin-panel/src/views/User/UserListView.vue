<!-- src/views/User/UserListView.vue -->
<template>
  <div class="user-list-container">
    <h2>用户管理</h2>
    <p>这里将显示用户列表。</p>
    <el-alert
      title="注意"
      type="warning"
      description="当前后端代码中没有直接提供用于获取用户列表的管理员API。您需要根据需求在后端（Weixin_Project/Controllers/）添加一个 AdminUserController，实现获取用户列表、禁用/启用用户等功能。"
      show-icon
      :closable="false"
      class="full-width-alert-content"
    />
    <!-- 实际的用户列表表格将在这里实现 -->
  </div>
</template>

<script setup>
// 用户管理逻辑将在这里实现
</script>

<style scoped>
.user-list-container {
  padding: 20px;
}

.full-width-alert-content :deep(.el-alert__content) {
  flex-grow: 1; /* 关键：让内容区域扩展，占据所有可用空间 */
  text-align: center; /* 在内容区域扩展后，再让其内部文本居中 */
}
</style>
