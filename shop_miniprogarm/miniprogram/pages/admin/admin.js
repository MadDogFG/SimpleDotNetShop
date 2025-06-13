Page({
  data: {
    products: [],
    currentPage: 1,
    totalPages: 1,
    pageSize: 9, // 每页固定9个商品（3x3网格）
    selectedId: null,
    selectedProductName: "",
    loading: false,
    showModal: false,
    modalTitle: "",
    formData: {
      id: null,
      name: "",
      price: 0,
      stock: 0,
      imageUrl: "",
      isDeleted: false
    },
    activeTab: 'products' // 默认激活商品管理tab
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
          // 添加商品删除状态
          products.forEach(product => {
            if (product.id) {
              product.isDeleted = product.isDeleted || false;
            }
          });
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
    const { id } = e.currentTarget.dataset;
    const product = this.data.products.find(p => p.id === id);
    if (product) {
      this.setData({
        showModal: true,
        modalTitle: "编辑商品",
        formData: {
          id: product.id,
          name: product.name,
          price: product.price,
          stock: product.stock,
          imageUrl: product.imageUrl,
          isDeleted: product.isDeleted || false
        }
      });
    }
  },
    // 添加新方法：恢复上架商品
    restoreProduct() {
        const token = wx.getStorageSync('token');
        const { id } = this.data.formData;
        
        this.setData({ loading: true });
        
        wx.request({
          url: `https://localhost:7024/api/Admin/RestoreProduct?id=${id}`,
          method: 'PUT',
          header: { 
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          },
          success: (res) => {
            if (res.statusCode === 200) {
              wx.showToast({ title: '商品已恢复上架' });
              this.setData({ 
                showModal: false,
                loading: false
              });
              this.loadProducts();
            } else {
              wx.showToast({ title: '恢复失败', icon: 'none' });
              this.setData({ loading: false });
            }
          },
          fail: () => {
            this.setData({ loading: false });
            wx.showToast({ title: '请求失败', icon: 'none' });
          }
        });
      },
  // 添加商品按钮
  navigateToAdd() {
    console.log("添加商品");
    this.setData({
      showModal: true,
      modalTitle: "添加商品",
      formData: {
        id: null,
        name: "",
        price: 0,
        stock: 0,
        imageUrl: ""
      }
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
  },

  // 处理表单输入
  handleInputChange(e) {
    const { field } = e.currentTarget.dataset;
    const value = e.detail.value;
    this.setData({
      [`formData.${field}`]: field === 'price' || field === 'stock' ? parseFloat(value) : value
    });
  },
  // 关闭模态框
  closeModal() {
    this.setData({
      showModal: false
    });
  },
  // 提交表单
  submitForm() {
    const { formData, modalTitle } = this.data;
    const token = wx.getStorageSync('token');
    const method = modalTitle === "添加商品" ? 'POST' : 'PUT';
    const url = modalTitle === "添加商品" 
      ? "https://localhost:7024/api/Admin/AddProduct" 
      : `https://localhost:7024/api/Admin/UpdateProduct?id=${formData.id}`;
    this.setData({ loading: true });
    
    wx.request({
      url,
      method,
      data: formData,
      header: { 
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      success: (res) => {
        if (res.statusCode === 200) {
          wx.showToast({ title: `${modalTitle}成功` });
          this.setData({ showModal: false });
          this.loadProducts();
        } else {
          wx.showToast({ title: '操作失败', icon: 'none' });
        }
        this.setData({ loading: false });
      },
      fail: () => {
        this.setData({ loading: false });
        wx.showToast({ title: '请求失败', icon: 'none' });
      }
    });
  },
  // 切换底部导航
  switchTab(e) {
    const tab = e.currentTarget.dataset.tab;
    this.setData({ activeTab: tab });
    
    if (tab === 'products') {
      this.loadProducts();
    } else {
      // 其他页面暂时用模拟数据
      wx.showToast({
        title: `${tab}页面`,
        icon: 'none',
        duration: 1000
      });
    }
  }
});