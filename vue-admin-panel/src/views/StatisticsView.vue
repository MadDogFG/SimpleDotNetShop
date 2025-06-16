<!-- src/views/StatisticsView.vue -->
<template>
  <div class="statistics-container">
    <h2>统计信息</h2>
    <p>这里将显示销量、用户数、商品数等统计信息。</p>
    <el-alert
      title="注意"
      type="warning"
      description="当前后端代码中没有直接提供用于获取统计数据的管理员API。您需要根据需求在后端（Weixin_Project/Controllers/）添加一个 AdminStatisticsController，实现获取总销量、用户总数、商品总数等统计功能。"
      show-icon
      :closable="false"
      class="full-width-alert-content"
    />
    <!-- 实际的统计图表和数据将在这里实现 -->
  </div>
</template>

<script setup></script>

<style scoped>
.statistics-container {
  padding: 20px;
}

.full-width-alert-content :deep(.el-alert__content) {
  flex-grow: 1; /* 关键：让内容区域扩展，占据所有可用空间 */
  text-align: center; /* 在内容区域扩展后，再让其内部文本居中 */
}
</style>
