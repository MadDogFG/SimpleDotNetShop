Page({
  data: {
    products: [],
    currentPage: 1,
    totalPages: 1,
    pageSize: 9, // 每页固定9个商品（3x3网格）
    selectedId: null,
    selectedProductName: "",
    loading: false
  },

  onLoad() {
    this.loadProducts();
  },

  // 加载商品数据
  loadProducts() {
    this.setData({ loading: true });
    
    const token = wx.getStorageSync('token');
    const { currentPage, pageSize } = this.data;
    
    wx.request({
      url: `https://localhost:7024/api/Admin/GetAllProducts`,
      method: 'GET',
      data: {
        pageIndex: currentPage,
        pageSize: pageSize
      },
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200) {
          const data = res.data;
          // 确保返回的商品数量符合网格要求（不足时添加空项）
          const products = data.items || [];
          while (products.length < pageSize) {
            products.push({ empty: true }); // 添加空项占位
          }
          
          this.setData({
            products: products,
            totalPages: Math.ceil(data.totalCount / pageSize),
            loading: false
          });
        }
      },
      fail: () => {
        this.setData({ loading: false });
        wx.showToast({ title: '加载失败', icon: 'none' });
      }
    });
  },

  // 分页处理
  handlePageChange(e) {
    const { type } = e.currentTarget.dataset;
    let newPage = this.data.currentPage;
    
    if (type === 'prev' && this.data.currentPage > 1) {
      newPage--;
    } else if (type === 'next' && this.data.currentPage < this.data.totalPages) {
      newPage++;
    }
    
    if (newPage !== this.data.currentPage) {
      this.setData({ currentPage: newPage }, () => {
        this.loadProducts();
      });
    }
  },

  // 导航到编辑页面
  navigateToEdit(e) {
    console.log("编辑商品");
    if (this.data.selectedId) return;
    
    const { id } = e.currentTarget.dataset;
    wx.navigateTo({
      url: `/pages/admin/edit?id=${id}`
    });
  },

  // 添加商品按钮
  navigateToAdd() {
    console.log("添加商品");
    wx.navigateTo({
      url: '/pages/admin/add'
    });
  },

  // 删除图标点击处理
  handleDelete(e) {
    const id = e.currentTarget.dataset.id;
    const name = e.currentTarget.dataset.name;
    
    this.setData({
      selectedId: id,
      selectedProductName: name
    });
  },

  // 确认删除请求
  confirmDelete(e) {
    const id = this.data.selectedId;
    const token = wx.getStorageSync('token');
    
    wx.request({
      url: `https://localhost:7024/api/Admin/DeleteProduct?id=${id}`,
      method: 'DELETE',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200) {
          wx.showToast({ title: '删除成功' });
          this.setData({ selectedId: null });
          this.loadProducts();
        } else {
          wx.showToast({ title: '删除失败', icon: 'none' });
        }
      }
    });
  },

  // 取消选择
  cancelSelect() {
    this.setData({ selectedId: null });
  }
});