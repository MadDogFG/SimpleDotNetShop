// admin.js
const API_BASE_URL = 'https://localhost:7024/api/AdminProduct'; // API基础路径，方便管理

Page({
  data: {
    products: [],
    currentPage: 1,
    totalPages: 1,
    pageSize: 9,
    selectedId: null,
    selectedProductName: '',
    loading: false,
    showModal: false,
    modalTitle: "",
    formData: {
      id: null,
      name: '',
      description: '', // 添加描述字段
      price: 0,
      stock: 0,
      imageUrl: '',
      isDeleted: false
    },
    activeTab: 'products',
    emptyProductSlots: [] // 用于填充空位的数组
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
      url: `${API_BASE_URL}/GetAllProducts`,
      method: 'GET',
      data: {
        pageIndex: currentPage,
        pageSize: pageSize
      },
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200) {
          const backendData = res.data;
          let productsToShow = backendData.items || [];
          
          // 计算并填充空位 (确保3x3网格视觉效果)
          let emptySlots = [];
          const itemsToFill = pageSize - productsToShow.length;
          if (productsToShow.length > 0 && itemsToFill > 0) { // 只在有数据且需要填充时填充
            for (let i = 0; i < itemsToFill; i++) {
              emptySlots.push({ empty: true, id: `empty_${i}` }); // 给空项一个唯一key
            }
          }

          this.setData({
            products: productsToShow.concat(emptySlots), // 合并真实商品和空位
            totalPages: backendData.totalPages || 1,
            loading: false
          });
        } else {
          wx.showToast({ title: `加载失败: ${res.data.message || res.statusCode}`, icon: 'none' });
          this.setData({ loading: false, products:[], totalPages:1 }); // 加载失败时清空数据
        }
      },
      fail: (err) => {
        this.setData({ loading: false, products:[], totalPages:1 });
        wx.showToast({ title: '网络请求失败', icon: 'none' });
        console.error("loadProducts fail", err);
      },
      complete: () => {
        this.setData({ loading: false });
      }
    });
  },

  // 分页处理
  handlePageChange(e) {
    if (this.data.loading) return; // 防止重复点击
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

  // 点击商品项（打开编辑弹窗）
  navigateToEdit(e) {
    const { id } = e.currentTarget.dataset;
    if (!id || String(id).startsWith('empty_')) { // 如果点击的是空占位项
        console.log("点击了空占位符或无效ID");
        return;
    }
    const product = this.data.products.find(p => p.id === id && !p.empty);
    if (product) {
      this.setData({
        showModal: true,
        modalTitle: "编辑商品",
        formData: {
          id: product.id,
          name: product.name || '',
          description: product.description || '',
          price: product.price || 0,
          stock: product.stock || 0,
          imageUrl: product.imageUrl || '',
          isDeleted: product.isDeleted || false
        }
      });
    }
  },

  // 恢复上架商品
  restoreProduct() {
    if (this.data.loading) return;
    const token = wx.getStorageSync('token');
    const { id } = this.data.formData;
    if (!id) {
        wx.showToast({ title: '未选择商品', icon: 'none' });
        return;
    }

    this.setData({ loading: true });
    wx.request({
      url: `${API_BASE_URL}/RestoreProduct/${id}`,
      method: 'PUT',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200 || res.statusCode === 204) {
          wx.showToast({ title: '商品已恢复上架' });
          this.setData({ showModal: false, selectedId: null });
          this.loadProducts();
        } else {
          wx.showToast({ title: `恢复失败: ${(res.data && res.data.title) || res.data || '未知错误'}`, icon: 'none' });
        }
      },
      fail: (err) => {
        wx.showToast({ title: '恢复请求失败', icon: 'none' });
        console.error("restoreProduct fail", err);
      },
      complete: () => {
        this.setData({ loading: false });
      }
    });
  },

  // 添加商品按钮（打开添加弹窗）
  navigateToAdd() {
    this.setData({
      showModal: true,
      modalTitle: "添加商品",
      formData: { // 重置表单
        id: null,
        name: '',
        description: '',
        price: null, // 设置为null或undefined，让placeholder显示
        stock: null,
        imageUrl: '',
        isDeleted: false
      }
    });
  },

  // 删除图标点击处理
  handleDelete(e) {
    // 阻止冒泡到navigateToEdit
    // event.stopPropagation() 在小程序中这样用： return false; 但catchtap本身就阻止冒泡

    const id = e.currentTarget.dataset.id;
    const name = e.currentTarget.dataset.name;
    const product = this.data.products.find(p => p.id === id);

    if (product && product.isDeleted) {
        wx.showToast({ title: '该商品已是下架状态', icon: 'none' });
        return; // 如果已删除，不弹出确认框
    }
    this.setData({
      selectedId: id,
      selectedProductName: name
    });
  },

  // 确认删除请求
  confirmDelete() {
    if (this.data.loading) return;
    const id = this.data.selectedId;
    if (!id) return;
    const token = wx.getStorageSync('token');
    this.setData({ loading: true });

    wx.request({
      url: `${API_BASE_URL}/DeleteProduct/${id}`,
      method: 'DELETE',
      header: { 'Authorization': `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200 || res.statusCode === 204) {
          wx.showToast({ title: '删除成功' });
          this.loadProducts();
        } else {
          wx.showToast({ title: `删除失败: ${(res.data && res.data.title) || res.data || '未知错误'}`, icon: 'none' });
        }
      },
      fail: (err) => {
        wx.showToast({ title: '删除请求失败', icon: 'none' });
        console.error("confirmDelete fail", err);
      },
      complete: () => {
        this.setData({ loading: false, selectedId: null, selectedProductName: '' });
      }
    });
  },

  // 取消选择 (用于删除确认弹窗)
  cancelSelect() {
    this.setData({ selectedId: null, selectedProductName: '' });
  },

  // 处理表单输入
  handleInputChange(e) {
    const { field } = e.currentTarget.dataset;
    let value = e.detail.value;
    this.setData({
      [`formData.${field}`]: value
    });
  },

  // 关闭模态框
  closeModal() {
    if (this.data.loading && this.data.modalTitle.includes('商品')) {
        // 如果正在提交商品表单，不允许直接关闭
        wx.showToast({ title: '正在处理...', icon: 'none'});
        return;
    }
    this.setData({ showModal: false });
  },

  // 提交表单 (添加或编辑)
  submitForm() {
    if (this.data.loading) return;
    const { formData, modalTitle } = this.data;
    const token = wx.getStorageSync('token');
    
    // 基本前端校验
    if (!formData.name || formData.name.trim() === "") {
        wx.showToast({ title: '商品名称不能为空', icon: 'none' }); return;
    }
    const price = parseFloat(formData.price);
    if (isNaN(price) || price <= 0) {
        wx.showToast({ title: '价格必须是大于0的数字', icon: 'none' }); return;
    }
    const stock = parseInt(formData.stock);
    if (isNaN(stock) || stock < 0) {
        wx.showToast({ title: '库存必须是大于等于0的整数', icon: 'none' }); return;
    }

    this.setData({ loading: true });

    let url = '';
    let method = '';
    let dataToSend = {
        name: formData.name.trim(),
        description: formData.description ? formData.description.trim() : '无',
        price: price,
        stock: stock,
        imageUrl: formData.imageUrl ? formData.imageUrl.trim() : '无'
    };

    if (modalTitle === "添加商品") {
      method = 'POST';
      url = `${API_BASE_URL}/CreateProduct`;
    } else if (modalTitle === "编辑商品") {
      method = 'PUT';
      url = `${API_BASE_URL}/UpdateProduct/${formData.id}`;
    } else {
      wx.showToast({ title: '未知操作', icon: 'none' });
      this.setData({ loading: false });
      return;
    }

    wx.request({
      url: url,
      method: method,
      data: dataToSend,
      header: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      success: (res) => {
        if (res.statusCode === 200 || res.statusCode === 201 || res.statusCode === 204) {
          wx.showToast({ title: `${modalTitle}成功` });
          this.setData({ showModal: false });
          this.loadProducts();
        } else {
          let errorMsg = `${modalTitle}失败`;
          if (res.data) {
            if (res.data.errors) {
              const errors = res.data.errors;
              const firstErrorField = Object.keys(errors)[0];
              if (errors[firstErrorField] && errors[firstErrorField].length > 0) {
                errorMsg = errors[firstErrorField][0];
              }
            } else if (res.data.title) { // ASP.NET Core ProblemDetails
                errorMsg = res.data.title;
            } else if (typeof res.data === 'string') {
                errorMsg = res.data;
            } else if (res.data.message) {
                errorMsg = res.data.message;
            }
          }
          wx.showToast({ title: errorMsg, icon: 'none', duration: 3000 });
        }
      },
      fail: (err) => {
        wx.showToast({ title: `${modalTitle}请求失败`, icon: 'none' });
        console.error("submitForm fail", err);
      },
      complete: () => {
        this.setData({ loading: false });
      }
    });
  },

  // 切换底部导航
  switchTab(e) {
    if (this.data.loading) { // 如果正在加载，不允许切换
        wx.showToast({title: '请稍候...', icon: 'none'});
        return;
    }
    const tab = e.currentTarget.dataset.tab;
    if (this.data.activeTab === tab) return; // 如果已经是当前tab，不操作

    this.setData({ activeTab: tab, products: [], currentPage: 1, totalPages: 1 }); // 清空当前内容区并重置分页

    if (tab === 'products') {
      this.loadProducts();
    } else {
      // wx.showToast({
      //   title: `已切换到 ${tab}`, // 暂时不提示，直接显示placeholder
      //   icon: 'none',
      //   duration: 1000
      // });
      // 未来这里会调用 loadOrders(), loadUsers(), loadStatistics() 等
      // 当前其他tab是空的，所以products会是空数组，显示“暂无数据”或对应的placeholder
    }
  },
  // 用于阻止模态框背景滚动 (可选)
  preventTouchMove() {
    return;
  }
});